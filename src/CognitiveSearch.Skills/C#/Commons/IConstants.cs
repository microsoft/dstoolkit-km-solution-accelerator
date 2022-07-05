// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Services.Common
{
    public class IConstants
    {
        public static bool IsSupportedImageType(string name)
        {
            string lowered = name.ToLowerInvariant();

            if (lowered.EndsWith("png") || lowered.EndsWith("tif") || lowered.EndsWith("jpeg") || lowered.EndsWith("jpg"))
            {
                return true;
            }

            if (name.Contains("docProps/"))
            {
                return false;
            }

            return false;
        }

        public static readonly string ContainerConnectionString = FEnvironment.StringReader("ContainerConnectionString");

        public static readonly string[] ContainerNames = FEnvironment.StringReader("ContainerName","documents").Split(',');

        public static readonly string MetadataContainerName = FEnvironment.StringReader("MetadataContainerName", "metadata");
        public static readonly string ImageContainerName = FEnvironment.StringReader("ImageContainerName", "images");
        public static readonly string ThumbnailsContainerName = FEnvironment.StringReader("ThumbnailsContainerName", "thumbnails");

        public static readonly string tikaEndpoint = FEnvironment.StringReader("TikaContainerUrl", String.Empty);
        public static readonly string tikaUnpackEndPoint = "/azure/unpack";
        public static readonly string tikaConvertEndPoint = "/azure/convert";
        public static readonly List<string> tikaConvertExtensions = FEnvironment.StringArrayReader("TikaConverterSupportedExtensions");

        public static readonly string UserAgent = FEnvironment.StringReader("UserAgent", "MicrosoftPOC/1.0");

        public const int MinImageSizeForComputerVision = 25 * 1024;
        public const int MaxImageSizeForComputerVision = 4 * 1024 * 1024;

        public static readonly string ComputerVisionEndpoint = FEnvironment.StringReader("ComputerVisionEndpoint");
        public static readonly string ComputerVisionSubscriptionKey = FEnvironment.StringReader("ComputerVisionSubscriptionKey");

        public const int numberOfCharsInOperationId = 36;

        public static readonly bool CustomVisionEnabled = FEnvironment.BooleanReader("CustomVisionEnabled", false);

        public static readonly string GraphDirectory = FEnvironment.StringReader("GraphDirectory", "graph");

    }
}
