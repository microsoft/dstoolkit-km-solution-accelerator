# -*- coding: utf-8 -*-
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import json
import azure.functions as func
from PIL import Image, ImageSequence
import PIL
import base64
from io import BytesIO
import io
import math
import os

# https://stackoverflow.com/questions/51152059/pillow-in-python-wont-let-me-open-image-exceeds-limit
# PIL.Image.MAX_IMAGE_PIXELS = 933120000
PIL.Image.MAX_IMAGE_PIXELS = None

# Azure Computer Vision OCR Minimum image size limits
MIN_WIDTH=50
MIN_HEIGHT=50
#
# Example: Azure Computer Vision OCR has a maximum image size limit of 10000x10000. 
#
MAX_WIDTH=10000
MAX_HEIGHT=10000
MAX_IMAGE_SIZE=50*1024*1024
IMAGE_RATIO_THRESHOLD=3
IMAGE_CROP_OVERLAP=50

DEFAULT_MIN_IMAGE_MODE = 'RGBA' # for color image “L” (luminance) for greyscale images, “RGB” for true color images, and “CMYK” for pre-press images.
DEFAULT_MIN_IMAGE_SIZE = (MIN_WIDTH, MIN_HEIGHT)
DEFAULT_MIN_IMAGE_COLOR = (255, 255, 255)

# os.environ.get('image_min_width',MIN_WIDTH)
# os.environ.get('image_min_height',MIN_HEIGHT)
# os.environ.get('image_max_width',MAX_WIDTH)
# os.environ.get('image_max_height',MAX_HEIGHT)

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
#
# PIL Conversion to PNG - Greyscale 
#
def img_to_base64_str_with_conversion(img):
    buffered = BytesIO()
    img = img.convert('L')
    img.save(buffered, format="PNG", optimize=True)
    buffered.seek(0)
    img_byte = buffered.getvalue()
    # img_file_size_png = buffered.tell()
    img_file_size_png = len(img_byte)
    return img_file_size_png, base64.b64encode(img_byte).decode()

def img_to_base64_str(img, target_format = "PNG"):
    if target_format in ["JPEG","JPG"]:
        if img.mode != 'RGB':
            img = img.convert('RGB')
    buffered = BytesIO()
    img.save(buffered, format=target_format, optimize=True)
    buffered.seek(0)
    img_byte = buffered.getvalue()
    img_file_size_png = len(img_byte)
    return img_file_size_png, base64.b64encode(img_byte).decode()


