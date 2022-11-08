// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Models;
using Knowledge.Models.Ingress;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Knowledge.Services
{
    public interface IQueryService
    {
        public Task<SearchResponse> GetDocumentsAsync(IngressSearchRequest request);

        public Task<SearchResponse> GetDocumentByIndexKey(IngressSearchRequest request);

        public Task<SearchResponse> GetDocumentById(IngressSearchRequest request);

        public Task<SearchResponse> GetDocumentEmbedded(IngressSearchRequest request);

        public Task<SearchResponse> GetDocumentCoverImage(IngressSearchRequest request);

        public Task<SearchResponse> GetDocumentCoverImageByIndexKey(IngressSearchRequest request);

        public Task<SearchResponse> GetDocumentSiblings(IngressSearchRequest request);

        public Task<SearchResponse> GetLatestDocumentsAsync(IngressSearchRequest request);

        public Task<SearchResponse> GetImagesAsync(IngressSearchRequest request);

        public Task<SearchResponse> GetLatestImagesAsync(IngressSearchRequest request);

        public Task<SearchResponse> GetVideosAsync(IngressSearchRequest request);

        public string GetSearchId();

        public Task<List<string>> AutocompleteAsync(IngressSuggestionRequest request);

        public Task<List<string>> SuggestAsync(IngressSuggestionRequest request);

        public void RunIndexers();

        public Task RunIndexer(string indexerName);

        public Task TransformQuery(IngressSearchRequest request);

        public Task<SearchResponse> GetSimilarDocuments(IngressSearchRequest request);
    }
}