// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Web;

namespace Microsoft.Services.Common
{
    public class IDocumentEntity
    {
        public string IndexKey { get; set; }

        public string Id { get; set; }
        public string Extension { get; set; } = string.Empty;

        public string Name { get; set; }
        public string WebUrl { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public bool IsDeleted { get; set; }
        public bool IsFolder { get; set; }

        public string ParentUrl { get; set; }

        public string GetExtension()
        {
            return Path.GetExtension(this.Name);
        }
        public bool IsPageImage()
        {
            return Path.GetFileNameWithoutExtension(this.Name).EndsWith("-99999");
        }

        public static string GetContentBlobPath(IDocumentEntity doc, string containerName, string containerUri)
        {
            return HttpUtility.UrlDecode(doc.WebUrl.Replace(containerUri, containerName));
        }

        public static string GetRelativeContentBlobPath(IDocumentEntity doc, string containerUri)
        {
            return HttpUtility.UrlDecode(doc.WebUrl.Replace(containerUri, "").Substring(1));
        }

        public static string GetRelativeImagesPath(IDocumentEntity doc, string containerName, string containerUri)
        {
            //return "images/" + PartitionKey + "/" + RowKey + Extension;
            return HttpUtility.UrlDecode(doc.WebUrl.Replace(containerUri, containerName));
        }

        public static string GetParentDocumentMetadataPath(IDocumentEntity doc, string containerName, string containerUri)
        {
            return HttpUtility.UrlDecode(doc.ParentUrl.Replace(containerUri, containerName));
        }

        public static string GetRelativeMetadataPath(IDocumentEntity doc, string containerName, string containerUri)
        {
            return GetRelativeMetadataPathByUrl(doc.WebUrl, containerName, containerUri);
        }
        public static string GetRelativeMetadataPathByUrl(string url, string containerName, string containerUri)
        {
            return HttpUtility.UrlDecode(url.Replace(containerUri, containerName));
        }

        public static string GetRelativeThumbnailPath(IDocumentEntity doc, string containerName, string containerUri)
        {
            return HttpUtility.UrlDecode(doc.WebUrl.Replace(containerUri, containerName) + ".thumbnail.jpg");
        }
    }
}
