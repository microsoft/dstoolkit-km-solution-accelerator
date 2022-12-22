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
MAX_DOC_PER_REQUEST=int(os.environ["NER_MAX_DOC_PER_REQUEST"])

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
    entity_recognition_mapping = {
        "Person": {"name": "persons", "matched":[]}, 
        "Location": {"name": "locations", "matched":[]}, 
        "Organization": {"name": "organizations", "matched":[]},
        "Quantity": {"name": "quantities", "matched":[]}, 
        "DateTime": {"name": "dateTimes", "matched":[]}, 
        "URL": {"name": "urls", "matched":[]}, 
        "Email": {"name": "emails", "matched":[]}, 
        "PersonType": {"name": "personTypes", "matched":[]}, 
        "Event": {"name": "events", "matched":[]}, 
        "Product": {"name": "products", "matched":[]}, 
        "Skill": {"name": "skills", "matched":[]}, 
        "Address": {"name": "addresses", "matched":[]}, 
        "Phone Number": {"name": "phoneNumbers", "matched":[]}, 
        "IP Address": {"name": "ipAddresses", "matched":[]}
    }
    if body:
        result = compose_response(req.headers, body, entity_recognition_mapping)
        return func.HttpResponse(result, mimetype="application/json")
    else:
        return func.HttpResponse(
             "Invalid body",
             status_code=400
        )

def compose_response(headers, json_data, entity_recognition_mapping):
    values = json.loads(json_data)['values']
    
    # Prepare the Output before the loop
    results = {}
    results["values"] = []
    
    if 'type' in headers:
        if headers['type'] == 'big':
            for value in values:
                output_record = transform_value_big(headers, value, entity_recognition_mapping)
                if output_record != None:
                    results["values"].append(output_record)
        else:
            results = transform_value_small(headers, values, entity_recognition_mapping)
    else:
        for value in values:
            output_record = transform_value_big(headers, value, entity_recognition_mapping)
            if output_record != None:
                results["values"].append(output_record)

    return json.dumps(results, ensure_ascii=False)

def transform_value_small(headers, records, entity_recognition_mapping):
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

            result = text_analytics_client.recognize_entities(languageData[lang]['chunks'], language = lang)
            accepted_categories = ["Person", "Location", "Organization", "Quantity", "DateTime", "URL", "Email", "PersonType", "Event", "Product", "Skill", "Address", "Phone Number", "IP Address"]
            if "categories" in headers:
                accepted_categories = headers['categories']
            elif "ENTITY_RECOGNITION_ACCEPPTED" in os.environ:
                accepted_categories = os.environ['ENTITY_RECOGNITION_ACCEPPTED']
            categories_dict = dict(filter(lambda elem: elem[0] in accepted_categories, entity_recognition_mapping.items()))
            
            for (doc, id) in zip(result, languageData[lang]['ids']):
                document = {}
                document['data'] = {}
                document['recordId'] = id
                namedEntities = []
                if not doc.is_error:
                    for entity in doc.entities:
                        if entity.category in categories_dict:
                            min_precision = 0
                            if 'minimumPrecision' in headers:
                                min_precision = float(headers['minimumPrecision'])
                            elif "MIN_PRECISION_ENTITY_RECOGNITION" in os.environ:
                                min_precision = float(os.environ["MIN_PRECISION_ENTITY_RECOGNITION"])
                            if entity.confidence_score < min_precision:
                                continue
                        
                            entity_extracted = {}
                            entity_extracted['category'] = entity.category
                            categories_dict[entity.category]['matched'].append(entity.text)
                            entity_extracted['subcategory'] = entity.subcategory
                            entity_extracted['length'] = entity.length
                            entity_extracted['offset'] = entity.offset
                            entity_extracted['confidenceScore'] = entity.confidence_score
                            entity_extracted['text'] = entity.text
                            namedEntities.append(entity_extracted)
                    if len(namedEntities) > 0:
                        for category in  categories_dict:
                            document['data'][categories_dict[category]['name']] = categories_dict[category]['matched']
                            categories_dict[category]['matched'] = []
                        document['data']['namedEntities'] = namedEntities
                else:
                    document['errors'] = [{"message": doc.error.code + ": " + doc.error.message}]
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
def transform_value_big(headers, record, entity_recognition_mapping):
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
        
        result = text_analytics_client.recognize_entities(chunks, language = data['languageCode'])
        
        accepted_categories = ["Person", "Location", "Organization", "Quantity", "DateTime", "URL", "Email", "PersonType", "Event", "Product", "Skill", "Address", "Phone Number", "IP Address"]
        if "categories" in headers:
            accepted_categories = headers['categories']
        elif "ENTITY_RECOGNITION_ACCEPPTED" in os.environ:
            accepted_categories = os.environ['ENTITY_RECOGNITION_ACCEPPTED']
        categories_dict = dict(filter(lambda elem: elem[0] in accepted_categories, entity_recognition_mapping.items()))
        
        for doc in result:
            namedEntities = []
            if not doc.is_error:
                for entity in doc.entities:
                    if entity.category in categories_dict:
                        min_precision = 0
                        if 'minimumPrecision' in headers:
                            min_precision = float(headers['minimumPrecision'])
                        elif "MIN_PRECISION_ENTITY_RECOGNITION" in os.environ:
                            min_precision = float(os.environ["MIN_PRECISION_ENTITY_RECOGNITION"])
                        if entity.confidence_score < min_precision:
                            continue
                    
                        entity_extracted = {}
                        entity_extracted['category'] = entity.category
                        categories_dict[entity.category]['matched'].append(entity.text)
                        entity_extracted['subcategory'] = entity.subcategory
                        entity_extracted['length'] = entity.length
                        entity_extracted['offset'] = entity.offset
                        entity_extracted['confidenceScore'] = entity.confidence_score
                        entity_extracted['text'] = entity.text
                        namedEntities.append(entity_extracted)
                
                if len(namedEntities) > 0:
                    for category in  categories_dict:
                        document['data'][categories_dict[category]['name']] = categories_dict[category]['matched']
                    document['data']['namedEntities'] = namedEntities


            else:
                document['errors'] = [{"message": doc.error.code + ": " + doc.error.message}]


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
