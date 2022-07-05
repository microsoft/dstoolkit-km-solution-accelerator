// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace CognitiveSearch.UI.Models
{
    public class DashboardPage : AbstractPage
    {
        public string Title { get; set; }
        public string PlaceHolder { get; set; }
        public bool WebFacet { get; set; }

        public List<PageHighlight> Highlights { get; set; }

        public DashboardPage()
        {
            Highlights = new List<PageHighlight>();
        }

        public string GetSearchInputPlaceHolder()
        {
            return Localizations["en"].placeHolder;
        }
        public string GetSearchInputTitle()
        {
            return Localizations["en"].title;
        }
    }

    public class PageHighlight
    {
        public string id { get; set; }
        public bool enable { get; set; }
        public string title { get; set; }
        public string icon { get; set; }
        public string url { get; set; }

        public List<PageInsights> Insights { get; set; }

        public PageHighlight()
        {
            Insights = new List<PageInsights>();
        }
    }

}
