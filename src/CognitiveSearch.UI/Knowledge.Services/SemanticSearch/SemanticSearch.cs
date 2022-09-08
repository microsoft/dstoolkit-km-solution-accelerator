// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration;
using Knowledge.Configuration.AzureStorage;
using Knowledge.Configuration.SemanticSearch;
using Knowledge.Models;
using Knowledge.Models.Ingress;
using Knowledge.Services.AzureSearch;
using Knowledge.Services.AzureSearch.REST;
using Knowledge.Services.Helpers;
using Knowledge.Services.Translation;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Knowledge.Services.SemanticSearch
{
    public class SemanticSearch : AzureSearchRESTService, ISemanticSearchService
    {
        ITranslationService translationService;

        SemanticSearchConfig config;

        public SemanticSearch(TelemetryClient telemetry,
            ITranslationService translator,
            StorageConfig strCfg,
            IDistributedCache cache, 
            SearchServiceConfig serviceConfig,
            SemanticSearchConfig semanticConfig)
        {
            this.telemetryClient = telemetry;

            this.translationService = translator;
            this.distCache = cache;
            this.serviceConfig = serviceConfig;
            this.storageConfig = strCfg;

            this.config = semanticConfig;

            if (config.IsEnabled)
            {
                this.InitSearchClients();
            }
        }

        public async Task<AzureSearchRESTResponse> GetSemanticRESTResults(IngressSearchRequest request)
        {
            if (!config.IsEnabled)
            {
                return new AzureSearchRESTResponse();
            }

            if (request.retrievableFields == null) 
            {
                request.retrievableFields = this.GetModel(request.indexName).ReducedRetrievableFields;
            }

            if (!string.IsNullOrEmpty(request.queryText))
            {
                request.queryText = request.queryText.Replace("-", "").Replace("?", "");
            }
            else
            {
                request.queryText = QueryHelper.QUERY_ALL;
            }

            // Automatic Translation
            if (request.queryText != QueryHelper.QUERY_ALL) request.queryText = translationService.TranslateSearchText(request.queryText);

            // Create the Semantic AzureSearchServiceRequest 
            AzureSearchServiceRequest req = CreateSemanticSearchRequest(request);

            AzureSearchRESTResponse response = await AzureSearchRestAPI(this.GetModel(request.indexName).IndexName, req);

            response.tokens = this.GetContainerSasUris();

            return response;
        }

        public async Task<SearchResponse> GetSemanticResults(IngressSearchRequest request)
        {
            if (!config.IsEnabled)
            {
                return new SearchResponse();
            }

            if (request.retrievableFields == null)
            {
                request.retrievableFields = this.GetModel(request.indexName).ReducedRetrievableFields;
            }

            if (!string.IsNullOrEmpty(request.queryText))
            {
                request.queryText = request.queryText.Replace("-", "").Replace("?", "");
            }
            else
            {
                request.queryText = QueryHelper.QUERY_ALL;
            }

            // Automatic Translation
            if (request.queryText != QueryHelper.QUERY_ALL) request.queryText = translationService.TranslateSearchText(request.queryText);

            //var searchId = this.GetSearchId().ToString();

            // Create the Semantic AzureSearchServiceRequest 
            AzureSearchServiceRequest req = CreateSemanticSearchRequest(request);

            telemetryClient.TrackTrace(Newtonsoft.Json.JsonConvert.SerializeObject(req)); 

            AzureSearchRESTResponse restResponse = await AzureSearchRestAPI(this.GetModel(request.indexName).IndexName,req);

            SearchResponse response = CreateSearchResponse(restResponse,request.indexName); 

            response.Tokens = this.GetContainerSasUris();

            return response;
        }

        private AzureSearchServiceRequest CreateSemanticSearchRequest(IngressSearchRequest req)
        {
            AzureSearchServiceRequest request = new AzureSearchServiceRequest
            {
                search = req.queryText,
                queryType = config.queryType,
                queryLanguage = config.queryLanguage,
                semanticConfiguration = config.semanticConfiguration,
                speller = config.speller,
                answers = config.answers,
                count = config.count,
                highlightPreTag = config.highlightPreTag,
                highlightPostTag = config.highlightPostTag,
                top = config.top
            };

            // Select Fields filter
            if (req.retrievableFields != null)
            {
                request.select = String.Join(",", req.retrievableFields);
            }

            List<String> SelectFacets = new List<String>();

            foreach (String item in GetModel(req.indexName).Facets.Select(f => f.Name).ToList())
            {
                SelectFacets.Add(item + ",sort:count");
            }

            // Facets filter
            request.facets = SelectFacets;

            string filter = String.Empty;

            filter = QueryHelper.AddFilter(filter, req.incomingFilter);

            // Facet
            string facet_filter = FacetHelper.GenerateFacetFilter(GetModel(req.indexName), req.searchFacets);

            filter = QueryHelper.AddFilter(filter, facet_filter);

            // Content Sources
            string sources_filter = ContentSourcesHelper.GenerateFilter(req.content_sources);

            filter = QueryHelper.AddFilter(filter, sources_filter);

            // Security Trimming
            if (this.serviceConfig.IsSecurityTrimmingEnabled)
            {
                string security_filter = PermissionsHelper.GeneratePermissionsFilter(serviceConfig.PermissionsPublicFilter,
                    serviceConfig.PermissionsProtectedFilter,
                    req.permissions);

                filter = QueryHelper.AddFilter(filter, security_filter);
            }

            request.filter = filter;

            return request;
        }

        public SearchResponse CreateSearchResponse(AzureSearchRESTResponse response, string indexName = null)
        {
            Dictionary<string, string> s_tokens = GetContainerSasUris();

            var facetResults = new Dictionary<string, IList<FacetValue>>();
            var tagsResults = new Dictionary<string, IList<FacetValue>>();

            if (response != null)
            {
                if (response.facets != null)
                {
                    // Populate selected facets from the Search Model
                    foreach (var facetResult in response.facets.Where(f => this.GetModel(indexName).Facets.Where(x => x.Name == f.Key).Any()))
                    {
                        List<FacetValue> values = new List<FacetValue>();

                        foreach (FacetEntry fr in facetResult.Value)
                        {
                            FacetValue fv = new FacetValue
                            {
                                count = fr.count
                            };

                            if (fr.value.GetType() == typeof(String))
                            {
                                fv.value = (string)fr.value;
                            }
                            if (fr.value.GetType() == typeof(DateTime))
                            {
                                fv.value = (DateTime.Parse(fr.value)).ToString();
                            }

                            values.Add(fv);
                        }

                        if (values.Count() > 0)
                        {
                            facetResults.Add(facetResult.Key, values);
                        }
                    }

                    // Populate selected tags from the Search Model
                    foreach (var tagResult in response.facets.Where(t => this.GetModel(indexName).Tags.Where(x => x.Name == t.Key).Any()))
                    {
                        List<FacetValue> values = new List<FacetValue>();

                        foreach (FacetEntry fr in tagResult.Value)
                        {
                            FacetValue fv = new FacetValue
                            {
                                count = fr.count
                            };

                            if (fr.value.GetType() == typeof(String))
                            {
                                fv.value = (string)fr.value;
                            }
                            if (fr.value.GetType() == typeof(DateTime))
                            {
                                fv.value = (DateTime.Parse(fr.value)).ToString();
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
                Results = (response?.documents),
                Facets = facetResults,
                Tags = tagsResults,
                Count = (response == null ? 0 : Convert.ToInt32(response.count)),
                //SearchId = searchId,
                IdField = this.serviceConfig.KeyField,
                Tokens = s_tokens,
                IsPathBase64Encoded = this.serviceConfig.IsPathBase64Encoded,
                IsSemanticSearch = true,
                SemanticAnswers = response?.answers
            };
            return result;
        }

    }
}
