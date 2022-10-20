# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import io
import logging
import azure.functions as func
import time
import json
from azure.cognitiveservices.vision.computervision import ComputerVisionClient
from azure.cognitiveservices.vision.computervision.models import VisualFeatureTypes, Details, ComputerVisionErrorResponseException
from msrest.authentication import CognitiveServicesCredentials
import base64
from PIL import Image
from io import BytesIO
import tempfile

import os

endpoint = os.environ["COMPUTER_VISION_ENDPOINT"]
key = os.environ["COMPUTER_VISION_KEY"]

credentials = CognitiveServicesCredentials(key)
computer_vision_client = ComputerVisionClient(
    endpoint=endpoint,
    credentials=credentials
)

visualFeatures = {
    'adult': VisualFeatureTypes.adult, 
    'brands': VisualFeatureTypes.brands, 
    'categories': VisualFeatureTypes.categories, 
    'description': VisualFeatureTypes.description, 
    'faces': VisualFeatureTypes.faces, 
    'objects': VisualFeatureTypes.objects, 
    'tags': VisualFeatureTypes.tags
    }

detailsDict = {
    # 'celebrities': Details.celebrities, 
    'landmarks': Details.landmarks
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

        image_features=[VisualFeatureTypes.image_type, VisualFeatureTypes.color]

        # visualFeatureList = visualFeatureList.split(',')
        if 'visualFeatures' in headers:
            visualFeatureList = headers['visualFeatures'].replace('[', '').replace(']', '').replace('\'', '').replace('\"', '').replace(' ', '').split(',')
            for visualFeature in visualFeatureList:
                image_features.append(visualFeatures[str(visualFeature)])
        else:
            image_features=[VisualFeatureTypes.image_type,VisualFeatureTypes.faces,VisualFeatureTypes.categories,
        VisualFeatureTypes.color,VisualFeatureTypes.tags,VisualFeatureTypes.description,VisualFeatureTypes.objects,VisualFeatureTypes.brands]
        details = [Details.landmarks]

        details = []
        if 'details' in headers:
            detailsList = headers['details'].replace('[', '').replace(']', '').replace('\'', '').replace('\"', '').replace(' ', '').split(',')
            for detail in detailsList:
                details.append(detailsDict[str(detail)])
        else:
            details = [Details.landmarks]

        # image_type = "ImageType"
        # faces = "Faces"
        # adult = "Adult"
        # categories = "Categories"
        # color = "Color"
        # tags = "Tags"
        # description = "Description"
        # objects = "Objects"
        # brands = "Brands"
        if 'imgUrl' in data:
            image_url = data["imgUrl"]  + data["imgSasToken"]
            print(image_url)
            # SDK call
            if "defaultLanguageCode" in headers: 
                rawHttpResponse = computer_vision_client.analyze_image(image_url,visual_features=image_features, details=details, raw=True, language=headers['defaultLanguageCode'])
            else:
                rawHttpResponse = computer_vision_client.analyze_image(image_url,visual_features=image_features, details=details, raw=True)

            document['data']=json.loads(rawHttpResponse.response.content)

        elif 'file_data' in data:
            base64Image = base64.b64decode(data["file_data"]["data"])
            img_stream=io.BytesIO(base64Image)
            img_byte = get_mb_dimension(img_stream)
            if img_byte > 4*1024*1024:
                im = convert_bytesIO_to_Pillow(img_stream)
                im = im.convert('L')
                img_stream = img_to_bytesIO(im)
                img_byte = get_mb_dimension(img_stream)
                #img_byte, im = get_mb_dimension(img_stream)

            # img_stream=io.BytesIO(base64.b64decode(data["file_data"]["data"]))
            # kb_size = img_stream.tell()
            if "defaultLanguageCode" in headers: 
                rawHttpResponse = computer_vision_client.analyze_image_in_stream(img_stream,visual_features=image_features, details=details, raw=True, language=headers['defaultLanguageCode'])
            else:
                rawHttpResponse = computer_vision_client.analyze_image_in_stream(img_stream,visual_features=image_features, details=details, raw=True)

            document['data']=json.loads(rawHttpResponse.response.content)

        else:
            # Return empty response
            document['data']=''
            document['warnings'].append({ "message": "No image data found."})

    except ComputerVisionErrorResponseException as error:
        return(
            {
            "recordId": recordId,
            "errors": [ { "message": "KeyError:" + error.args[0] }   ]       
            })

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

def convert_bytesIO_to_Pillow(img_stream):
    im = Image.open(img_stream)
    return im
def get_mb_dimension(img_stream): 
    img_stream.seek(0)
    return len(img_stream.getvalue())

def img_to_bytesIO(img, target_format = "JPEG"):
    buffered = BytesIO()
    img.save(buffered, format=target_format, optimize=True)
    buffered.seek(0)
    img_byte = buffered.getvalue()
    base64Image = base64.b64encode(img_byte)
    return io.BytesIO(base64.b64decode(base64Image))
