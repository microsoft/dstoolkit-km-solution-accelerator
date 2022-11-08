// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.AzureSearch
{
    using Knowledge.Configuration;
    using Knowledge.Configuration.AzureStorage;
    using Knowledge.Configuration.Graph;
    using Knowledge.Configuration.SemanticSearch;
    using Knowledge.Models;
    using Knowledge.Models.Ingress;
    using Knowledge.Services.AzureSearch.REST;
    using Knowledge.Services.AzureSearch.SDK;
    using Knowledge.Services.SemanticSearch;
    using Microsoft.ApplicationInsights;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using System.Threading.Tasks;

    public class AzureSearchService : AzureSearchSDKService, IAzureSearchService
    {
        public AzureSearchService(TelemetryClient telemetry, SearchServiceConfig configuration, SemanticSearchConfig semanticCfg, StorageConfig strCfg, ISemanticSearchService semanticSvc, GraphConfig graphConfig) : base(telemetry, configuration, semanticCfg, strCfg, semanticSvc, graphConfig)
        {
            this.telemetryClient = telemetry;
            this.serviceConfig = configuration;
            this.storageConfig = strCfg;

            this.semanticConfig = semanticCfg;
            this.semanticSearch = semanticSvc;

            this.graphConfig = graphConfig;

            InitSearchClients();
        }

        //
        // https://docs.microsoft.com/en-us/azure/search/search-more-like-this
        //
        
        public new async Task<SearchResponse> GetSimilarDocuments(IngressSearchRequest request)
        {
            AzureSearchSimilarParameters sp = new()
            {
                top = 20,
                searchFields = "keyPhrases",
                moreLikeThis = request.index_key
            };

            // Search REST Client
            AzureSearchRESTResponse jsonresponse = await AzureSearchRESTService.AzureSearchRestAPI(this.serviceConfig, this.serviceConfig.IndexName, sp);

            return CreateSearchResponse(jsonresponse, this.serviceConfig.IndexName);
        }

        public SearchResponse CreateSearchResponse(AzureSearchRESTResponse response, string indexName = null)
        {
            Dictionary<string, string> s_tokens = GetContainerSasUris();

            var facetResults = new Dictionary<string, IList<FacetValue>>();

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
                }
            }

            var result = new SearchResponse
            {
                IndexName = indexName,
                Results = (response?.documents),
                Facets = facetResults,
                Count = (response == null ? 0 : Convert.ToInt32(response.count)),
                IdField = this.serviceConfig.KeyField,
                Tokens = s_tokens,
                IsPathBase64Encoded = this.serviceConfig.IsPathBase64Encoded,
            };
            return result;
        }

    }
}
