# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import io
import logging
import azure.functions as func
import time
import json
from azure.cognitiveservices.vision.computervision import ComputerVisionClient
from msrest.authentication import CognitiveServicesCredentials
import base64
from io import BytesIO
import os

endpoint = os.environ["COMPUTER_VISION_ENDPOINT"]
key = os.environ["COMPUTER_VISION_KEY"]

credentials = CognitiveServicesCredentials(key)
computer_vision_client = ComputerVisionClient(
    endpoint=endpoint,
    credentials=credentials
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

    for value in values:
        output_record = transform_value(headers, value)
        if output_record != None:
            results["values"].append(output_record)

    return json.dumps(results, ensure_ascii=False)        

## Perform an operation on a record
def transform_value(headers, record):
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

        if 'domain' in data:
            domain=data['domain']
            if 'language' in data:
                language=data['language']
            else:
                language="en"
            if 'imgUrl' in data:
                image_url = data["imgUrl"]  + data["imgSasToken"]
                print(image_url)
                # SDK call
                rawHttpResponse = computer_vision_client.analyze_image_by_domain(domain,image_url,language=language,raw=True)
            elif 'file_data' in data:
                img_stream=io.BytesIO(base64.b64decode(data["file_data"]["data"]))
                rawHttpResponse = computer_vision_client.analyze_image_by_domain_in_stream(domain,img_stream,language=language,raw=True)

            document['data'][domain]=json.loads(rawHttpResponse.response.content)
    # except HTTPFailure as e:
    #     return (
    #         {
    #         "recordId": recordId,
    #         "errors": [ { "message": "HTTPFailure:" + e.status_code }   ]
    #         })
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
