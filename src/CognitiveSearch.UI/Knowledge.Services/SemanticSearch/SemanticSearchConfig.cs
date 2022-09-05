// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration;

namespace Knowledge.Services.SemanticSearch
{
    public class SemanticSearchConfig : AbstractServiceConfig
    {
        public string queryType { get; set; }
        public string searchFields { get; set; }
        public string semanticConfiguration { get; set; }
        public string queryLanguage {  get; set; }
        public string speller {  get; set; }
        public string answers {  get; set; }
        public bool count {  get; set; }
        public string highlightPreTag { get; set; }
        public string highlightPostTag { get; set; }
        public int top { get; set; }
    }
}
