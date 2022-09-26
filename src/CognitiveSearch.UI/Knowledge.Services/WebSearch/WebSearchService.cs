// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration.WebSearch;
using Knowledge.Services.Helpers;
using Microsoft.ApplicationInsights;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Knowledge.Services.WebSearch
{
    // https://github.com/microsoft/bing-search-dotnet-samples/blob/main/rest/quickstarts/WebSearch.cs

    public class WebSearchService : AbstractService, IWebSearchService
    {
        static string path = "/v7.0/search";

        static string newsPath = "/v7.0/news/search";
        static string newsTrendsPath = "/v7.0/news/trendingtopics";

        static string imagesPath = "/v7.0/images/search";
        static string videosPath = "/v7.0/videos/search";

        // Each of the query parameters you may specify.
        private const string QUERY_PARAMETER = "?q=";  // Required
        private const string MKT_PARAMETER = "&mkt=";  // Strongly suggested
        private const string RESPONSE_FILTER_PARAMETER = "&responseFilter=";
        private const string COUNT_PARAMETER = "&count=";
        private const string OFFSET_PARAMETER = "&offset=";
        private const string FRESHNESS_PARAMETER = "&freshness=";
        private const string SAFE_SEARCH_PARAMETER = "&safeSearch=";
        private const string TEXT_DECORATIONS_PARAMETER = "&textDecorations=";
        private const string TEXT_FORMAT_PARAMETER = "&textFormat=";
        private const string ANSWER_COUNT = "&answerCount=";
        private const string PROMOTE = "&promote=";

        // Bing uses the X-MSEdge-ClientID header to provide users with consistent
        // behavior across Bing API calls. See the the reference documentation
        // fo usage.

        //private static string _clientIdHeader = null;

        private readonly WebSearchConfig WebSearchConfig;

        public WebSearchService(TelemetryClient telemetry, WebSearchConfig webConfig)
        {
            this.telemetryClient = telemetry;
            this.WebSearchConfig = webConfig;
        }

        public async Task<string> GetWebResults(WebSearchRequest request)
        {
            if (!this.WebSearchConfig.IsEnabled)
            {
                return "{}";
            }

            var rawQuery = String.Empty;

            var queryText = String.Empty;

            var freshness_filter = String.Empty;

            if (!String.IsNullOrEmpty(request.queryText) && !request.queryText.Equals(QueryHelper.QUERY_ALL))
            {
                queryText += "(" + request.queryText + ")";
            }

            // Facets
            var facetQueryText = String.Empty;

            if (request.searchFacets != null)
            {
                bool firstFacet = true;
                foreach (var item in request.searchFacets)
                {

                    if (item.Type == "daterange")
                    {
                        //&freshness=2019-02-01..2019-05-30
                        freshness_filter = item.Values[0].value + ".." + item.Values[1].value;

                        continue;
                    }

                    if (!firstFacet)
                    {
                        facetQueryText += " AND (";
                    }
                    else
                    {
                        facetQueryText += "(";
                        firstFacet = false;
                    }

                    bool firstFacetValue = true;

                    foreach (var facetValue in item.Values)
                    {
                        if (!firstFacetValue)
                        {
                            facetQueryText += " AND (";
                        }
                        else
                        {
                            facetQueryText += "(";
                            firstFacetValue = false;
                        }

                        if (facetValue.query != null)
                        {
                            if (facetValue.query.Length > 0)
                            {
                                facetQueryText += " (";
                                facetQueryText += string.Join(") OR (", facetValue.query);
                                facetQueryText += " )";
                            }
                            else
                            {
                                facetQueryText += " (" + facetValue.value + ")";
                            }
                        }
                        else
                        {
                            facetQueryText += " (" + facetValue.value + ")";
                        }
                        // Facet Value entry closure
                        facetQueryText += ") ";
                    }

                    facetQueryText += ") ";
                }
            }

            if (String.IsNullOrEmpty(queryText))
            {
                rawQuery = facetQueryText;
            }
            else
            {
                if (String.IsNullOrEmpty(facetQueryText))
                {
                    rawQuery = queryText;
                }
                else
                {
                    rawQuery = queryText + " AND " + facetQueryText;
                }
            }

            // Query Filter 
            if (!String.IsNullOrEmpty(request.incomingFilter))
            {
                if (!request.incomingFilter.StartsWith("responseFilter"))
                {
                    rawQuery += (" AND " + request.incomingFilter);
                }
            }

            // Return an empty response here
            if (String.IsNullOrEmpty(rawQuery))
            {
                return "{}";
            }

            // Remember to encode query parameters like q, responseFilters, promote, etc.
            var queryString = QUERY_PARAMETER + Uri.EscapeDataString(rawQuery.Trim());

            // Response Filter 
            if (!String.IsNullOrEmpty(request.incomingFilter))
            {
                if (request.incomingFilter.StartsWith("responseFilter="))
                {
                    queryString += RESPONSE_FILTER_PARAMETER + Uri.EscapeDataString(request.incomingFilter.Replace("responseFilter=", String.Empty));
                }
            }

            queryString += MKT_PARAMETER + this.WebSearchConfig.Market;
            queryString += TEXT_DECORATIONS_PARAMETER + Boolean.TrueString;

            if (!String.IsNullOrEmpty(freshness_filter))
            {
                queryString += FRESHNESS_PARAMETER + freshness_filter;
            }
            queryString += COUNT_PARAMETER + request.count;
            // Paginate - infinite scroll
            queryString += OFFSET_PARAMETER + ((request.currentPage - 1) * request.count);
            queryString += SAFE_SEARCH_PARAMETER + "Strict";
            queryString += TEXT_FORMAT_PARAMETER + "HTML";

            string contentString = "{}";

            //this.httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this.WebSearchConfig.SubscriptionKey);

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, WebSearchConfig.Endpoint + path + queryString))
            {
                httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", this.WebSearchConfig.SubscriptionKey);

                // Request headers. The subscription key is the only required header but you should
                // include User-Agent (especially for mobile), X-MSEdge-ClientID, X-Search-Location
                // and X-MSEdge-ClientIP (especially for local aware queries).
                httpRequest.Headers.Add("X-MSEdge-ClientIP", request.clientip);

                var response = await httpClient.SendAsync(httpRequest);

                response.EnsureSuccessStatusCode();

                contentString = await response.Content.ReadAsStringAsync();
            }

            return contentString;
        }


        public async Task<string> GetNewsResults(WebSearchRequest request)
        {
            return await InvokeBing(request, newsPath);
        }

        public async Task<string> GetImagesResults(WebSearchRequest request)
        {
            return await InvokeBing(request, imagesPath);
        }

        public async Task<string> GetVideosResults(WebSearchRequest request)
        {
            return await InvokeBing(request, videosPath);
        }

        private async Task<string> InvokeBing(WebSearchRequest request, string endpoint)
        {
            if (!this.WebSearchConfig.IsEnabled)
            {
                return "{}";
            }

            var rawQuery = String.Empty;

            var queryText = String.Empty;

            var freshness_filter = String.Empty;

            if (!String.IsNullOrEmpty(request.queryText) && !request.queryText.Equals(QueryHelper.QUERY_ALL))
            {
                queryText += "(" + request.queryText + ")";
            }

            // Facets
            var facetQueryText = String.Empty;

            if (request.searchFacets != null)
            {
                bool firstFacet = true;
                foreach (var item in request.searchFacets)
                {

                    if (item.Type == "daterange")
                    {
                        //&freshness=2019-02-01..2019-05-30
                        freshness_filter = item.Values[0].value + ".." + item.Values[1].value;

                        continue;
                    }

                    if (!firstFacet)
                    {
                        facetQueryText += " AND (";
                    }
                    else
                    {
                        facetQueryText += "(";
                        firstFacet = false;
                    }

                    bool firstFacetValue = true;

                    foreach (var facetValue in item.Values)
                    {
                        if (!firstFacetValue)
                        {
                            facetQueryText += " AND (";
                        }
                        else
                        {
                            facetQueryText += "(";
                            firstFacetValue = false;
                        }

                        if (facetValue.query != null)
                        {
                            if (facetValue.query.Length > 0)
                            {
                                facetQueryText += " (";
                                facetQueryText += string.Join(") OR (", facetValue.query);
                                facetQueryText += " )";
                            }
                            else
                            {
                                facetQueryText += " (" + facetValue.value + ")";
                            }
                        }
                        else
                        {
                            facetQueryText += " (" + facetValue.value + ")";
                        }
                        // Facet Value entry closure
                        facetQueryText += ") ";
                    }

                    facetQueryText += ") ";
                }
            }

            if (String.IsNullOrEmpty(queryText))
            {
                rawQuery = facetQueryText;
            }
            else
            {
                if (String.IsNullOrEmpty(facetQueryText))
                {
                    rawQuery = queryText;
                }
                else
                {
                    rawQuery = queryText + " AND " + facetQueryText;
                }
            }

            // Query Filter 
            if (!String.IsNullOrEmpty(request.incomingFilter))
            {
                if (!request.incomingFilter.StartsWith("responseFilter"))
                {
                    rawQuery += (" AND " + request.incomingFilter);
                }
            }

            // Return an empty response here
            if (String.IsNullOrEmpty(rawQuery))
            {
                return "{}";
            }

            // Remember to encode query parameters like q, responseFilters, promote, etc.
            var queryString = QUERY_PARAMETER + Uri.EscapeDataString(rawQuery.Trim());

            // Response Filter 
            if (!String.IsNullOrEmpty(request.incomingFilter))
            {
                if (request.incomingFilter.StartsWith("responseFilter="))
                {
                    queryString += RESPONSE_FILTER_PARAMETER + Uri.EscapeDataString(request.incomingFilter.Replace("responseFilter=", String.Empty));
                }
            }

            queryString += MKT_PARAMETER + this.WebSearchConfig.Market;
            queryString += TEXT_DECORATIONS_PARAMETER + Boolean.TrueString;

            if (!String.IsNullOrEmpty(freshness_filter))
            {
                queryString += FRESHNESS_PARAMETER + freshness_filter;
            }
            queryString += COUNT_PARAMETER + request.count;
            // Paginate - infinite scroll
            queryString += OFFSET_PARAMETER + ((request.currentPage - 1) * request.count);
            queryString += SAFE_SEARCH_PARAMETER + "Strict";
            queryString += TEXT_FORMAT_PARAMETER + "HTML";

            string contentString = "{}";

            //this.httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this.WebSearchConfig.SubscriptionKey);

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, WebSearchConfig.Endpoint + endpoint + queryString))
            {
                httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", this.WebSearchConfig.SubscriptionKey);

                // Request headers. The subscription key is the only required header but you should
                // include User-Agent (especially for mobile), X-MSEdge-ClientID, X-Search-Location
                // and X-MSEdge-ClientIP (especially for local aware queries).
                httpRequest.Headers.Add("X-MSEdge-ClientIP", request.clientip);

                var response = await httpClient.SendAsync(httpRequest);

                response.EnsureSuccessStatusCode();

                contentString = await response.Content.ReadAsStringAsync();
            }

            return contentString;
        }


        public bool IsWebSearchEnable()
        {
            return WebSearchConfig.IsEnabled;
        }
    }
}
