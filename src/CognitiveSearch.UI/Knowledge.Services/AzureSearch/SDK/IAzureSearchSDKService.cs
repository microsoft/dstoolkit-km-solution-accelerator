// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.AzureSearch.SDK
{
    using Azure.Search.Documents;
    using Azure.Search.Documents.Models;
    using Knowledge.Configuration;
    using Knowledge.Models;
    using System;
    using System.Collections.Generic;

    public interface IAzureSearchSDKService : IAzureSearchService
    {
        public SearchOptions GenerateSearchOptions(QueryParameters parameters, 
                                                   SearchFacet[] searchFacets = null,
                                                   string[] selectFields = null,
                                                   List<String> selectFacets = null,
                                                   int currentPage = 1,
                                                   string polygonString = null,
                                                   string incomingfilter = "",
                                                   SearchPermission[] permissions = null, 
                                                   string indexName = null,
                                                   string[] contentSources = null);

        public SearchResults<SearchDocument> SearchDocuments(string indexName, 
                                                             string searchText,
                                                             SearchOptions options);
        public SearchServiceConfig GetSearchConfig();
    }
}
