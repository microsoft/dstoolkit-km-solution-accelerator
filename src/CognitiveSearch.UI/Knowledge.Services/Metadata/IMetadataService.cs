// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Knowledge.Services.Metadata
{
    public interface IMetadataService
    {
        public Task<string> GetDocumentMetadataAsync(string documentPath); 
    }
}