// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Storage.Blobs;

namespace Microsoft.Services.Common
{
    // https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata

    public class BlobHelper
    {
        public static string ReplaceFirstOccurrence(string Source, string Find, string Replace)
        {
            int Place = Source.IndexOf(Find);

            if (Place >= 0)
            {
                return Source.Remove(Place, Find.Length).Insert(Place, Replace);
            }
            else { return Source; }
        }

        public static string ReplaceLastOccurrence(string Source, string Find, string Replace)
        {
            int Place = Source.LastIndexOf(Find);
            if (Place >= 0)
            {
                return Source.Remove(Place, Find.Length).Insert(Place, Replace);
            }
            else { return Source; }
        }

        public static async System.Threading.Tasks.Task<bool> IsBlobExistsAsync(BlobContainerClient container, IDocumentEntity docitem)
        {
            string blobpath = IDocumentEntity.GetContentBlobPath(docitem, container.Name, container.Uri.ToString());

            //string blobname = blobpath.Replace(container.Name + "/", "");
            string blobname = ReplaceFirstOccurrence(blobpath, container.Name + "/", "");

            BlobClient blob = container.GetBlobClient(blobname);

            bool existingblob = await blob.ExistsAsync();

            return existingblob;
        }
        public static async System.Threading.Tasks.Task<bool> IsBlobExistsAsync(BlobContainerClient container, string blobpath)
        {
            //string blobname = blobpath.Replace(container.Name + "/", "");
            string blobname = ReplaceFirstOccurrence(blobpath, container.Name + "/", "");

            BlobClient blob = container.GetBlobClient(blobname);

            bool existingblob = await blob.ExistsAsync();

            return existingblob;
        }
    }
}
