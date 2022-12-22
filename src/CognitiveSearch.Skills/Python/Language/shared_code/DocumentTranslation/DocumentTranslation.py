# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import os

from datetime import datetime, timedelta
from azure.core.credentials import AzureKeyCredential
from azure.ai.translation.document import DocumentTranslationClient
from azure.storage.blob import BlobClient,generate_blob_sas, generate_container_sas,BlobSasPermissions,ContainerSasPermissions, ContentSettings, BlobProperties

endpoint = os.environ["DOCUMENT_TRANSLATION_ENDPOINT"]
subscription_key = os.environ["DOCUMENT_TRANSLATION_KEY"]
target_language = os.environ["DOCUMENT_TRANSLATION_LANGUAGE"]

credential = AzureKeyCredential(subscription_key)

document_translation_client = DocumentTranslationClient(endpoint, credential)

available_language = ["af", "sq", "am", "ar", "hy", "as", "az", "bn", "ba", "bs", "bg","yue", "ca",
"zh", "zh_chs", "zh_cht", "lzh", "zh-Hans", "zh-Hant", 
"hr", "cs", "da", "prs", "dv", "nl", "en","et", "fj", "fil", "fi",
"fr", "fr-ca", "ka", "de", "el", "gu", "ht", "he", "hi", "mww",
"hu", "is", "id", "iu", "ga", "it", "ja", "kn", "kk", "km", "tlh-Latn", "tlh-Piqd", "ko",
"ku", "kmr", "ky", "lo", "lv", "lt", "mk", "mg", "ms", "ml", "mt", "mi", "mr", "mn-Cyrl",
"mn-Mong", "my", "ne", "nb", "or", "ps", "fa", "pl", "pt", "pt-pt", "pa", "otq", "ro", "ru",
"sm", "sr-Cyrl", "sr-Latn", "sk", "sl", "es", "sw", "sv", "ty", "ta", "tt", "te", "th", "bo",
"ti", "to", "tr", "tk", "uk", "ur", "ug", "uz", "vi", "cy", "yua"]

supported_extension = [".pdf", ".csv", ".html",".htm", ".xlf", ".markdown",".mdown",".mkdn",".md",".mkd",".mdwn",".mdtxt",".mdtext",".rmd",
".mthml",".mht",".xls",".xlsx",".msg",".ppt",".pptx",".doc",".docx",".odt",".odp",".ods",".rtf",".tsv",".txt"]

#
# Azure Blob Storage for Document Translated outputs
#
blob_storage_integration=False

# Where the translated documents will be put
TRANSLATION_CONTAINER = 'translation'

if 'StorageAccountName' in os.environ:
    STORAGE_ACCOUNT_NAME=os.environ['StorageAccountName']
    STORAGE_ACCOUNT_KEY=os.environ['StorageKey']
    blob_storage_integration=True


def get_blob_sas_source(container_name, blob_name):
    sas_token = generate_blob_sas(account_name=STORAGE_ACCOUNT_NAME, 
                                container_name=container_name,
                                blob_name=blob_name,
                                account_key=STORAGE_ACCOUNT_KEY,
                                permission=BlobSasPermissions(read=True,list=True),
                                expiry=datetime.utcnow() + timedelta(hours=1))
    return '?'+sas_token

def get_blob_sas_target(container_name, blob_name):
    sas_token = generate_blob_sas(account_name=STORAGE_ACCOUNT_NAME, 
                                container_name=container_name,
                                blob_name=blob_name,
                                account_key=STORAGE_ACCOUNT_KEY,
                                permission=BlobSasPermissions(write=True,list=True),
                                expiry=datetime.utcnow() + timedelta(hours=1))
    return '?'+sas_token

def get_container_sas(container_name):
    sas_token = generate_container_sas(account_name=STORAGE_ACCOUNT_NAME, 
                                container_name=container_name,
                                account_key=STORAGE_ACCOUNT_KEY,
                                permission=ContainerSasPermissions(write=True,list=True,read=True),
                                expiry=datetime.utcnow() + timedelta(hours=1))
    return '?'+sas_token

