# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import azure.functions as func
import json
from azure.core.credentials import AzureKeyCredential
from azure.ai.textanalytics import TextAnalyticsClient
import os

endpoint = os.environ["LANGUAGE_ENDPOINT"]
key = os.environ["LANGUAGE_KEY"]

#https://docs.microsoft.com/en-us/azure/cognitive-services/language-service/concepts/data-limits#maximum-characters-per-document
MAX_CHARS_PER_DOC=int(os.environ["MAX_CHARS_PER_DOC"])
#https://docs.microsoft.com/en-us/azure/cognitive-services/language-service/concepts/data-limits#maximum-documents-per-request
MAX_DOC_PER_REQUEST=int(os.environ["EL_MAX_DOC_PER_REQUEST"])

text_analytics_client = TextAnalyticsClient(endpoint=endpoint, credential=AzureKeyCredential(key))

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
    except ValueError:
        return func.HttpResponse(
             "Invalid body",
             status_code=400
        )
    if body:
        result = compose_response(req.headers, body)
        return func.HttpResponse(result, mimetype="application/json")
    else:
        return func.HttpResponse(
             "Invalid body",
             status_code=400
        )

def compose_response(headers, json_data):
    values = json.loads(json_data)['values']
    
    # Prepare the Output before the loop
    results = {}
    results["values"] = []
    
    if 'type' in headers and headers['type'] != 'big':
        results = transform_value_small(headers, values)
    else:
        for value in values:
            output_record = transform_value_big(headers, value)
            if output_record != None:
                results["values"].append(output_record)


    return json.dumps(results, ensure_ascii=False)

def transform_value_small(headers, records):
    try:
        results = {}
        results["values"] = []
        
        languageData = {}
        for value in records:
            if 'languageCode' not in value['data']:
                if 'defaultLanguageCode' in headers:
                    value['data']['languageCode'] = str(headers['defaultLanguageCode'])
                else:
                    value['data']['languageCode'] = 'en'
            if value['data']['languageCode'] not in languageData:
                languageData[value['data']['languageCode']] = {'ids': [], 'chunks':[]}
            languageData[value['data']['languageCode']]['ids'].append(value['recordId'])
            languageData[value['data']['languageCode']]['chunks'].append(value['data']['text'])

        for lang in languageData:
            result = text_analytics_client.recognize_linked_entities(languageData[lang]['chunks'], language = lang)
                
            for (doc, id) in zip(result, languageData[lang]['ids']):
                document = {}
                document['data'] = {}
                document['recordId'] = id
                document['data']['entities'] = []
                if not doc.is_error:
                    for entity in doc.entities:
                        extracted_entity = {}
                        extracted_entity['name'] = entity.name
                        extracted_entity['id'] = entity.data_source_entity_id
                        extracted_entity['language'] = entity.language
                        extracted_entity['url'] = entity.url
                        extracted_entity['bingId'] = entity.bing_entity_search_api_id
                        extracted_entity['dataSource'] = entity.data_source
                        extracted_entity['matches'] = []
                        for match in entity.matches:
                            min_precision = 0
                            if 'minimumPrecision' in headers:
                                min_precision = float(headers['minimumPrecision'])
                            elif "MIN_PRECISION_ENTITY_LINKING" in os.environ:
                                min_precision = float(os.environ["MIN_PRECISION_ENTITY_LINKING"])
                            
                            if match.confidence_score < min_precision:
                                continue
                            extracted_match = {
                                "text": match.text,
                                "offset": match.offset,
                                "length": match.length,
                                "confidenceScore": match.confidence_score
                            } 
                            extracted_entity['matches'].append(extracted_match)
                        if len(extracted_entity['matches'])>0:
                            document['data']['entities'].append(extracted_entity)
                else:
                    document['warnings'] = [{"message": doc.error.code + ": " + doc.error.message}]
                results['values'].append(document)
    except KeyError as error:
        ids = []
        for value in records:
            ids.append(value['recordId'])
        results = {}
        results["values"] = []
        for id in ids:
            results["values"].append(
            {
                "recordId": id,
                "errors": [ { "message": "KeyError:" + error.args[0] }   ]       
            }
            )
        return results
    except AssertionError as error:
        ids = []
        for value in records:
            ids.append(value['recordId'])
        results = {}
        results["values"] = []
        for id in ids:
            results["values"].append(
            {
                "recordId": id,
                "errors": [ { "message": "AssertionError:" + error.args[0] }   ]
            }
            )
        return results
    except SystemError as error:
        ids = []
        for value in records:
            ids.append(value['recordId'])
        results = {}
        results["values"] = []
        for id in ids:
            results["values"].append(
            {
                "recordId": id,
                "errors": [ { "message": "SystemError:" + error.args[0] }   ]
            }
            )
        return results
    except AttributeError as error:
        ids = []
        for value in records:
            ids.append(value['recordId'])
        results = {}
        results["values"] = []
        for id in ids:
            results["values"].append(
            {
                "recordId": id,
                "errors": [ { "message": "AttributeError:" + error.args[0] }   ]       
            }
            )
        return results

    return results

