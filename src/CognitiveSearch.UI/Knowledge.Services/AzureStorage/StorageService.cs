// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Sas;
using Microsoft.ApplicationInsights;
using System;

namespace Knowledge.Services.AzureStorage
{
    public class StorageService : AbstractService, IStorageService
    {
        public StorageConfig storageConfig;

        public StorageService(TelemetryClient telemetry, StorageConfig storageCfg)
        {
            this.storageConfig = storageCfg;
            this.telemetryClient = telemetry;
        }

        public BlobContainerClient GetContainerClient(string containerName)
        {
            return new BlobContainerClient(new Uri($"https://{storageConfig.StorageAccountName}.blob.core.windows.net/{containerName}"),
                new StorageSharedKeyCredential(storageConfig.StorageAccountName, storageConfig.StorageAccountKey));
        }

        public StorageSharedKeyCredential GetStorageSharedKeyCredential()
        {
            return new StorageSharedKeyCredential(this.storageConfig.StorageAccountName, this.storageConfig.StorageAccountKey);
        }

        public BlobSasQueryParameters GetServiceSasUriForContainer(BlobContainerClient containerClient,
                                                  string storedPolicyName = null)
        {
            // Check whether this BlobContainerClient object has been authorized with Shared Key.
            if (containerClient.CanGenerateSasUri)
            {
                // Create a SAS token that's valid for one hour.
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerClient.Name,
                    Resource = "c"
                };

                if (storedPolicyName == null)
                {
                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(4);
                    sasBuilder.SetPermissions(BlobSasPermissions.All);
                    //BlobSasPermissions.List | 
                    //BlobSasPermissions.Write);
                    sasBuilder.SetPermissions(BlobContainerSasPermissions.All);
                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }

                return sasBuilder.ToSasQueryParameters(GetStorageSharedKeyCredential());
            }
            else
            {
                this.telemetryClient.TrackTrace("BlobContainerClient is not authorized with Shared Key.");
                return null;
            }
        }

        public Uri GetServiceSasUriForBlob(BlobClient blobClient,
            string storedPolicyName = null)
        {
            // Check whether this BlobClient object has been authorized with Shared Key.
            if (blobClient.CanGenerateSasUri)
            {
                // Create a SAS token that's valid for one hour.
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = blobClient.GetParentBlobContainerClient().Name,
                    BlobName = blobClient.Name,
                    Resource = "b"
                };

                if (storedPolicyName == null)
                {
                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(4);
                    sasBuilder.SetPermissions(BlobSasPermissions.All);
                    //BlobSasPermissions.List | 
                    //BlobSasPermissions.Write);
                    sasBuilder.SetPermissions(BlobContainerSasPermissions.All);
                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }

                Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
                this.telemetryClient.TrackTrace("SAS URI for blob is : " + sasUri.ToString());

                return sasUri;
            }
            else
            {
                this.telemetryClient.TrackTrace("BlobClient is not authorized with Shared Key.");
                return null;
            }
        }

        public Uri GetServiceSasUriForDirectory(DataLakeDirectoryClient directoryClient,
                                                  string storedPolicyName = null)
        {
            if (directoryClient.CanGenerateSasUri)
            {
                // Create a SAS token that's valid for one hour.
                DataLakeSasBuilder sasBuilder = new DataLakeSasBuilder()
                {
                    // Specify the file system name, the path, and indicate that
                    // the client object points to a directory.
                    FileSystemName = directoryClient.FileSystemName,
                    Resource = "d",
                    IsDirectory = true,
                    Path = directoryClient.Path,
                };

                // If no stored access policy is specified, create the policy
                // by specifying expiry and permissions.
                if (storedPolicyName == null)
                {
                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(4);
                    sasBuilder.SetPermissions(DataLakeSasPermissions.Read |
                        DataLakeSasPermissions.Write |
                        DataLakeSasPermissions.List);
                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }

                // Get the SAS URI for the specified directory.
                Uri sasUri = directoryClient.GenerateSasUri(sasBuilder);
                this.telemetryClient.TrackTrace("SAS URI for directory is : " + sasUri.ToString());

                return sasUri;
            }
            else
            {
                this.telemetryClient.TrackTrace("DataLakeDirectoryClient is not authorized with Shared Key.");
                return null;
            }
        }
    }
}
