// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.AzureSearch.REST
{
    using Knowledge.Configuration;
    using Knowledge.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    public class AzureSearchRESTService : AbstractSearchService
    {
        private static readonly HttpClient RestHTTPClient = new();

        public static async Task<AzureSearchRESTResponse> AzureSearchRestAPI(SearchServiceConfig serviceConfig, string indexName, object request)
        {
            AzureSearchRESTResponse jsonresponse = new();

            string searchServiceUrl = "https://" + serviceConfig.ServiceName + ".search.windows.net/indexes/" + indexName + "/docs/search?api-version=" + serviceConfig.APIVersion;

            string azureSearchResult = null;

            string reqbody = Newtonsoft.Json.JsonConvert.SerializeObject(request); 

            using (HttpRequestMessage req = new(HttpMethod.Post, searchServiceUrl))
            {
                req.Headers.Add("Accept", "application/json");
                req.Headers.Add("api-key", serviceConfig.QueryKey);
                req.Content = new StringContent(reqbody);
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage similarResponse = await RestHTTPClient.SendAsync(req);

                if (similarResponse.IsSuccessStatusCode)
                {
                    azureSearchResult = await similarResponse.Content.ReadAsStringAsync();

                    if ( azureSearchResult != null) jsonresponse = Newtonsoft.Json.JsonConvert.DeserializeObject<AzureSearchRESTResponse>(azureSearchResult);
                }
            }

            return jsonresponse;
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
