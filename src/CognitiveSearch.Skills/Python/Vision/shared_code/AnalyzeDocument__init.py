# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import io
import logging
import json
import os
import logging
import datetime
from json import JSONEncoder
import azure.functions as func
from azure.ai.formrecognizer import FormRecognizerClient
from azure.core.credentials import AzureKeyCredential
from azure.ai.formrecognizer import DocumentAnalysisClient
from azure.storage.blob import BlobClient
import base64

from .Utils import StorageUtils

#
# Helpers 
#
def format_bounding_region(bounding_regions):
    if not bounding_regions:
        return "N/A"
    return ", ".join("Page #{}: {}".format(region.page_number, format_polygon(region.polygon)) for region in bounding_regions)

def format_polygon(polygon):
    if not polygon:
        return "N/A"
    return ", ".join(["[{}, {}]".format(p.x, p.y) for p in polygon])

#
# FORM Rcognizer variables
#
form_endpoint = os.environ["FORMS_RECOGNIZER_ENDPOINT"]
form_key = os.environ["FORMS_RECOGNIZER_KEY"]
form_model=os.environ["FORMS_RECOGNIZER_MODEL"]

form_recognizer_client = FormRecognizerClient(form_endpoint, AzureKeyCredential(form_key))

# Trick to get the raw json response from the API.
# class KMDocumentAnalysisClient(DocumentAnalysisClient):

#     def _analyze_document_callback(self, raw_response, _, headers):  # pylint: disable=unused-argument
#         analyze_operation_result = self._deserialize(self._generated_models.AnalyzeResultOperation, raw_response)
#         return analyze_operation_result.analyze_result
#         # return json.loads(raw_response.http_response.)

# document_analysis_client = KMDocumentAnalysisClient(
#     endpoint=form_endpoint, credential=AzureKeyCredential(form_key)
# )

document_analysis_client = DocumentAnalysisClient(
    endpoint=form_endpoint, credential=AzureKeyCredential(form_key)
)

#
# HOCR Generation
#

HOcrHeader = """<?xml version='1.0' encoding='UTF-8'?>
<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'>
<html xmlns='http://www.w3.org/1999/xhtml' xml:lang='en' lang='en'>
<head><title></title>
<meta http-equiv='Content-Type' content='text/html;charset=utf-8'/>
<meta name='ocr-system' content='Microsoft Cognitive Services'/>
<meta name='ocr-capabilities' content='ocr_page ocr_carea ocr_par ocr_line ocrx_word'/>
</head><body>"""
HOcrFooter ="""</body></html>"""
HOcrEnabled = True

#
# Main 
#
def main(req: func.HttpRequest, context: func.Context) -> func.HttpResponse:
    logging.info(f'{context.function_name} HTTP trigger function processed a request.')
    if hasattr(context, 'retry_context'):
        logging.info(f'Current retry count: {context.retry_context.retry_count}')
        
        if context.retry_context.retry_count == context.retry_context.max_retry_count:
            logging.info(
                f"Max retries of {context.retry_context.max_retry_count} for "
                f"function {context.function_name} has been reached")
    
    try:
        body = json.dumps(req.get_json())
        if body:
            result = compose_response(body)
            return func.HttpResponse(result, mimetype="application/json")
        else:
            return func.HttpResponse(
                "Invalid body",
                status_code=400
            )
    except ValueError:
        return func.HttpResponse(
             "Invalid body",
             status_code=400
        )
    except KeyError:
        return func.HttpResponse(
             "Skill configuration error. Endpoint & Key required.",
             status_code=400
        )

def compose_response(json_data):
    values = json.loads(json_data)['values']
    # Prepare the Output before the loop
    results = {}
    results["values"] = []
    
    for value in values:
        output_record = transform_value(value)
        if output_record != None:
            results["values"].append(output_record)
    
    return json.dumps(results, ensure_ascii=False, cls=DateTimeEncoder)

