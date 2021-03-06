// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Services.Configuration;
using Knowledge.Services.Graph;
using Knowledge.Services.WebSearch;
using Knowledge.Services.Maps;

namespace CognitiveSearch.UI.Configuration
{
    public class AppConfig
    {
        public OrganizationConfig Organization { get; set; }

        public UIConfig UIConfig { get; set; }

        public SearchServiceConfig SearchConfig { get; set; }

        public GraphConfig GraphConfig { get; set; }

        public MapConfig MapConfig { get; set; }

        public WebSearchConfig WebSearchConfig { get; set; }

        public string UIVersion { get; set; }
    }
}