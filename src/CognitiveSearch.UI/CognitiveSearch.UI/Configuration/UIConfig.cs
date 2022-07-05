// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Models;
using System.Collections.Generic;

namespace CognitiveSearch.UI.Configuration
{
    public class UIConfig
    {
        public string Language{ get; set; }
        public List<string> ColorPalette { get; set; }
        public bool UploadData { get; set; }

        public DashboardPage LandingPage { get; set; }

        public List<SearchVertical> Verticals { get; set; }

        public List<SearchVertical> GetVerticalsFromLanguage()
        {
            return Verticals;
        }

        public List<AbstractPage> GetPages()
        {
            List<AbstractPage> pages = new();
            pages.AddRange(Verticals);

            return pages;
        }

        public SearchVertical GetVerticalByName(string name)
        {
            foreach (SearchVertical item in Verticals)
            {
                if ( item.name.ToLowerInvariant().Equals(name.ToLowerInvariant()))
                {
                    return item;
                }
            }

            return null;
        }
        public SearchVertical GetVerticalById(string id)
        {
            foreach (SearchVertical item in Verticals)
            {
                if (item.id.ToLowerInvariant().Equals(id.ToLowerInvariant()))
                {
                    return item;
                }
            }

            return null;
        }       
    }
}
