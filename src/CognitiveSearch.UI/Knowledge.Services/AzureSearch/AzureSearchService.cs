// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.AzureSearch
{
    using Knowledge.Configuration;
    using Knowledge.Configuration.Graph;
    using Knowledge.Services.AzureSearch.REST;
    using Knowledge.Services.Helpers;
    using Knowledge.Services.Models;
    using Knowledge.Services.Models.Ingress;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class AzureSearchService : AzureSearchRESTService, IAzureSearchService
    {
        private List<string> ignoreFields = new List<string>();

        private IMemoryCache _cache;

        public AzureSearchService (TelemetryClient telemetry, IMemoryCache memoryCache,
            SearchServiceConfig serviceConfig,
            GraphConfig graphConfig)
        {
            this.telemetryClient = telemetry;
            this._cache = memoryCache;
            this.serviceConfig = serviceConfig;
            this.graphConfig = graphConfig;
        }

        private async Task<AzureSearchServiceResponse> GetDocumentFromAzureSearch(SearchResponse SearchResponse, SearchRequest queryEntity, List<string> facets=null, string filter = "",bool paragraphSearch=false)
        {
            //if (string.IsNullOrWhiteSpace(filter))
            //{
            //    LoggerHelper.Instance.LogVerbose($"Preparing filter clause for Azure Search Query");
            //    filter = QueryHelper.BuildQueryFilter(queryEntity);
            //}

            AzureSearchServiceRequest sp = null; 

            if ( paragraphSearch )
            {
                sp = new AzureSearchServiceRequest
                {
                    searchFields = "paragraphs",
                    highlight = "paragraphs-3",
                    highlightPreTag = ServicesConstants.SEARCH_HIGHLIGHT_PRETAG,
                    highlightPostTag = ServicesConstants.SEARCH_HIGHLIGHT_POSTTAG,
                    select = "paragraphs,metadata_storage_name,document_id,document_uri,title",
                    top = 5,
                    search = queryEntity.EscapedTranslatedQueryText
                };
            }
            else
            {
                SearchSchema searchSchema;

                if (!this._cache.TryGetValue($"SchemaField_{serviceConfig.IndexName}", out searchSchema))
                {
                    LoggerHelper.Instance.LogVerbose($"Schema Fields not present in cache");
                    var fields = this._searchIndexClient.GetIndex(this.serviceConfig.IndexName).Value.Fields;
                    searchSchema = new SearchSchema().AddFields(fields);
                    this._cache.Set<SearchSchema>($"SchemaField_{serviceConfig.IndexName}", searchSchema, new MemoryCacheEntryOptions() { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(serviceConfig.CacheExpirationForSchemaField) });
                    LoggerHelper.Instance.LogVerbose($"Schema Fields set in cache");
                }

                SearchModel Model = new(searchSchema,serviceConfig, graphConfig);

                sp = new AzureSearchServiceRequest
                {
                    count = true,
                    searchMode = "all",
                    top = queryEntity.PageSize,
                    skip = (queryEntity.PageNumber - 1) * queryEntity.PageSize,
                    facets = Model.Facets.Select(f => f.Name).ToList(),
                    searchFields = string.Join(",", Model.SearchableFields),
                    highlight = string.Join("-3,", Model.SearchableFields),
                    highlightPreTag = ServicesConstants.SEARCH_HIGHLIGHT_PRETAG,
                    highlightPostTag = ServicesConstants.SEARCH_HIGHLIGHT_POSTTAG,
                    search = queryEntity.EscapedTranslatedQueryText,
                    queryType = "full"
                };

                if (facets != null && facets.Any())
                {
                    sp.facets = facets;
                }

                if (!string.IsNullOrWhiteSpace(filter)) sp.filter = filter;

            }

            int totalCount = 0;

            // AZURE SEARCH REST API

            AzureSearchRESTResponse jsonresponse = await AzureSearchRestAPI(serviceConfig.IndexName, sp);

            if (jsonresponse.documents.Count == 0)
            {
                LoggerHelper.Instance.LogVerbose($"Azure Search - unsuccessful with translated query. Reverting to the original quey text {queryEntity.EscapedQueryText}.");

                sp.search = queryEntity.EscapedQueryText;
                jsonresponse = await AzureSearchRestAPI(serviceConfig.IndexName,sp);
            }
            totalCount = jsonresponse.documents.Count;

            LoggerHelper.Instance.LogVerbose($"Azure Search successful with {jsonresponse.documents.Count} records.");

            // Process Results 

            var result = new AzureSearchServiceResponse
            {
                results = jsonresponse.documents
            };

            LoggerHelper.Instance.LogEvent($"AzureSearchModel", "AzureSearchService", SearchResponse.SearchId, serviceConfig.IndexName, queryEntity.QueryText, totalCount);

            LoggerHelper.Instance.LogVerbose($"Azure Paragraphs Search successful with {result.results.Count} records.");

            return result;
        }

        //
        // https://docs.microsoft.com/en-us/azure/search/search-more-like-this
        //
        public async Task<AzureSearchServiceResponse> GetSimilarDocuments(SearchResponse SearchResponse, SearchRequest queryEntity)
        {
            // Azure Cognitivie Search - Structure of a moreLikeThis query 
            // 
            // {
            //   "moreLikeThis": "29",
            //   "searchFields": "Description"
            // }                
            //

            AzureSearchSimilarParameters sp = new()
            {
                top = 20,
                searchFields = "keyPhrases",
                moreLikeThis = queryEntity.QueryText
            };

            // Search REST Client
            AzureSearchRESTResponse jsonresponse = await this.AzureSearchRestAPI(this.serviceConfig.IndexName, sp);

            var result = new AzureSearchServiceResponse
            {
                results = jsonresponse.documents
            };

            return result;
        }

        public Task<SearchResponse> GetDocumentsAsync(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetDocumentByIndexKey(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetDocumentById(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetDocumentEmbedded(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetDocumentCoverImage(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }
        public Task<SearchResponse> GetDocumentCoverImageByIndexKey(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetLatestDocumentsAsync(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetImagesAsync(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetLatestImagesAsync(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetVideosAsync(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public string GetSearchId()
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> AutocompleteAsync(IngressSuggestionRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> SuggestAsync(IngressSuggestionRequest request)
        {
            throw new NotImplementedException();
        }

        public void RunIndexers()
        {
            throw new NotImplementedException();
        }

        public Task RunIndexer(string indexerName)
        {
            throw new NotImplementedException();
        }

        public Task TransformQuery(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetDocumentSiblings(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetNewsAsync(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> GetLatestNewsAsync(IngressSearchRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