#
# Extract Tables from Results
#
def extract_tables(blobname,input_tables):
    tables = []
    for table_idx, table in enumerate(input_tables):
        print(
            "Table # {} has {} rows and {} columns".format(
                table_idx, table.row_count, table.column_count
            )
        )

        for region in table.bounding_regions:
            print(
                "Table # {} location on page: {} is {}".format(
                    table_idx,
                    region.page_number,
                    region.polygon,
                )
            )

        cells = []
        for cell in table.cells:
            print(
                "...Cell[{}][{}] has content '{}'".format(
                    cell.row_index,
                    cell.column_index,
                    cell.content,
                )
            )
            cells.append(
                {
                    "text": cell.content,
                    "rowIndex": cell.row_index,
                    "rowSpan": cell.row_span,
                    "colIndex": cell.column_index,
                    "colSpan": cell.column_span,
                    "is_header": (cell.kind=='columnHeader')
                }
            )
            for region in cell.bounding_regions:
                print(
                    "...content on page {} is within bounding polygon '{}'\n".format(
                        region.page_number,
                        region.polygon,
                    )
                )

        # tables.append(
        #     {
        #         "row_count": table.row_count,
        #         "column_count": table.column_count,
        #         "cells": cells
        #     }
        # )
        tables.append(json.dumps(
            {
                "row_count": table.row_count,
                "column_count": table.column_count,
                "cells": cells
            }
        ))

    persist_object(blobname,".tables",tables)

    # Pivoted tables TODO
    pivoted_tables=[]
    persist_object(blobname,".pivoted.tables",pivoted_tables)

    return tables

#
# Extract Pages
#
def extract_pages(base_url,blobname,result):

    hocrdocument = HOcrHeader

    for page in result.pages:
        
        print("----Analyzing document from page #{}----".format(page.page_number))
        print(
            f"Page has width: {page.width} and height: {page.height}, measured with unit: {page.unit}"
        )

        hocrdocument+=f"<div class='ocr_page' id='page_{page.page_number}' title='image \"{base_url}\"; bbox 0 0 {page.width} {page.height}; ppageno {page.page_number}'>"
        hocrdocument+=f"<div class='ocr_carea' id='block_{page.page_number}_1'>"

        for line_idx, line in enumerate(page.lines):
            words = line.get_words()
            print(
                "...Line # {} has {} words within bounding polygon '{}'".format(
                    line_idx,
                    len(words),
                    format_polygon(line.polygon)
                )
            )

            hocrdocument+=f"<span class='ocr_line' id='line_{page.page_number}_{line_idx}' title='baseline -0.002 -5; x_size 30; x_descenders 6; x_ascenders 6'>"

            for word_idx, word in enumerate(words):                
                annotation = ''
                #     if (wordAnnotations != null && wordAnnotations.TryGetValue(word.Text, out string wordAnnotation))
                #     {
                #         annotation = $"data-annotation='{wordAnnotation}'";
                #     }
                bbox = f"bbox {word.polygon[0]} {word.polygon[1]} {word.polygon[4]} {word.polygon[6]}"

                hocrdocument+=f"<span class='ocrx_word' id='word_{page.page_number}_{line_idx}_{word_idx}' title='{bbox}' {annotation}>{word.content}</span>"

            # Line
            hocrdocument+=("</span>")

        for selection_mark in page.selection_marks:
            print(
                "...Selection mark is '{}' within bounding polygon '{}' and has a confidence of {}".format(
                    selection_mark.state,
                    selection_mark.polygon,
                    selection_mark.confidence,
                )
            )

        # Reading area
        hocrdocument+=("</div>")
        # Page
        hocrdocument+=("</div>")

    # not serializable object...
    # persist_object(blobname,".pages",result.pages)

    # HOcr
    hocrdocument+=HOcrFooter
    persist_text(blobname,".hocr",hocrdocument)

