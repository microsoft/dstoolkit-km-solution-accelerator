# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import azure.functions as func
import json
from azure.core.credentials import AzureKeyCredential
from azure.ai.textanalytics import TextAnalyticsClient
import os

endpoint = os.environ["COGNITIVE_SERVICES_ENDPOINT"]
key = os.environ["COGNITIVE_SERVICES_KEY"]

if 'LANGUAGE_MODEL_VERSION' in os.environ:
    model=os.environ["LANGUAGE_MODEL_VERSION"]
else:
    model='latest'

#https://docs.microsoft.com/en-us/azure/cognitive-services/language-service/concepts/data-limits#maximum-characters-per-document
MAX_CHARS_PER_DOC=int(os.environ["MAX_CHARS_PER_DOC"])
#https://docs.microsoft.com/en-us/azure/cognitive-services/language-service/concepts/data-limits#maximum-documents-per-request
MAX_DOC_PER_REQUEST=int(os.environ["LD_MAX_DOC_PER_REQUEST"])

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

    if 'type' in headers:
        if headers['type'] == 'big':
            for value in values:
                output_record = transform_value_big(headers, value)
                if output_record != None:
                    results["values"].append(output_record)
        else:
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
        
        ids = []
        chunks = []
        for value in records:
            ids.append(value['recordId'])
            chunks.append(value['data']['text'])
        
        if 'defaultCountryHint' in headers:
            result = text_analytics_client.detect_language(chunks, country_hint = str(headers['defaultCountryHint']))
        else:
            result = text_analytics_client.detect_language(chunks, country_hint = 'US')

        for (doc, id) in zip(result, ids):
            document = {}
            document['data'] = {}
            document['recordId'] = id
            if not doc.is_error:
                document['data']['languageCode'] = doc.primary_language.iso6391_name
                document['data']['languageName'] = doc.primary_language.name
                document['data']['score'] = doc.primary_language.confidence_score
            else:
                document['data']['languageCode'] = 'en'
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

        document['data'] = {}

        assert ('data' in record), "'data' field is required."
        data = record['data']

        # https://aka.ms/text-analytics-data-limits
        text = data['text'][:MAX_CHARS_PER_DOC]

        # no chunking here
        #         
        if 'countryHint' not in data:
            if 'defaultCountryHint' in headers:
                data['countryHint'] = str(headers['defaultCountryHint'])
            else:
                data['countryHint'] = 'US'

        # 15-Dec-2022
        # https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/language-detection/language-support
        # model_version - Hack to retain Japanese language detection quality...
        result = text_analytics_client.detect_language([text], country_hint = data['countryHint'], model_version = '2021-11-20')

        for doc in result:
            if not doc.is_error:
                document['data']['languageCode'] = doc.primary_language.iso6391_name
                document['data']['languageName'] = doc.primary_language.name
                document['data']['score'] = doc.primary_language.confidence_score
            else:
                document['data']['languageCode'] = 'en'
                document['warnings'] = [{"message": doc.error.code + ": " + doc.error.message}]           

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

    return (document)
