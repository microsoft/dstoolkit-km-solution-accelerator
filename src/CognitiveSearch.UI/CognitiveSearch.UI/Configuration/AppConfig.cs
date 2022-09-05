// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration.Graph;
using Knowledge.Configuration.Maps;
using Knowledge.Configuration.WebSearch;

namespace CognitiveSearch.UI.Configuration
{
    public class AppConfig
    {
        public OrganizationConfig Organization { get; set; }

        public ClarityConfig Clarity { get; set; }

        public UIConfig UIConfig { get; set; }

        public GraphConfig GraphConfig { get; set; }

        public MapConfig MapConfig { get; set; }

        public WebSearchConfig WebSearchConfig { get; set; }

        public string UIVersion { get; set; }
    }
}