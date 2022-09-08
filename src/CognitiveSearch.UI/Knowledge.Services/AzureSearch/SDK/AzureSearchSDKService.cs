// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Knowledge.Configuration;
using Knowledge.Configuration.AzureStorage;
using Knowledge.Configuration.Graph;
using Knowledge.Configuration.SemanticSearch;
using Knowledge.Models;
using Knowledge.Models.Ingress;
using Knowledge.Services.Helpers;
using Knowledge.Services.SemanticSearch;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Knowledge.Services.AzureSearch.SDK
{
    public class AzureSearchSDKService : AbstractSearchSDKService, IAzureSearchSDKService
    {
        private string SearchId;

        private SemanticSearchConfig semanticConfig;
        private ISemanticSearchService semanticSearch;

        public AzureSearchSDKService(TelemetryClient telemetry,
            SearchServiceConfig configuration,
            SemanticSearchConfig semanticCfg,
            StorageConfig strCfg,
            ISemanticSearchService semanticSvc,
            GraphConfig graphConfig)
        {
            this.telemetryClient = telemetry;
            this.serviceConfig = configuration;
            this.storageConfig = strCfg;

            this.semanticConfig = semanticCfg;
            this.semanticSearch = semanticSvc;

            this.graphConfig = graphConfig;

            InitSearchClients();
        }

        public async Task<SearchResponse> GetDocumentsAsync(IngressSearchRequest request)
        {
            this.telemetryClient.TrackTrace(JsonConvert.SerializeObject(request), Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Verbose);
            
            var retrievableFields = this.GetModel(request.indexName).ReducedRetrievableFields;

            if (!string.IsNullOrEmpty(request.queryText))
            {
                request.queryText = request.queryText.Replace("-", "").Replace("?", "");
            }
            else
            {
                request.queryText = QUERY_ALL;
            }

            var searchId = this.GetSearchId().ToString();

            SearchResponse result; 

            if ( (request.options != null) && (request.options.isSemanticSearch) && semanticConfig.IsEnabled)
            {
                // Semantic search doesn't allow more than 50 results. No pagination possible. 
                if (request.currentPage > 1 )
                {
                    result = new SearchResponse(); 
                }
                else
                {
                    request.retrievableFields = retrievableFields;
                    result = await this.semanticSearch.GetSemanticResults(request);
                }
            }
            else
            {
                var searchTask = this.SearchDocuments(request.queryText,
                                                      request.searchFacets,
                                                      retrievableFields,
                                                      null,
                                                      request.currentPage,
                                                      incomingfilter: request.incomingFilter,
                                                      parameters: request.parameters,
                                                      permissions: request.permissions,
                                                      polygonString: request.polygonString,
                                                      indexName: request.indexName,
                                                      contentSources: request.content_sources);

                result = this.CreateSearchResponse(searchTask, searchId, request.indexName);
            }

            return result;
        }

        public SearchResults<SearchDocument> SearchDocuments(            
            string searchText,
            SearchFacet[] searchFacets = null,
            string[] selectFields = null,
            List<String> selectFacets = null,
            int currentPage = 1,
            string incomingfilter = "",
            QueryParameters parameters = null,
            SearchPermission[] permissions = null,
            string polygonString = null,
            string indexName = null,
            string[] contentSources = null)
        {

            QueryParameters qparams = parameters ?? (new());

            SearchOptions options = GenerateSearchOptions(qparams,
                                                          searchFacets,
                                                          selectFields,
                                                          selectFacets,
                                                          currentPage,
                                                          polygonString,
                                                          incomingfilter,
                                                          permissions,
                                                          indexName, 
                                                          contentSources);

            this.telemetryClient.TrackTrace(JsonConvert.SerializeObject(options), Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Verbose);

            return this.SearchDocuments(indexName, searchText, options);
        }

        public SearchResults<SearchDocument> SearchDocuments(string indexName, string searchText, SearchOptions options)
        {
            // Perform search
            try
            {
                var s = GenerateSearchId(searchText, options);

                this.SearchId = s.Result;

                return this.GetSearchClient(indexName).Search<SearchDocument>(searchText, options);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }
            return null;
        }

        public SearchOptions GenerateSearchOptions(
            QueryParameters queryParameters,
            SearchFacet[] searchFacets = null,
            string[] selectFields = null,
            List<String> selectFacets = null,
            int currentPage = 1,
            string polygonString = null,
            string incomingfilter = "",
            SearchPermission[] permissions = null,
            string indexName = null,
            string[] contentSources = null)
        {

            // For more information on search parameters visit: 
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.search.models.searchparameters?view=azure-dotnet
            SearchOptions options = new SearchOptions()
            {

                SearchMode = SearchMode.All,
                Size = queryParameters.RowCount,
                Skip = (currentPage - 1) * queryParameters.RowCount,
                IncludeTotalCount = true,
                QueryType = SearchQueryType.Full
            };

            if (queryParameters.ScoringProfile != null)
            {
                options.ScoringProfile = queryParameters.ScoringProfile;
            }

            // Select Fields filter
            if (selectFields != null)
            {
                foreach (string s in selectFields)
                {
                    options.Select.Add(s);
                }
            }

            // Facets filter
            List<String> facetsList = new List<String>();

            //if (selectFacets != null && selectFacets.Count > 0)
            if (selectFacets != null)
            {
                    facetsList = selectFacets;
            }
            else
            {
                facetsList = GetModel(indexName).Facets.Select(f => f.Name).ToList();
            }

            foreach (string f in facetsList)
            {
                options.Facets.Add(f + ",count:10,sort:count");
            }

            // OderBy filter
            if (queryParameters.inOrderBy != null)
            {
                foreach (string f in queryParameters.inOrderBy)
                {
                    options.OrderBy.Add(f);
                }
            }

            foreach (var item in this.serviceConfig.GetHHFields())
            {
                options.HighlightFields.Add(item);
            }

            options.HighlightPreTag = "<span class=\"highlight\">";
            options.HighlightPostTag = "</span>";

            string filter = incomingfilter;

            if ( String.IsNullOrEmpty(filter) )
            {
                filter = String.Empty;
            }

            // Facet
            string facet_filter = FacetHelper.GenerateFacetFilter(GetModel(indexName), searchFacets);

            filter = QueryHelper.AddFilter(filter, facet_filter);

            // Content Sources
            string sources_filter = ContentSourcesHelper.GenerateFilter(contentSources);

            filter = QueryHelper.AddFilter(filter, sources_filter);

            // Security Trimming
            if ( serviceConfig.IsSecurityTrimmingEnabled)
            {
                string security_filter = PermissionsHelper.GeneratePermissionsFilter(serviceConfig.PermissionsPublicFilter,
                    serviceConfig.PermissionsProtectedFilter,
                    permissions);

                filter = QueryHelper.AddFilter(filter, security_filter);
            }

            options.Filter = filter;

            // Add Filter based on geographic polygon if it is set.
            if (polygonString != null && polygonString.Length > 0)
            {
                string geoQuery = "geo.intersects(geoLocation, geography'POLYGON((" + polygonString + "))')";

                if (options.Filter != null && options.Filter.Length > 0)
                {
                    options.Filter += " and (" + geoQuery + ")";
                }
                else
                {
                    options.Filter = geoQuery;
                }
            }

            return options;
        }

        private async Task<SearchDocument> LookUp(string indexName, string id)
        {
            try
            {
                return await this.GetSearchClient(indexName).GetDocumentAsync<SearchDocument>(id);
            }
            catch (Exception ex)
            {   
                this.telemetryClient.TrackException(ex);
            }
            return null;
        }

        private async Task<string> GenerateSearchId(string searchText, SearchOptions options)
        {
            return Guid.NewGuid().ToString();
        }

        public string GetSearchId()
        {
            if (this.SearchId != null) { return this.SearchId; }
            return string.Empty;
        }

        private SearchResponse CreateSearchResponse(SearchResults<SearchDocument> response, string searchId, string indexName, bool SasTokens = true)
        {
            Dictionary<string, string> s_tokens;

            if (SasTokens)
            {
                s_tokens = GetContainerSasUris();
            }
            else
            {
                s_tokens = new Dictionary<string, string>();
            }

            var facetResults = new Dictionary<string, IList<FacetValue>>();
            var tagsResults = new Dictionary<string, IList<FacetValue>>();

            if (response != null)
            {
                if (response.Facets != null)
                {
                    // Populate selected facets from the Search Model
                    foreach (var facetResult in response.Facets.Where(f => this.GetModel(indexName).Facets.Where(x => x.Name == f.Key).Any()))
                    {
                        List<FacetValue> values = new List<FacetValue>();

                        foreach (FacetResult fr in facetResult.Value)
                        {
                            FacetValue fv = new FacetValue
                            {
                                count = (long)fr.Count
                            };

                            if (fr.Value.GetType() == typeof(String))
                            {
                                fv.value = (string)fr.Value;
                            }
                            if (fr.Value.GetType() == typeof(DateTime))
                            {
                                fv.value = ((DateTime)fr.Value).ToString();
                            }

                            values.Add(fv);
                        }

                        if (values.Count() > 0)
                        {
                            facetResults.Add(facetResult.Key, values);
                        }
                    }

                    // Populate selected tags from the Search Model
                    foreach (var tagResult in response.Facets.Where(t => this.GetModel(indexName).Tags.Where(x => x.Name == t.Key).Any()))
                    {
                        List<FacetValue> values = new List<FacetValue>();

                        foreach (FacetResult fr in tagResult.Value)
                        {
                            FacetValue fv = new FacetValue
                            {
                                count = (long)fr.Count
                            };

                            if (fr.Value.GetType() == typeof(String))
                            {
                                fv.value = (string)fr.Value;
                            }
                            if (fr.Value.GetType() == typeof(DateTime))
                            {
                                fv.value = ((DateTime)fr.Value).ToString();
                            }

                            values.Add(fv);
                        }

                        if (values.Count() > 0)
                        {
                            tagsResults.Add(tagResult.Key, values);
                        }
                    }
                }
            }

            var result = new SearchResponse
            {
                IndexName = indexName,
                Results = ConvertSearchDocuments(response),
                Facets = facetResults,
                Tags = tagsResults,
                Count = (response == null ? 0 : Convert.ToInt32(response.TotalCount)),
                SearchId = searchId,
                IdField = this.serviceConfig.KeyField,
                Tokens = s_tokens,
                IsPathBase64Encoded = this.serviceConfig.IsPathBase64Encoded
            };

            return result;
        }

        /// <summary>
        /// Initiates a run of the search indexer.
        /// </summary>
        public void RunIndexers()
        {
            foreach (var indexer in this.serviceConfig.GetSearchIndexers())
            {
                _ = this.RunIndexer(indexer);
            }
        }

        public async Task RunIndexer(string indexerName)
        {
            SearchIndexerClient _searchIndexerClient = new SearchIndexerClient(new Uri($"https://{this.serviceConfig.ServiceName}.search.windows.net/"), new AzureKeyCredential(this.serviceConfig.AdminKey));

            var indexStatus = await _searchIndexerClient.GetIndexerStatusAsync(indexerName);
            if (indexStatus.Value.LastResult.Status != IndexerExecutionStatus.InProgress)
            {
                _searchIndexerClient.RunIndexer(indexerName);
            }
        }

        private Dictionary<string, object> ConvertSearchDocument(SearchDocument doc)
        {
            if (doc != null)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(doc));
            }
            else
            {
                return null;
            }
        }
        private IList<Dictionary<string, object>> ConvertSearchDocuments(SearchResults<SearchDocument> results)
        {
            if ( results != null)
            {
                return JsonConvert.DeserializeObject<IList<Dictionary<string, object>>>(JsonConvert.SerializeObject(results.GetResults()));
            }
            else
            {
                return null;
            }
        }

        public async Task<SearchResponse> GetDocumentByIndexKey(IngressSearchRequest request)
        {
            var response = await this.LookUp(request.indexName, request.index_key);

            var result = new SearchResponse
            {
                Result = ConvertSearchDocument(response)
            };

            return result;
        }

        public async Task<SearchResponse> GetDocumentById(IngressSearchRequest request)
        {
            var embeddedfilter = $"document_id eq '{request.document_id}'";

            QueryParameters queryParameters = request.parameters ?? (new());
            queryParameters.RowCount = 1;

            UserOptions userOptions = new();

            // Perform search based on query, facets, filter, etc.
            var response = this.SearchDocuments(searchText: QUERY_ALL,
                                                incomingfilter: embeddedfilter,
                                                parameters: queryParameters,
                                                permissions: request.permissions,
                                                indexName: request.indexName);

            var results = response.GetResults();

            if (results.Any())
            {
                SearchDocument doc = results.First().Document;

                var result = new SearchResponse
                {
                    Result = ConvertSearchDocument(doc)
                };

                return result;

            }
            else
            {
                return new SearchResponse();
            }
        }

        // IMAGES Search 

        public async Task<SearchResponse> GetDocumentEmbedded(IngressSearchRequest request)
        {
            var embeddedfilter = $"image_parentid eq '{request.document_id}'";

            QueryParameters queryParameters = request.parameters ?? (new());
            queryParameters.RowCount = 100;
            queryParameters.inOrderBy = new List<string> { "document_filename asc" };

            UserOptions userOptions = new();

            // Perform search based on query, facets, filter, etc.
            var response = this.SearchDocuments(searchText: QUERY_ALL,
                                                selectFacets: new List<string>(),
                                                selectFields: this.GetModel(request.indexName).ReducedRetrievableFields,
                                                incomingfilter: embeddedfilter,
                                                parameters: queryParameters,
                                                permissions: request.permissions,
                                                indexName: request.indexName);

            var searchId = this.GetSearchId().ToString();

            SearchResponse result = this.CreateSearchResponse(response, searchId, request.indexName);

            return result;
        }

        // SIBLINGS
        public async Task<SearchResponse> GetDocumentSiblings(IngressSearchRequest request)
        {
            // Perform search based on query, facets, filter, etc.
            var response = this.SearchDocuments(searchText: QUERY_ALL,
                                                selectFacets: new List<string>(),
                                                selectFields: this.GetModel(request.indexName).ReducedRetrievableFields,
                                                incomingfilter: request.incomingFilter,
                                                parameters: request.parameters,
                                                permissions: request.permissions,
                                                indexName: request.indexName);

            var searchId = this.GetSearchId().ToString();

            SearchResponse result = this.CreateSearchResponse(response, searchId, request.indexName);

            return result;
        }

        public async Task<SearchResponse> GetDocumentCoverImage(IngressSearchRequest request)
        {
            var embeddedfilter = $"(image_parentid eq '{request.document_id}' and ((page_number ge 1) or (document_filename eq 'thumbnail.jpeg')))";
            // Support for document itself if it is an image...
            embeddedfilter += $" or (document_id eq '{request.document_id}' and content_group eq 'Image')";

            QueryParameters queryParameters = request.parameters ?? (new());
            queryParameters.RowCount = 1;
            queryParameters.inOrderBy = new List<string> { "page_number asc" };

            UserOptions userOptions = new();

            // Perform search based on query, facets, filter, etc.
            var response = this.SearchDocuments(searchText: QUERY_ALL,
                                                selectFacets: new List<string>(),
                                                selectFields: this.GetModel(request.indexName).ReducedRetrievableFields,
                                                incomingfilter: embeddedfilter,
                                                parameters: queryParameters,
                                                permissions: request.permissions,
                                                indexName: request.indexName);

            var searchId = this.GetSearchId().ToString();

            SearchResponse result = this.CreateSearchResponse(response, searchId, request.indexName, false);

            return result;
        }

        public async Task<SearchResponse> GetDocumentCoverImageByIndexKey(IngressSearchRequest request)
        {
            SearchDocument response = await this.LookUp(request.indexName, request.index_key);

            object document_id = null;

            if (response.TryGetValue("document_id", out document_id))
            {
                request.document_id = (string)document_id; 

                return await GetDocumentCoverImage(request);
            }
            else
            {
                return new SearchResponse();
            }
        }

        public async Task<SearchResponse> GetLatestImagesAsync(IngressSearchRequest request)
        {
            if (!string.IsNullOrEmpty(request.queryText))
            {
                request.queryText = request.queryText.Replace("-", "").Replace("?", "");
            }
            else
            {
                request.queryText = QUERY_ALL;
            }

            var embeddedfilter = $"search.in(content_type,'{this.serviceConfig.ImagesFilter}')";

            QueryParameters queryParameters = request.parameters ?? (new());
            queryParameters.inOrderBy = new List<string> { "last_modified desc" };

            UserOptions userOptions = new();

            // Perform search based on query, facets, filter, etc.
            var response = this.SearchDocuments(searchText: request.queryText,
                                                searchFacets: request.searchFacets,
                                                selectFacets: new List<string>(),
                                                selectFields: this.GetModel(request.indexName).ReducedRetrievableFields,
                                                currentPage: request.currentPage,
                                                incomingfilter: embeddedfilter,
                                                parameters: queryParameters,
                                                permissions: request.permissions,
                                                indexName: request.indexName);

            var searchId = this.GetSearchId().ToString();

            SearchResponse result = this.CreateSearchResponse(response, searchId, request.indexName);

            return result;
        }
        public async Task<SearchResponse> GetImagesAsync(IngressSearchRequest request)
        {
            var retrievableFields = this.GetModel(request.indexName).ReducedRetrievableFields;

            if (!string.IsNullOrEmpty(request.queryText))
            {
                request.queryText = request.queryText.Replace("-", "").Replace("?", "");
            }
            else
            {
                request.queryText = QUERY_ALL;
            }

            var imagesFilter = $"search.in(content_type,'{this.serviceConfig.ImagesFilter}')";

            if (String.IsNullOrEmpty(request.incomingFilter))
            {
                request.incomingFilter = imagesFilter;
            }
            else
            {
                request.incomingFilter += " and " + imagesFilter;
            }

            var searchId = this.GetSearchId().ToString();

            QueryParameters queryParameters = request.parameters ?? (new());
            queryParameters.RowCount = 50;
            queryParameters.ScoringProfile = "images";

            UserOptions userOptions = new();

            SearchResponse result;

            if ((request.options != null) && (request.options.isSemanticSearch) && semanticConfig.IsEnabled)
            {
                // Semantic search doesn't allow more than 50 results. No pagination possible. 
                if (request.currentPage > 1)
                {
                    result = new SearchResponse();
                }
                else
                {
                    request.retrievableFields = retrievableFields;
                    result = await this.semanticSearch.GetSemanticResults(request);
                }
            }
            else
            {
                // Get 50 images at the time
                var searchTask = this.SearchDocuments(searchText: request.queryText,
                                                      searchFacets: request.searchFacets,
                                                      selectFields: retrievableFields,
                                                      currentPage: request.currentPage,
                                                      incomingfilter: request.incomingFilter,
                                                      parameters: queryParameters,
                                                      permissions: request.permissions, indexName: request.indexName);

                result = this.CreateSearchResponse(searchTask, searchId, request.indexName);
            }

            return result;
        }

        public async Task<SearchResponse> GetVideosAsync(IngressSearchRequest request)
        {
            request.retrievableFields = this.GetModel(request.indexName).ReducedRetrievableFields;

            if (!string.IsNullOrEmpty(request.queryText))
            {
                request.queryText = request.queryText.Replace("-", "").Replace("?", "");
            }
            else
            {
                request.queryText = QUERY_ALL;
            }

            var videosFilter = $"search.in(content_type,'{this.serviceConfig.VideosFilter}')";
            if (String.IsNullOrEmpty(request.incomingFilter))
            {
                request.incomingFilter = videosFilter;
            }
            else
            {
                request.incomingFilter += " and " + videosFilter;
            }

            var searchId = this.GetSearchId().ToString();

            QueryParameters queryParameters = request.parameters ?? (new());
            queryParameters.RowCount = 50;
            queryParameters.ScoringProfile = "videos";

            UserOptions userOptions = new();

            SearchResponse result;

            if ((request.options != null) && (request.options.isSemanticSearch) && semanticConfig.IsEnabled)
            {
                // Semantic search doesn't allow more than 50 results. No pagination possible. 
                if (request.currentPage > 1)
                {
                    result = new SearchResponse();
                }
                else
                {
                    result = await this.semanticSearch.GetSemanticResults(request);
                }
            }
            else
            {
                // Get 50 videos at the time
                var searchTask = this.SearchDocuments(searchText: request.queryText,
                                                      searchFacets: request.searchFacets,
                                                      selectFields: request.retrievableFields,
                                                      currentPage: request.currentPage,
                                                      incomingfilter: request.incomingFilter,
                                                      parameters: queryParameters,
                                                      permissions: request.permissions, indexName: request.indexName);

                result = this.CreateSearchResponse(searchTask, searchId, request.indexName);
            }

            return result;
        }

        public async Task<List<string>> AutocompleteAsync(IngressSuggestionRequest request)
        {
            // Setup the autocomplete parameters.
            var options = new AutocompleteOptions()
            {
                Mode = AutocompleteMode.OneTermWithContext,
                Size = 6
            };
            options.SearchFields.Add(request.targetField);

            var autocompleteResult = await this.GetSearchClient(request.indexName).AutocompleteAsync(request.term, request.suggester, options).ConfigureAwait(false);

            // Convert the autocompleteResult results to a list that can be displayed in the client.
            return autocompleteResult.Value.Results.Select(x => x.Text).ToList();
        }

        public async Task<List<string>> SuggestAsync(IngressSuggestionRequest request)
        {
            var options = new SuggestOptions()
            {
                UseFuzzyMatching = request.fuzzy,
                Size = 8
            };

            options.SearchFields.Add(request.targetField);

            if (request.highlights)
            {
                options.HighlightPreTag = "<b>";
                options.HighlightPostTag = "</b>";
            }

            // Only one suggester can be specified per index. ??? TO VALIDATE
            var suggestResult = await this.GetSearchClient(request.indexName).SuggestAsync<SearchDocument>(request.term, request.suggester, options).ConfigureAwait(false);

            // Convert the suggest query results to a list that can be displayed in the client.
            return suggestResult.Value.Results.Select(x => x.Text).ToList();
        }


        public async Task<SearchResponse> GetLatestDocumentsAsync(IngressSearchRequest request)
        {
            if (!string.IsNullOrEmpty(request.queryText))
            {
                request.queryText = request.queryText.Replace("-", "").Replace("?", "");
            }
            else
            {
                request.queryText = QUERY_ALL;
            }

            var embeddedfilter = $"not search.in(content_type,'{this.serviceConfig.ImagesFilter}')";

            QueryParameters queryParameters = request.parameters ?? (new());
            queryParameters.inOrderBy = new List<string> { "last_modified desc" };

            UserOptions userOptions = request.options ?? (new());

            // Perform search based on query, facets, filter, etc.
            var response = this.SearchDocuments(searchText: request.queryText,
                                                searchFacets: request.searchFacets,
                                                selectFacets: new List<string>(),
                                                selectFields: this.GetModel(request.indexName).ReducedRetrievableFields,
                                                currentPage: request.currentPage,
                                                incomingfilter: embeddedfilter,
                                                parameters: queryParameters,
                                                permissions: request.permissions, indexName: request.indexName);

            var searchId = this.GetSearchId().ToString();

            SearchResponse result = this.CreateSearchResponse(response, searchId, request.indexName);

            return result;
        }

        public SearchServiceConfig GetSearchConfig()
        {
            return serviceConfig;
        }

        public Task TransformQuery(IngressSearchRequest request)
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
