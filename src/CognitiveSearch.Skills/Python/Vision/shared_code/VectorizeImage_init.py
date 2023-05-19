# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

# Image Vectorization - PREVIEW 4.0
# https://github.com/Azure/cognitive-search-vector-pr
# 
# https://learn.microsoft.com/en-us/azure/cognitive-services/computer-vision/how-to/image-retrieval
# 
# Text Vectorization is done with OpenAI
#

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
import requests
from .Utils import StorageUtils

# Environment variables
endpoint = os.environ["COMPUTER_VISION_ENDPOINT"]
key = os.environ["COMPUTER_VISION_KEY"]

version = os.getenv("COMPUTER_VISION_API_VERSION", "2023-02-01-preview")
model = os.getenv("COMPUTER_VISION_API_MODEL", "latest")

# SDK Computer Vision client
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

        if 'imgUrl' in data and 'imgSasToken' in data:
            if len(data['imgUrl']) > 0:
                image_url = data["imgUrl"]  + data["imgSasToken"]
                print(image_url)
            
                path = f'/computervision/retrieval:vectorizeImage'
                constructed_url = endpoint + path
                # curl.exe -v -X POST "https://<endpoint>/computervision/retrieval:vectorizeImage?api-version=2023-02-01-preview&modelVersion=latest" 
                # -H "Content-Type: application/json" -H "Ocp-Apim-Subscription-Key: <subscription-key>" --data-ascii "
                # {
                # 'url':'https://learn.microsoft.com/azure/cognitive-services/computer-vision/media/quickstarts/presentation.png'
                # }"

                request_vision = {
                    'url': image_url
                }
                params_vision = {
                    'api-version': version,
                    'modelVersion': model
                }
                headers_vision = {
                    'Ocp-Apim-Subscription-Key': key,
                    'Content-type': 'application/json; charset=UTF-8'
                }
                response = requests.post(constructed_url, params=params_vision, headers=headers_vision, json=request_vision)
                response_content = response.json()

                if response.status_code == requests.codes.ok:
                    document['data']=response_content

                    # Sink it to the Vectorization Index
                    StorageUtils.persist_vector_object(data["imgUrl"], '.json',document)

                elif response.status_code == requests.codes.too_many_requests:
                    document['warnings'].append({ "message": "Error:" + str(response.status_code) })
                else:
                    document['warnings'].append({ "message": "Error:" + str(response.status_code) })
            else:
                # Return empty response
                document['data']=''
                document['warnings'].append({ "message": "Empty url(s) or Sas token found.)"})
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
