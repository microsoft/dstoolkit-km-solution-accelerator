# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import json
import azure.functions as func
from ocrlayout.bboxhelper import BBoxHelper

from .Utils import StorageUtils, DateTimeEncoder

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
        result = compose_response(body)
        return func.HttpResponse(result, mimetype="application/json")
    else:
        return func.HttpResponse(
             "Invalid body",
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
    return json.dumps(results, ensure_ascii=False)        


## Perform an operation on a record
def transform_value(record):
    try:
        recordId = record['recordId']
    except AssertionError  as error:
        return None

    # Validate the inputs
    try:
        bboxrequest=record['data']["ocrlayout"]
        bboxresponse=BBoxHelper(verbose=False).processAzureOCRResponse(bboxrequest)

        document = {}
        document['recordId'] = recordId
        document['data'] = {}
        document['data']['text'] = bboxresponse.text
    except KeyError as error:
        return (
            {
                "recordId": recordId,
                "data":{
                    "text":""
                },
                "warnings": [ { "message": "KeyError:" + error.args[0] }   ]       
            })
    except AssertionError as error:
        return (
            {
                "recordId": recordId,
                "data":{
                    "text":""
                },
                "warnings": [ { "message": "AssertionError:" + error.args[0] }   ]       
            })

    return (document)
