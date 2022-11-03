# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

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

class DateTimeEncoder(JSONEncoder):
        #Override the default method
        def default(self, obj):
            if isinstance(obj, (datetime.date, datetime.datetime)):
                return obj.isoformat()

endpoint = os.environ["FORMS_RECOGNIZER_ENDPOINT"]
key = os.environ["FORMS_RECOGNIZER_KEY"]

form_recognizer_client = FormRecognizerClient(endpoint, AzureKeyCredential(key))
document_analysis_client = DocumentAnalysisClient(
    endpoint=endpoint, credential=AzureKeyCredential(key)
)

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
            logging.info(body)
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
def extract_tables(result):

    tables = []
    for table_idx, table in enumerate(result.tables):       
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

        tables.append(
            {
                "row_count": table.row_count,
                "column_count": table.column_count,
                "cells": cells
            }
        )
    return tables

#
# Extract Pages (Read)
#
def extract_pages(result):
    for page in result.pages:
        print("----Analyzing document from page #{}----".format(page.page_number))
        print(
            "Page has width: {} and height: {}, measured with unit: {}".format(
                page.width, page.height, page.unit
            )
        )

        for line_idx, line in enumerate(page.lines):
            words = line.get_words()
            print(
                "...Line # {} has {} words and text '{}' within bounding polygon '{}'".format(
                    line_idx,
                    len(words),
                    line.content,
                    line.polygon,
                )
            )

            for word in words:
                print(
                    "......Word '{}' has a confidence of {}".format(
                        word.content, word.confidence
                    )
                )

        for selection_mark in page.selection_marks:
            print(
                "...Selection mark is '{}' within bounding polygon '{}' and has a confidence of {}".format(
                    selection_mark.state,
                    selection_mark.polygon,
                    selection_mark.confidence,
                )
            )

#
# Extract Paragraphs (Layout)
#
def extract_paragraphs(result):
    paragraphs=[]
    for paragraph in result.paragraphs:
        paragraphs.append(paragraph.content)

    return paragraphs

#
# Extract KV pairs
#
def extract_kv(result):
    kvs=[]
    for kv_pair in result.key_value_pairs:
        if kv_pair.key:
            print(
                    "Key '{}' found within '{}' bounding regions".format(
                        kv_pair.key.content,
                        kv_pair.key.bounding_regions,
                    )
                )
        if kv_pair.value:
            print(
                    "Value '{}' found within '{}' bounding regions\n".format(
                        kv_pair.value.content,
                        kv_pair.value.bounding_regions,
                    )
                )

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
        print(data)

        form_url = data["formUrl"]  + data["formSasToken"]   
        print(form_url)
        # poller = form_recognizer_client.begin_recognize_content_from_url(form_url)
        # pages = poller.result()  

        poller = document_analysis_client.begin_analyze_document_from_url("prebuilt-document", form_url)
        result = poller.result()

        # Extract Pages
        # pages = extract_pages(result)

        # Extract Paragraphs
        paragraphs = extract_paragraphs(result)

        # Extract Tables
        tables = extract_tables(result)
        
        # Extract Key Value Pairs
        kvs = extract_kv(result)

        # Extract Styles
        for style in result.styles:
            if style.is_handwritten:
                print("Document contains handwritten content: ")
                print(",".join([result.content[span.offset:span.offset + span.length] for span in style.spans]))

    except AssertionError  as error:
        return (
            {
            "recordId": recordId,
            "data": {
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
                "tables": tables,
                "tables_count": len(tables),
                "paragraphs": paragraphs,
                "paragraphs_count": len(paragraphs),
                "kvs": kvs,
                "kvs_count": len(kvs)
            }
            })