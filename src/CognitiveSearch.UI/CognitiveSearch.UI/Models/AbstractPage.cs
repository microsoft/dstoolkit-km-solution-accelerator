// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Services.Models;
using System.Collections.Generic;
namespace CognitiveSearch.UI.Models
{
    public abstract class AbstractPage
    {
        public string pageTitle { get; set; }
        public string path { get; set; }

        public string searchMethod { get; set; }
        public Dictionary<string, Localization> Localizations { get; set; }

        // Security
        public bool isSecured { get; set; }
        public string userRole { get; set; }

        public bool enable { get; set; }
        public string fonticon { get; set; }
        public string svgicon { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string altTitle { get; set; }

    }

    public class PageInsights
    {
        public string id { get; set; }
        public bool enable { get; set; }
        public string title { get; set; }
        public string icon { get; set; }
        public string method { get; set; }
        public InsightParameters parameters { get; set; }
        public List<InsightLayout> layouts { get; set; }

        public PageInsights()
        {
            layouts = new List<InsightLayout>();
        }
    }

    public class InsightParameters
    {
        public string tablename { get; set; }

        public string tag_id { get; set; }
        public string tag_class { get; set; }
        public string update_method { get; set; }
        public List<string> extras { get; set; }
        public List<SearchFacet> facets { get; set; }
    }

    public class InsightLayout
    {
        public string divid { get; set; }
        public string divclass { get; set; }
    }
}