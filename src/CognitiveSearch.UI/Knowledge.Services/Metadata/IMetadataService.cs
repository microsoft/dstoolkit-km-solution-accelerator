// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Knowledge.Services.Metadata
{
    public interface IMetadataService
    {
        public static string Container = "metadata";

        public static string HtmlMetadata = ".html";
        public static string JsonMetadata = ".json";

        public Task<string> GetDocumentMetadataAsync(string documentPath, string type); 
    }
}