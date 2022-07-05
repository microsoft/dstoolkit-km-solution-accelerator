// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Services.Models;
using System;

namespace CognitiveSearch.UI.Models
{
    public class SearchViewModel
    {
        public string currentQuery { get; set; }

        public SearchFacet[] selectedFacets { get; set; }

        public int currentPage { get; set; }

        public string searchId { get; set; }

        public AbstractPage config { get; set; }

        public SearchViewModel()
        {
            selectedFacets = Array.Empty<SearchFacet>();
        }

        // Only use on dashboard views like configuration or Home page...
        public DashboardPage GetDashboardPageConfig()
        {
            return (DashboardPage)config;
        }
    }
}