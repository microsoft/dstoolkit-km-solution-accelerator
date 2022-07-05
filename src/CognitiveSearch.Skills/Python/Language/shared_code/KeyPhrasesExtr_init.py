# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import azure.functions as func
import json
from azure.core.credentials import AzureKeyCredential
from azure.ai.textanalytics import TextAnalyticsClient
import os

endpoint = os.environ["TEXT_ANALYTICS_ENDPOINT"]
key = os.environ["TEXT_ANALYTICS_KEY"]

#https://docs.microsoft.com/en-us/azure/cognitive-services/language-service/concepts/data-limits#maximum-characters-per-document
MAX_CHARS_PER_DOC=int(os.environ["MAX_CHARS_PER_DOC"])
#https://docs.microsoft.com/en-us/azure/cognitive-services/language-service/concepts/data-limits#maximum-documents-per-request
MAX_DOC_PER_REQUEST=int(os.environ["KPE_MAX_DOC_PER_REQUEST"])

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
            result = text_analytics_client.extract_key_phrases(languageData[lang]['chunks'], language = lang)
            for (doc, id) in zip(result, languageData[lang]['ids']):
                document = {}
                document['data'] = {}
                document['recordId'] = id
                if not doc.is_error:
                    max_keyPhrase_count = 10
                    if 'maxKeyPhraseCount' in headers:
                        max_keyPhrase_count = int(headers['maxKeyPhraseCount'])
                    elif "MAX_KEYPHRASES_EXTR" in os.environ:
                        max_keyPhrase_count = int(os.environ["MAX_KEYPHRASES_EXTR"])

                    if len(doc.key_phrases) <= max_keyPhrase_count:
                        document['data']['keyPhrases'] = doc.key_phrases
                    else:
                        document['data']['keyPhrases'] = doc.key_phrases[:max_keyPhrase_count]
                else:
                    document['errors'] = [{"message": doc.error.code + ": " + doc.error.message}]
                    
                document['data']['languageCode'] = lang
                document['warnings'] = doc.warnings
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
        document['warnings'] = []

        assert ('data' in record), "'data' field is required."
        data = record['data']


        if 'languageCode' not in data:
            if 'defaultLanguageCode' in headers:
                data['languageCode'] = headers['defaultLanguageCode']
            else:
                data['languageCode'] = 'en'

        # https://aka.ms/text-analytics-data-limits
        text = data['text']
        chunks = [text[i:i+MAX_CHARS_PER_DOC] for i in range(0, len(text), MAX_CHARS_PER_DOC)]

        # If the number of chunks is greater than the maximum allowed, then ignore the rest of it.
        if len(chunks) > MAX_DOC_PER_REQUEST:
            chunks=chunks[:MAX_DOC_PER_REQUEST]

        result = text_analytics_client.extract_key_phrases(chunks, language = data['languageCode'], show_stats=True)
        for doc in result:
            if not doc.is_error:
                max_keyPhrase_count = 10
                if 'maxKeyPhraseCount' in headers:
                    max_keyPhrase_count = int(headers['maxKeyPhraseCount'])
                elif "MAX_KEYPHRASES_EXTR" in os.environ:
                    max_keyPhrase_count = int(os.environ["MAX_KEYPHRASES_EXTR"])

                if len(doc.key_phrases) <= max_keyPhrase_count:
                    document['data']['keyPhrases'] = doc.key_phrases
                else:
                    document['data']['keyPhrases'] = doc.key_phrases[:max_keyPhrase_count]
            else:
                document['errors'] = [{"message": doc.error.code + ": " + doc.error.message}]
                
            document['data']['languageCode'] = data['languageCode']
            document['warnings'] = doc.warnings

        #document['data']=json.loads(rawHttpResponse.response.content)

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
