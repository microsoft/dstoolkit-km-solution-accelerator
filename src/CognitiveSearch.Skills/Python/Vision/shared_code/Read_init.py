# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import io
import logging
import azure.functions as func
import time
import json
from azure.cognitiveservices.vision.computervision import ComputerVisionClient
from azure.cognitiveservices.vision.computervision.models import VisualFeatureTypes, OperationStatusCodes
from msrest.authentication import CognitiveServicesCredentials
import base64
from io import BytesIO
import os

from .Utils import StorageUtils, DateTimeEncoder

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

        raw = True
        numberOfCharsInOperationId = 36

        if 'language' in data:
            language=data['language']
        else:
            language=None

        rawHttpResponse=None
        
        if 'imgUrl' in data and 'imgSasToken' in data:        
            image_url = data["imgUrl"]  + data["imgSasToken"]
            print(image_url)
            # SDK call
            if language:
                rawHttpResponse = computer_vision_client.read(image_url, language=language, raw=True)
            else:
                rawHttpResponse = computer_vision_client.read(image_url, raw=True)
        elif 'file_data' in data:
            img_stream=io.BytesIO(base64.b64decode(data["file_data"]["data"]))
            if language:
                rawHttpResponse = computer_vision_client.read_in_stream(img_stream, language=language, raw=True)
            else:
                rawHttpResponse = computer_vision_client.read_in_stream(img_stream, raw=True)

        if rawHttpResponse:
            # Get ID from returned headers
            operationLocation = rawHttpResponse.headers["Operation-Location"]
            idLocation = len(operationLocation) - numberOfCharsInOperationId
            operationId = operationLocation[idLocation:]
            logging.info(f'Read operation id {str(operationId)}')

            while True:
                time.sleep(5)
                # SDK call
                result = computer_vision_client.get_read_result(operationId, raw=True)

                # Get data
                if result.output.status == OperationStatusCodes.succeeded:
                    document['data']['read']=json.loads(result.response.content)
                    break

                if result.output.status == OperationStatusCodes.failed:
                    document['warnings'].append({ "message": "Error:" + str(result.output.status) })
                    break
        else:
            # Return empty response
            document['data']=''
            document['warnings'].append({ "message": "No image data found."})

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
