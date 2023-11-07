# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging

from requests.models import HTTPError
import azure.functions as func
from operator import itemgetter
import json
import requests, uuid
import os
import time

endpoint = os.environ["COGNITIVE_SERVICES_ENDPOINT"]
subscription_key = os.environ["COGNITIVE_SERVICES_KEY"]
 
#https://docs.microsoft.com/en-us/azure/cognitive-services/language-service/concepts/data-limits#maximum-characters-per-document
MAX_CHARS_PER_DOC=int(os.environ["MAX_CHARS_PER_DOC"])
#https://docs.microsoft.com/en-us/azure/cognitive-services/language-service/concepts/data-limits#maximum-documents-per-request
MAX_DOC_PER_REQUEST=int(os.environ["TS_MAX_DOC_PER_REQUEST"])

TS_MODEL_VERSION=os.environ["TS_MODEL_VERSION"]

constructed_url = endpoint + "/text/analytics/"+TS_MODEL_VERSION+"/analyze"

headers_summarizer = {
                # Request headers
                'Content-Type': 'application/json',
                'Ocp-Apim-Subscription-Key': subscription_key
            }

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

    output_record = transform_value(headers, values)
    results["values"] = output_record

    return json.dumps(results, ensure_ascii=False)        

## Perform an operation on a record
def transform_value(headers, records):
    # Validate the inputs
    try:
        response_list = []      

        '''fromLanguageCode = 'en'
        if "fromLanguageCode" in data and data["fromLanguageCode"] in available_language:
            fromLanguageCode = data["fromLanguageCode"]
        elif "defaultFromLanguageCode" in headers and headers["defaultFromLanguageCode"] in available_language:
            fromLanguageCode = headers["defaultFromLanguageCode"]
        elif "suggestedFrom" in headers and headers["suggestedFrom"] in available_language:
            fromLanguageCode = headers["suggestedFrom"]

        toLanguageCode = headers["defaultToLanguageCode"]
        if "toLanguageCode" in data:
            toLanguageCode = data["toLanguageCode"]
        
        document['data']['translatedFromLanguageCode'] = fromLanguageCode
        document['data']['translatedToLanguageCode'] = toLanguageCode
        
        if fromLanguageCode != toLanguageCode:
            params = {
                'api-version': '3.0',
                'from': fromLanguageCode,
                'to': toLanguageCode
            }'''
            
        params = {}
        body = {
            "displayName": "Extracting Location & US Region",
            "analysisInput": {
                "documents": []
            },
            "tasks": {
                "extractiveSummarizationTasks": [
                    {
                        "parameters": {
                            "model-version": "latest"
                        }
                    }
                ],
            }
        }

        for record in records:
            assert ('data' in record), "'data' field is required."
            data = record['data']
            request_document = {
                "id": record['recordId'],
                "language": data['language'],
                "text": data['text']
            }
            body['analysisInput']['documents'].append(request_document)

        request = requests.post(constructed_url, params=params, headers=headers_summarizer, json=body)
        url = request.headers['operation-location']
        running = True
        while(running):
            request = requests.get(url, params=params, headers=headers_summarizer)
            response = request.json()
            status = response['status']
            if status != 'running' and status != 'notStarted':
                print(status)
                running = False
            else:
                time.sleep(5)

        if status == 'succeeded':
            results = response['tasks']['extractiveSummarizationTasks'][0]['results']
            
            for text, record in zip(results['documents'], records):
                sentences = text['sentences']
                sentences = sorted(sentences, key=itemgetter('rankScore'), reverse=True)
                document = {}
                document['recordId'] = record['recordId']
                document['data'] = {}            
                document['data']['summarizedText'] = sentences
                response_list.append(document)
        else:
            document = {}
            document['recordId'] = 1
            document['data'] = {}
            document['warnings'] = [{"message": request.text}]
            response_list.append(document)

    except KeyError as error:
        return [
            {
            "recordId": record['recordId'],
            "data" : {},
            "warnings": [ { "message": "KeyError:" + error.args[0] }   ]       
            }]
    except AssertionError as error:
        return [
            {
            "recordId": record['recordId'],
            "data" : {},
            "warnings": [ { "message": "AssertionError:" + error.args[0] }   ]       
            }]
    except SystemError as error:
        return [
            {
            "recordId": record['recordId'],
            "data" : {},
            "warnings": [ { "message": "SystemError:" + error.args[0] }   ]       
            }]
    except AttributeError as error:
        return [
            {
            "recordId": record['recordId'],
            "data" : {},
            "warnings": [ { "message": "AttributeError:" + error.args[0] }   ]       
            }]

    return response_list