## Perform an operation on a record
def transform_value(record):
    try:
        recordId = record['recordId']
    except AssertionError  as error:
        return None

    # Validate the inputs
    try:
        document = {}
        document['recordId'] = recordId
        document['data'] = {}

        images=[]

        # Original Image Size
        size=(MAX_WIDTH,MAX_HEIGHT)
        image_string=record['data']["file_data"]["data"]

        contentType=record['data']["file_data"]['contentType']

        image = io.BytesIO(base64.b64decode(image_string))

        orig_image = Image.open(image)

        # Image Metadata
        #
        (imagew,imageh) = orig_image.size

        # Calculate Image Size Ratio
        if imagew>imageh:
            image_ratio = imagew/imageh
        else:
            image_ratio = imageh/imagew

        # Metadata handling
        document['data']['image_metadata']={}
        document['data']['image_metadata']['width']=imagew
        document['data']['image_metadata']['height']=imageh
        document['data']['image_metadata']['ratio']=image_ratio

        # Thumbnail of the original image
        tbimg = orig_image.copy()
        tbimg.thumbnail((400,400))
        _, document['data']['image_metadata']['thumbnail_medium'] = img_to_base64_str(tbimg,"JPEG")
        tbimg.thumbnail((100,100))
        _, document['data']['image_metadata']['thumbnail_small'] = img_to_base64_str(tbimg,"JPEG")
        del tbimg
        logging.info('Image small & medium thumbnails created.')

        # Normalization
        #
        # Iterate through all images including frames (TIFF)
        for j, read_image in enumerate(ImageSequence.Iterator(orig_image)):
            # For each image (TIFF multi page included) 
            (imagew,imageh) = read_image.size

            # Calculate Image Size Ratio
            if imagew>imageh:
                image_ratio = imagew/imageh
            else:
                image_ratio = imageh/imagew

            # Deal with small images
            if imagew<MIN_WIDTH or imageh<MIN_HEIGHT:
                logging.info(f'Small Image W {imagew} H {imageh} Size Ratio {image_ratio}')
                # paste the crop image into a minimal size image.
                minimg = Image.new(DEFAULT_MIN_IMAGE_MODE, (max(imagew,MIN_WIDTH),max(imageh,MIN_HEIGHT)), DEFAULT_MIN_IMAGE_COLOR)
                minimg.paste(read_image)
                image = {}
                image['minsize'] = True
                image['$type'] = "file"
                image['originalWidth'] = imagew
                image['originalHeight'] = imageh
                image['originalSizeRatio'] = image_ratio
                image['width'] = max(imagew,MIN_WIDTH)
                image['height'] = max(imageh,MIN_HEIGHT)
                if "contentType" in record['data']["file_data"]:
                    image['contentType'] = record['data']["file_data"]["contentType"]
                image['rotationFromOriginal']=0
                if "url" in record['data']["file_data"]:
                    image['url']=record['data']["file_data"]['url']
                image['size'], image['data'] = img_to_base64_str(minimg)
                
                images.append(image)

            # Oversized images
            elif imagew>MAX_WIDTH or imageh>MAX_HEIGHT or image_ratio > IMAGE_RATIO_THRESHOLD:
                logging.info(f'Frame {j} Image outside Max Width or Height or Size Ratio.{(imagew,imageh,image_ratio)}')
                # Crop is required here
                wcrops = math.floor(imagew/MAX_WIDTH)
                if imagew % MAX_WIDTH != 0:
                    wcrops+=1
                hcrops = math.floor(imageh/MAX_HEIGHT)
                if imageh % MAX_HEIGHT != 0:
                    hcrops+=1

                # Let's now look at the image ratio of each tuple w,h 
                cropslist=[]
                for wkey in range(wcrops):
                    cropwidth=min(MAX_WIDTH,imagew-(wkey*MAX_WIDTH))
                    for hkey in range(hcrops):
                        cropheight=min(MAX_HEIGHT,imageh-(hkey*MAX_HEIGHT))

                        if cropwidth>cropheight:
                            ratio = cropwidth/cropheight
                        else:
                            ratio = cropheight/cropwidth

                        if ratio==1 or ratio>IMAGE_RATIO_THRESHOLD:
                            # Now repeat similar crop based on ratio this time

                            if cropwidth==cropheight:
                                cropslist.append((wkey*MAX_WIDTH,hkey*MAX_HEIGHT,(wkey*MAX_WIDTH)+cropwidth,(hkey*MAX_HEIGHT)+cropheight))
                            elif cropwidth>cropheight:
                                # crop on width
                                cropslist.append((wkey*MAX_WIDTH,hkey*MAX_HEIGHT,(wkey*MAX_WIDTH)+cropwidth,(hkey*MAX_HEIGHT)+ min(math.ceil(cropwidth/IMAGE_RATIO_THRESHOLD),MAX_HEIGHT)))

                                # denominator=(cropheight*IMAGE_RATIO_THRESHOLD)
                                # cropsratio = math.floor(cropwidth/denominator)
                                # if cropwidth % denominator != 0:
                                #     cropsratio+=1
                                # for wkeyr in range(cropsratio):
                                #     # we move the width n times
                                #     w1=denominator*wkeyr
                                #     w2=min(cropwidth,denominator*(wkeyr+1))
                                #     if w1!=w2:
                                #         cropslist.append(((wkey*MAX_WIDTH)+w1,hkey*MAX_HEIGHT,(wkey*MAX_WIDTH)+w2,(hkey*MAX_HEIGHT)+cropheight))
                                #     else:
                                #         logging.info(f'Image crop has same width. Skipping {w1} {w2}')
                            else:
                                # Crop on height
                                cropslist.append((wkey*MAX_WIDTH,hkey*MAX_HEIGHT,(wkey*MAX_WIDTH)+min(math.ceil(cropheight/IMAGE_RATIO_THRESHOLD),MAX_WIDTH),(hkey*MAX_HEIGHT)+cropheight))

                                # denominator=(cropwidth*IMAGE_RATIO_THRESHOLD)
                                # cropsratio = math.floor(cropheight/denominator)
                                # # if cropheight % denominator < MIN_HEIGHT:
                                # #     merge_last_entry=True
                                # # else:
                                # #     merge_last_entry=False
                                # if cropheight % denominator != 0:
                                #     cropsratio+=1
                                # for wkeyr in range(cropsratio):
                                #     # we move the height n times
                                #     h1=denominator*wkeyr
                                #     h2=min(cropheight,denominator*(wkeyr+1))
                                #     if h1!=h2:
                                #         cropslist.append((wkey*MAX_WIDTH,(hkey*MAX_HEIGHT)+h1,(wkey*MAX_WIDTH)+cropwidth,(hkey*MAX_HEIGHT)+h2))
                                #     else:
                                #         logging.info(f'Image crop has same height. Skipping {h1} {h2}')
                        else:
                            cropslist.append((wkey*MAX_WIDTH,hkey*MAX_HEIGHT,(wkey*MAX_WIDTH)+cropwidth,(hkey*MAX_HEIGHT)+cropheight))

                logging.info(f'Image crops list {cropslist}')

                for (idx,boundingbox) in enumerate(cropslist):
                    logging.info(f'Image normalized {idx} {boundingbox}')
                    image = {}
                    image['$type'] = "file"
                    image['originalWidth'] = imagew
                    image['originalHeight'] = imageh
                    image['originalSizeRatio'] = image_ratio

                    cropwidth=boundingbox[2]-boundingbox[0]
                    cropheight=boundingbox[3]-boundingbox[1]

                    # Documentation crop((x, y, x + width, y + height))
                    crop_image = read_image.crop(box=boundingbox)

                    if cropwidth<MIN_WIDTH or cropheight<MIN_HEIGHT:
                        # paste the crop image into a minimal size image.
                        minimg = Image.new(DEFAULT_MIN_IMAGE_MODE, (max(cropwidth,MIN_WIDTH),max(cropheight,MIN_HEIGHT)), DEFAULT_MIN_IMAGE_COLOR)
                        minimg.paste(crop_image.resize((max(cropwidth,MIN_WIDTH),max(cropheight,MIN_HEIGHT))), (0,0))
                        crop_image=minimg
                        image['minsize'] = True

                    image['frame'] = j
                    image['idx'] = idx
                    image['width'] = crop_image.size[0]
                    image['height'] = crop_image.size[1]
                    image['contentType'] = "image/jpeg"
                    image['rotationFromOriginal']=0
                    image['url']=record['data']["file_data"]['url']
                    image['size'], image['data'] = img_to_base64_str(crop_image,"JPEG")

                    images.append(image)
            else:
                logging.info(f'Frame {j} Image W {imagew} H {imageh} Size Ratio {image_ratio}')
                image = {}
                image['$type'] = "file"
                image['frame'] = j
                image['originalWidth'] = imagew
                image['originalHeight'] = imageh
                image['originalSizeRatio'] = image_ratio
                image['width'] = imagew
                image['height'] = imageh
                if "contentType" in record['data']["file_data"]:
                    image['contentType'] = record['data']["file_data"]["contentType"]
                image['rotationFromOriginal']=0
                if "url" in record['data']["file_data"]:
                    image['url']=record['data']["file_data"]['url']
                image['size'], image['data'] = img_to_base64_str(read_image)
                
                images.append(image)

        document['data']['normalized_images'] = images

        # This should be a configuration
        if contentType in ['image/tiff','image/tiff-fx']:
            document['data']['image_metadata']['image_data'] = images[0]['data']

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
