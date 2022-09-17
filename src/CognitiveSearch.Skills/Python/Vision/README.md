# Table of Functions/Skills 

- [Analyze Document](#analyze-document)
- [Analyze Image](#analyze-image)  
- [Analyze Image per Domain](#analyze-image-domain)
- [Describe Image](#describe-image)
- [Image Normalization](#image-normalization)
- [Read (OCR)](#read)
- [OCRLayout for reading order](#ocrlayout)


# Analyze Document

This function is targeting [Azure Applied AI Service - Form Recognizer](https://docs.microsoft.com/en-us/azure/applied-ai-services/form-recognizer/overview?tabs=v3-0)

It uses the layout model to get the tabulars information out of each page/slide/image.

_Skill definition_
```json
{
    "@odata.type": "#Microsoft.Skills.Custom.WebApiSkill",
    "name": "TablesExtraction",
    "description": "Extracts fields from a form using a pre-trained form recognition model",
    "uri": "{{param.vision.AnalyzeDocument}}",
    "httpMethod": "POST",
    "timeout": "PT3M",
    "context": "/document",
    "batchSize": 1,
    "inputs": [
        {
            "name": "formUrl",
            "source": "/document/metadata_storage_path"
        },
        {
            "name": "formSasToken",
            "source": "/document/metadata_storage_sas_token"
        }
    ],
    "outputs": [
        {
            "name": "tables",
            "targetName": "tables"
        },
        {
            "name": "tables_count",
            "targetName": "tables_count"
        }
    ]
},
```


# Analyze Image

This function is targeting [Azure Computer Vision Service - Image Analysis](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/overview-image-analysis)

__The Computer Vision Image Analysis service can extract a wide variety of visual features from your images. For example, it can determine whether an image contains adult content, find specific brands or objects, or find human faces.__

_About this skill_

This endpoint is used to call the Image Analysis using the Computer Vision Azure service. To start the function you need to do a POST request to the endpoint. The Image Analysis skill extracts a rich set of visual features based on the image content. For example, you can generate a caption from an image, generate tags, or identify celebrities and landmarks. 

The structure of the request is the following:

```https
content-type: application/json
defaultLanguageCode: A string indicating the language to return. The service returns recognition results in a specified language. If this parameter is not specified, the default value is "en".
visualFeatures: An array of strings indicating the visual feature types to return. Valid visual feature types include:
- adult - detects if the image is pornographic in nature (depicts nudity or a sex act), or is gory (depicts extreme violence or blood). Sexually suggestive content (also known as racy content) is also detected.
- brands - detects various brands within an image, including the approximate location. The brands visual feature is only available in English.
- categories - categorizes image content according to a taxonomy defined in the Cognitive Services Computer Vision documentation.
- description - describes the image content with a complete sentence in supported languages.
- faces - detects if faces are present. If present, generates coordinates, gender and age.
- objects - detects various objects within an image, including the approximate location. The objects visual feature is only available in English.
- tags - tags the image with a detailed list of words related to the image content.
    Names of visual features are case-sensitive. Note that the color and imageType visual features have been deprecated, but this functionality could still be accessed via a custom skill.
details: An array of strings indicating which domain-specific details to return. Valid visual feature types include:
- celebrities - identifies celebrities if detected in the image.
- landmarks - identifies landmarks if detected in the image.

{
    "values": [
        {
            "recordId": "1",
            "data": {
                "file_data": {
                    "$type": 
                    "url":
                    "data":  
                }
            }
        },
        {
            "recordId": "2",
            "data": {
                "imgUrl": URL of the image to analyze
                "imgSaSToken": 
            }
        }
    ]
}
```

The output depends on the ```visualFeatures``` and ```details``` provided:
- ```adult``` detects if the image is pornographic in nature (depicts nudity or a sex act), or is gory (depicts extreme violence or blood). Sexually suggestive content (also known as racy content) is also detected.
- ```brands```    detects various brands within an image, including the approximate location. The brands visual feature is only available in English.
- ```categories```    categorizes image content according to a taxonomy defined in the Cognitive Services Computer Vision documentation.
- ```description```   describes the image content with a complete sentence in supported languages.
- ```faces``` detects if faces are present. If present, generates coordinates, gender and age.
- ```objects```   detects various objects within an image, including the approximate location. The objects visual feature is only available in English.
- ```tags```  tags the image with a detailed list of words related to the image content.
- ```celebrities``` identifies celebrities if detected in the image.
- ```landmarks```   identifies landmarks if detected in the image.


_Skill definition_

```json
{
    "@odata.type": "#Microsoft.Skills.Custom.WebApiSkill",
    "name": "ImageAnalysis",
    "description": "Extract Image Analysis.",
    "uri": "{{param.vision.Analyze}}",
    "context": "/document/normalized_images/*",
    "httpMethod": "POST",
    "timeout": "PT3M",
    "batchSize": 1,
    "degreeOfParallelism": 2,
    "inputs": [
        {
            "name": "file_data",
            "source": "/document/normalized_images/*"
        }
    ],
    "outputs": [
        {
            "name": "categories",
            "targetName": "raw_categories"
        },
        {
            "name": "tags",
            "targetName": "raw_tags"
        },
        {
            "name": "description",
            "targetName": "raw_description"
        },
        {
            "name": "faces",
            "targetName": "raw_faces"
        },
        {
            "name": "brands",
            "targetName": "raw_brands"
        },
        {
            "name": "objects",
            "targetName": "raw_objects"
        }
    ],
    "httpHeaders": {
        "defaultLanguageCode": "en"
    }
},
```

# Analyze Image Domain

This function is targeting [Azure Computer Vision Service - Image Analysis - Domain Specific](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/concept-detecting-domain-content).

__In addition to tagging and high-level categorization, Computer Vision also supports further domain-specific analysis using models that have been trained on specialized data. There are two ways to use the domain-specific models: by themselves (scoped analysis) or as an enhancement to the categorization feature.__

|Name|	Description|
|--|--|
|celebrities|	Celebrity recognition, supported for images classified in the people_ category|
|landmarks|	Landmark recognition, supported for images classified in the outdoor_ or building_ categories|

For simplicity and scalability, we use the [Enhanced categorization analysis](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/concept-detecting-domain-content#enhanced-categorization-analysis) to extract landmarks and celebrities.

While we provide the skill as separated function for convenience, image description feature is part of the [Image Analysis](#image-analysis) output. 

# Describe Image

This function is targeting [Azure Computer Vision Service - Image Description](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/concept-describing-images).

__Computer Vision can analyze an image and generate a human-readable phrase that describes its contents. The algorithm returns several descriptions based on different visual features, and each description is given a confidence score. The final output is a list of descriptions ordered from highest to lowest confidence.__

__At this time, English is the only supported language for image description.__

While we provide the skill as separated function for convenience, image description feature is part of the [Image Analysis](#image-analysis) output. 

# Image Normalization 

This custom function aims to normalize the size of an image so it could fit into a normal image processing flow. 

Some cognitive services have limitation in terms of image size and dimensions. ACS also has its own limitation i.e. TIFF 

In order to have better completeness on images processing, we developed an image normalizer skill to tackle most common cases 

- Image dimensions over 10Kx10K are split into multiple images (cropped)
- TIFF multipage support
- Small image are resized to fit minimum computer vision requirement
- Small (100x100) & Medium (400x400) thumbnails generation
    - Medium thumbnail is used for pages/slides overview and document cover.

_Skill definition_

```json
{
    "@odata.type": "#Microsoft.Skills.Custom.WebApiSkill",
    "name": "ImageNormalization",
    "description": "Workaround TIF/TIFF issue in Azure Cognitive Search",
    "context": "/document",
    "uri": "{{param.vision.Normalize}}",
    "httpMethod": "POST",
    "timeout": "PT3M",
    "batchSize": 1,
    "degreeOfParallelism": 3,
    "inputs": [
        {
            "name": "file_data",
            "source": "/document/file_data"
        }
    ],
    "outputs": [
        {
            "name": "image_metadata",
            "targetName": "image_metadata"
        },
        {
            "name": "normalized_images",
            "targetName": "normalized_images"
        }
    ],
    "httpHeaders": {}
},
```

# Read

This function is targeting [Azure Applied AI Service - OCR](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/overview-ocr#read-api)

By default, the service will use the latest generally available (GA) model to extract text.

_Skill definition_

```json
{
    "@odata.type": "#Microsoft.Skills.Custom.WebApiSkill",
    "name": "OcrSkill",
    "uri": "{{param.vision.Read}}",
    "context": "/document/normalized_images/*",
    "httpMethod": "POST",
    "timeout": "PT3M",
    "batchSize": 1,
    "degreeOfParallelism": 2,
    "inputs": [
        {
            "name": "file_data",
            "source": "/document/normalized_images/*"
        }
    ],
    "outputs": [
        {
            "name": "read",
            "targetName": "ocrlayout"
        }
    ],
    "httpHeaders": {
        "lineEnding": "LineFeed",
        "defaultLanguageCode": "en",
        "detectOrientation": "true"
    }
},
```

# OCRLayout

This function is needed as an attempt to enforce a reading order in any OCR output. 

More details on the [ocrlayout](https://puthurr.github.io/ocrlayout/) purpose. 

_Skill definition_

```json
{
    "@odata.type": "#Microsoft.Skills.Custom.WebApiSkill",
    "name": "ocrlayout",
    "description": "Invoke ocrlayout to re-order the text out of OCR",
    "context": "/document/normalized_images/*",
    "uri": "{{param.vision.azureocrlayout}}",
    "httpMethod": "POST",
    "timeout": "PT1M",
    "batchSize": 5,
    "degreeOfParallelism": null,
    "inputs": [
        {
            "name": "ocrlayout",
            "source": "/document/normalized_images/*/ocrlayout"
        }
    ],
    "outputs": [
        {
            "name": "text",
            "targetName": "ocrlayoutText"
        }
    ],
    "httpHeaders": {}
},
```
