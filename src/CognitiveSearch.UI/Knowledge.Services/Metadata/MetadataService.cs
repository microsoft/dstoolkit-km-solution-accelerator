// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Metadata
{
    using Azure.Storage;
    using Azure.Storage.Blobs;
    using Knowledge.Services;
    using Knowledge.Services.AzureStorage;
    using Knowledge.Services.Helpers;
    using Microsoft.ApplicationInsights;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class MetadataService : AbstractService, IMetadataService
    {
        private BlobContainerClient container { get; set; }

        private StorageConfig config { get; set; }

        private string metadataContainerPath { get; set; }
        private string storageServicePath { get; set; }

        public MetadataService(TelemetryClient telemetry, StorageConfig _config)
        {
            this.telemetryClient = telemetry;
            this.config = _config;
            this.metadataContainerPath = $"https://{config.StorageAccountName}.blob.core.windows.net/metadata";
            this.storageServicePath = $"https://{config.StorageAccountName}.blob.core.windows.net/";
        }

        public async Task<string> GetDocumentMetadataAsync(string documentPath = "")
        {
            container = new BlobContainerClient(new Uri(metadataContainerPath), new StorageSharedKeyCredential(config.StorageAccountName, config.StorageAccountKey));

            string blobname = UrlUtility.UrlDecode(documentPath).Replace(this.storageServicePath, "");

            BlobClient blob = container.GetBlobClient(blobname+".json");

            bool existingblob = await blob.ExistsAsync();

            if (existingblob)
            {
                // Get the metadata json file from the storage
                MemoryStream mstream = new MemoryStream();
                await blob.DownloadToAsync(mstream);

                mstream.Seek(0, SeekOrigin.Begin);

                return Encoding.UTF8.GetString(mstream.ToArray());
            }

            return String.Empty;
        }
    }
}