#
# Extract Paragraphs (Layout)
#
def extract_paragraphs(blobname,input_paragraphs):
    paragraphs=[]
    for paragraph in input_paragraphs:
        paragraphs.append({
                "role": paragraph.role if paragraph.role is not None else "inline",
                "content":paragraph.content,
                "length":len(paragraph.content)
        })

    StorageUtils.persist_object(blobname,".paragraphs",paragraphs)

    return paragraphs

#
# Extract KV pairs
#
def extract_kv(blobname,key_value_pairs):
    kvs=[]
    for kv_pair in key_value_pairs:
        if kv_pair.key and kv_pair.value:
            kvs.append({
                "key":kv_pair.key.content,
                "value":kv_pair.value.content
            })
        else:
            kvs.append({
                "key":kv_pair.key.content,
                "value":''
            })

    StorageUtils.persist_object(blobname,".kvs",kvs)

    return kvs

## Perform an operation on a record
def transform_value(value):
    try:
        recordId = value['recordId']
    except AssertionError  as error:
        return None
    # Validate the inputs
    try:
        assert ('data' in value), "'data' field is required."
        data = value['data'] 

        base_url=None
        if 'formUrl' in data:
            base_url = data["formUrl"]
            form_url = base_url  + data["formSasToken"]
            logging.info(f'Form formUrl base url {form_url}')
            poller = document_analysis_client.begin_analyze_document_from_url(form_model, form_url)
        elif 'file_data' in data:
            base_url = data["file_data"]["url"]
            logging.info(f'Form file_data base url {data["file_data"]["url"]}')
            base64Image = base64.b64decode(data["file_data"]["data"])
            img_stream=io.BytesIO(base64Image)
            poller = document_analysis_client.begin_analyze_document(form_model, img_stream)
        else:
            logging.warning('Function - Invalid value record')

        result = poller.result()

        if base_url is None:
            if 'document_url' in data:
                base_url = data['document_url']

        logging.info(f'Form recognized base url {base_url}')

        target_url=StorageUtils.getTargetUrl(base_url)

        logging.info(f'Form recognized target url {target_url}')

        content = result.content 

        # Extract Pages
        pages=[]
        if result.pages:
            pages = extract_pages(base_url,target_url,result)

        # Extract Paragraphs
        paragraphs=[]
        if result.paragraphs:
            paragraphs = extract_paragraphs(target_url,result.paragraphs)

        # Extract Tables
        tables=[]
        if result.tables:
            tables = extract_tables(target_url,result.tables)
        
        # Extract Key Value Pairs
        kvs=[]
        if result.key_value_pairs:
            kvs = extract_kv(target_url,result.key_value_pairs)

        # Extract Styles
        if result.styles:
            for style in result.styles:
                if style.is_handwritten:
                    print("Document contains handwritten content: ")
                    print(",".join([result.content[span.offset:span.offset + span.length] for span in style.spans]))

    except AssertionError  as error:
        return (
            {
            "recordId": recordId,
            "data": {
                "content": '',
                "content_length": 0,
                "tables": [],
                "tables_count": 0,
                "paragraphs": [],
                "paragraphs_count": 0,
                "kvs": [],
                "kvs_count": 0
            },
            "warnings": [ { "message": "Error:" + error.args[0] }   ]       
            })
    except Exception as error:
        return (
            {
            "recordId": recordId,
            "data": {
                "content": '',
                "content_length": 0,
                "tables": [],
                "tables_count": 0,
                "paragraphs": [],
                "paragraphs_count": 0,
                "kvs": [],
                "kvs_count": 0
            },
            "warnings": [ { "message": "Error:" + str(error) }   ]       
            })
    return ({
            "recordId": recordId,
            "data": {
                "content": content,
                "content_length": len(content),
                "tables": tables,
                "tables_count": len(tables),
                "paragraphs": paragraphs,
                "paragraphs_count": len(paragraphs),
                "kvs": kvs,
                "kvs_count": len(kvs)
            }
            })