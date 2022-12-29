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

            // Automatic Translation - Could be removed if Semantic language would support more than English language.
            if (request.queryText != QueryHelper.QUERY_ALL) request.queryText = translationService.TranslateSearchText(request.queryText);

            // Create the Semantic AzureSearchServiceRequest 
            AzureSearchServiceRequest req = CreateSemanticSearchRequest(request);

            AzureSearchRESTResponse response = await AzureSearchRestAPI(this.serviceConfig, this.GetModel(request.indexName).IndexName, req);

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

            AzureSearchRESTResponse restResponse = await AzureSearchRestAPI(this.serviceConfig, this.GetModel(request.indexName).IndexName,req);

            // Format the response to our Search Response standard
            SearchResponse response = CreateSearchResponse(restResponse,request.indexName); 

            response.IsSemanticSearch = true;
            response.SemanticAnswers = restResponse?.answers;

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


    }
}
