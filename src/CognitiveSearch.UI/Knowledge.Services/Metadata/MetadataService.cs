// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Metadata
{
    using Azure.Identity;
    using Azure.Storage;
    using Azure.Storage.Blobs;
    using Knowledge.Configuration.AzureStorage;
    using Knowledge.Services;
    using Knowledge.Services.Helpers;
    using Microsoft.ApplicationInsights;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class MetadataService : AbstractService, IMetadataService
    {
        private BlobContainerClient container { get; set; }

        private new StorageConfig _config { get; set; }

        public MetadataService(TelemetryClient telemetry, StorageConfig config)
        {
            this.telemetryClient = telemetry;
            _config = config;
        }

        public async Task<string> GetDocumentMetadataAsync(string documentPath, string type)
        {
            if (! String.IsNullOrEmpty(documentPath))
            {
                var blobClient = new BlobServiceClient(new Uri($"https://{_config.StorageAccountName}.blob.core.windows.net/"), new DefaultAzureCredential());                
                var container = blobClient.GetBlobContainerClient(IMetadataService.Container);

                
                BlobUriBuilder blobUriBuilder = new(new Uri(documentPath));

                if (String.IsNullOrEmpty(type))
                {
                    type = IMetadataService.JsonMetadata; 
                }

                BlobClient blob = container.GetBlobClient(blobUriBuilder.BlobContainerName + "/" +blobUriBuilder.BlobName + type);

                bool existingblob = await blob.ExistsAsync();

                if (existingblob)
                {
                    // Get the metadata json file from the storage
                    MemoryStream mstream = new();
                    await blob.DownloadToAsync(mstream);

                    mstream.Seek(0, SeekOrigin.Begin);

                    return Encoding.UTF8.GetString(mstream.ToArray());
                }
            }

            return String.Empty;
        }
    }
}
