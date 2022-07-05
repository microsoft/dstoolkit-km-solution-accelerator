// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System;

namespace Knowledge.Services.AzureStorage
{
    public interface IStorageService
    {
        public BlobContainerClient GetContainerClient(string containerName);
        public BlobSasQueryParameters GetServiceSasUriForContainer(BlobContainerClient containerClient,
                                                          string storedPolicyName = null);
        public Uri GetServiceSasUriForBlob(BlobClient blobClient,
                    string storedPolicyName = null);

        public StorageSharedKeyCredential GetStorageSharedKeyCredential();
    }
}