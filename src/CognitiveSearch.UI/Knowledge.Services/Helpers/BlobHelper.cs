// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Storage.Blobs;

namespace Knowledge.Services.Helpers
{
    public class BlobHelper
    {
        public static async System.Threading.Tasks.Task<bool> IsBlobExistsAsync(BlobContainerClient container, string blobpath)
        {
            string blobname = blobpath.Replace(container.Name + "/", "");

            BlobClient blob = container.GetBlobClient(blobname);

            bool existingblob = await blob.ExistsAsync();

            if (existingblob)
            {
                return true;
            }

            return false;
        }
    }
}
