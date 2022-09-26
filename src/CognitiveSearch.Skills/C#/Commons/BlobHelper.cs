// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Storage.Blobs;
using System.Web;
using System;

namespace Microsoft.Services.Common
{
    // https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata

    public class BlobHelper
    {
        public static async System.Threading.Tasks.Task<bool> IsBlobExistsAsync(BlobContainerClient container, IDocumentEntity docitem)
        {
            string blobpath = IDocumentEntity.GetContentBlobPath(docitem, container.Name, container.Uri.ToString());

            string blobname = blobpath.Replace(container.Name + "/", "");
          
            BlobClient blob = container.GetBlobClient(blobname);

            bool existingblob = await blob.ExistsAsync();

            return existingblob;
        }
        public static async System.Threading.Tasks.Task<bool> IsBlobExistsAsync(BlobContainerClient container, string blobpath)
        {
            string blobname = blobpath.Replace(container.Name + "/", "");

            BlobClient blob = container.GetBlobClient(blobname);

            bool existingblob = await blob.ExistsAsync();

            return existingblob;
        }
    }
}
