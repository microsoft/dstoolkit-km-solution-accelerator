import io
import logging
import json
import os
import logging
import datetime
from json import JSONEncoder
from azure.storage.blob import BlobClient

#
# Azure Blob Storage for response persistence 
#
IMAGES_CONTAINER_NAME = os.getenv("IMAGES_STORAGE_CONTAINER_NAME", "images")

metadata_storage_integration=False
blob_conn_string = None

if ('METADATA_STORAGE_CONNECTION_STRING' in os.environ):
    # Retrieve the connection string from an environment variable. Note that a connection
    # string grants all permissions to the caller, making it less secure than obtaining a
    # BlobClient object using credentials.
    blob_conn_string = os.environ["METADATA_STORAGE_CONNECTION_STRING"]
    blob_metadata_container = os.getenv("METADATA_STORAGE_CONTAINER_NAME", "metadata")
    metadata_storage_integration=True

vector_storage_integration=False
if ('VECTOR_STORAGE_CONNECTION_STRING' in os.environ):
    # Retrieve the connection string from an environment variable. Note that a connection
    # string grants all permissions to the caller, making it less secure than obtaining a
    # BlobClient object using credentials.
    blob_vector_conn_string = os.environ["VECTOR_STORAGE_CONNECTION_STRING"]
    blob_vector_container = os.getenv("VECTOR_STORAGE_CONTAINER_NAME", "vectors")
    vector_storage_integration=True


class StorageUtils:

    def is_metadata_storage_enabled():
        return metadata_storage_integration
    # 
    # Save the output to metadata container
    #
    def persist_object(blobname, suffix, result):

        if metadata_storage_integration:
            # Save the pages output to metadata for latter use
            blob_client = BlobClient.from_connection_string(blob_conn_string,container_name=blob_metadata_container, blob_name=blobname+suffix)
            data = json.dumps(result, ensure_ascii=False, cls=DateTimeEncoder)
            blob_client.upload_blob(data=data,overwrite=True)

    def persist_text(blobname, suffix, data):

        if metadata_storage_integration:
            # Save the pages output to metadata for latter use
            blob_client = BlobClient.from_connection_string(blob_conn_string,container_name=blob_metadata_container, blob_name=blobname+suffix)
            blob_client.upload_blob(data=data,overwrite=True)

    def getTargetUrl(base_url):
        target_blobname=None
        if metadata_storage_integration:
            # Get a blob client from
            blob_client = BlobClient.from_blob_url(blob_url=base_url)
            if blob_client.container_name == IMAGES_CONTAINER_NAME:
                target_blobname = blob_client.blob_name
            else:
                target_blobname = blob_client.container_name+"/"+blob_client.blob_name
        return target_blobname

    # Vector storage
    def persist_vector_object(base_url, suffix, result):
        if vector_storage_integration:
            blobname = StorageUtils.getVectorTargetUrl(base_url)
            # Save the pages output to metadata for latter use
            vector_blob_client = BlobClient.from_connection_string(blob_vector_conn_string,container_name=blob_vector_container, blob_name=blobname+suffix)
            data = json.dumps(result, ensure_ascii=False, cls=DateTimeEncoder)
            vector_blob_client.upload_blob(data=data,overwrite=True)

    def getVectorTargetUrl(base_url):
        target_blobname=None
        if metadata_storage_integration:
            # Get a blob client from
            blob_client = BlobClient.from_blob_url(blob_url=base_url)
            target_blobname = blob_client.blob_name
        return target_blobname
            
class DateTimeEncoder(JSONEncoder):
        #Override the default method
        def default(self, obj):
            if isinstance(obj, (datetime.date, datetime.datetime)):
                return obj.isoformat()