## Perform an operation on a record
def transform_value(record, poll=False, simulation=False):
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
        document['errors'] = []

        assert ('data' in record), "'data' field is required."
        data = record['data']

        fromLanguageCode = 'en'
        if "fromLanguageCode" in data and data["fromLanguageCode"] in available_language:
            fromLanguageCode = data["fromLanguageCode"]

        #
        # Chinese language code normalization
        # https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/language-detection/language-support
        # vs
        # https://learn.microsoft.com/en-us/azure/cognitive-services/translator/language-support
        #
        if fromLanguageCode == 'zh':
            fromLanguageCode = 'lzh'
        if fromLanguageCode == 'zh_chs':
            fromLanguageCode = 'zh-Hans'
        if fromLanguageCode == 'zh_cht':
            fromLanguageCode = 'zh-Hant'

        document['data']['translatedFromLanguageCode'] = fromLanguageCode
        document['data']['translatedToLanguageCode'] = target_language

        document['data']['document_translated'] = False
        document['data']['document_translatable'] = False

        if blob_storage_integration:
            # Supported Extension
            fileExtension = None
            if "fileExtension" in data and data["fileExtension"] in supported_extension:
                fileExtension = data["fileExtension"]

            if fileExtension is not None:
                if "source_blob_url" in data:
                    source_blob_url=data['source_blob_url']

                    blob_client = BlobClient.from_blob_url(blob_url=source_blob_url)

                    if blob_client.container_name != TRANSLATION_CONTAINER:
                        if fromLanguageCode != target_language:
                            if not simulation:
                                # Translation require
                                source_blob_url_with_sas_token = source_blob_url + get_blob_sas_source(blob_client.container_name,blob_client.blob_name)

                                target_url = blob_client.scheme +'://'+blob_client.primary_hostname+'/'+TRANSLATION_CONTAINER+'/'+blob_client.container_name+"/"+blob_client.blob_name
                                # target_url_with_sas_token=target_url+get_blob_sas_target(TRANSLATION_CONTAINER,blob_client.container_name+"/"+blob_client.blob_name)
                                target_url_with_sas_token=target_url+get_container_sas(TRANSLATION_CONTAINER)

                                poller = document_translation_client.begin_translation(source_blob_url_with_sas_token, target_url_with_sas_token, target_language, storage_type="File")
                                document['data']['translation_opid'] = poller.id

                                if poll:
                                    translation_result = poller.result()

                                    for docresult in translation_result:
                                        logging.info(f"Document ID: {docresult.id}")
                                        logging.info(f"Document status: {docresult.status}")
                                        if docresult.status == "Succeeded":
                                            logging.info(f"Source document location: {docresult.source_document_url}")
                                            logging.info(f"Translated document location: {docresult.translated_document_url}")
                                            logging.info(f"Translated to language: {docresult.translated_to}\n")

                                            # Update the content type and language 
                                            if 'contentType' in data:
                                                blob_target = BlobClient.from_blob_url(target_url_with_sas_token)
                                                blobprops = blob_target.get_blob_properties()
                                                cnt_settings = blobprops.content_settings
                                                cnt_settings.content_type = data['contentType']
                                                cnt_settings.content_language = target_language
                                                blob_target.set_http_headers(cnt_settings)
                                        else:
                                            warning_message=f"Error Code: {docresult.error.code}, Message: {docresult.error.message}"
                                            document['warnings'] = [ { "message": warning_message } ]
                                            logging.error(f"Error Code: {warning_message}")

                            document['data']['document_translatable'] = True
                    else:
                        document['data']['document_translated'] = True
                else:
                    document['warnings'] = [ { "message": f"Source url and/or sas token not provided." } ]
            else:
                document['warnings'] = [ { "message": f"Unsupported File Extension." } ]
        else:
            document['warnings'] = [ { "message": f"Translation storage not set." } ]

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