## Perform an operation on a record
def transform_value_big(headers, record):
    try:
        recordId = record['recordId']
    except AssertionError  as error:
        return None

    # Validate the inputs
    try:
        document = {}
        document['recordId'] = recordId
        document['warnings'] = []

        document['data'] = {}
        document['data']['entities'] = []

        assert ('data' in record), "'data' field is required."
        data = record['data']

        if "text" in data:
            # https://aka.ms/text-analytics-data-limits
            text = data['text']
            chunks = [text[i:i+MAX_CHARS_PER_DOC] for i in range(0, len(text), MAX_CHARS_PER_DOC)]

            # If the number of chunks is greater than the maximum allowed, then ignore the rest of it.
            if len(chunks) > MAX_DOC_PER_REQUEST:
                chunks=chunks[:MAX_DOC_PER_REQUEST]

            if 'languageCode' not in data:
                if 'defaultLanguageCode' in headers:
                    data['languageCode'] = str(headers['defaultLanguageCode'])
                else:
                    data['languageCode'] = 'en'

            result = text_analytics_client.recognize_linked_entities(chunks, language = data['languageCode'])

            for doc in result:
                if not doc.is_error:
                    for entity in doc.entities:
                        extracted_entity = {}
                        extracted_entity['name'] = entity.name
                        extracted_entity['id'] = entity.data_source_entity_id
                        extracted_entity['language'] = entity.language
                        extracted_entity['url'] = entity.url
                        extracted_entity['bingId'] = entity.bing_entity_search_api_id
                        extracted_entity['dataSource'] = entity.data_source
                        extracted_entity['matches'] = []
                        for match in entity.matches:
                            min_precision = 0
                            if 'minimumPrecision' in headers:
                                min_precision = float(headers['minimumPrecision'])
                            elif "MIN_PRECISION_ENTITY_LINKING" in os.environ:
                                min_precision = float(os.environ["MIN_PRECISION_ENTITY_LINKING"])
                            
                            if match.confidence_score < min_precision:
                                continue
                            extracted_match = {
                                "text": match.text,
                                "offset": match.offset,
                                "length": match.length,
                                "confidenceScore": match.confidence_score
                            } 
                            extracted_entity['matches'].append(extracted_match)
                        if len(extracted_entity['matches'])>0:
                            document['data']['entities'].append(extracted_entity)

                else:
                    document['warnings'].append({"message": doc.error.code + ": " + doc.error.message})
        else:
            document['data']['entities'] = []
            document['warnings'] = [{"message": "No text found in the input"}]

    except KeyError as error:
        return (
            {
            "recordId": recordId,
            "errors": [ { "message": "KeyError:" + error.args[0] }   ]       
            })
    except AssertionError as error:
        return (
            {
            "recordId": recordId,
            "errors": [ { "message": "AssertionError:" + error.args[0] }   ]       
            })
    except SystemError as error:
        return (
            {
            "recordId": recordId,
            "errors": [ { "message": "SystemError:" + error.args[0] }   ]       
            })
    except AttributeError as error:
        return (
            {
            "recordId": recordId,
            "errors": [ { "message": "AttributeError:" + error.args[0] }   ]       
            })

    return (document)
