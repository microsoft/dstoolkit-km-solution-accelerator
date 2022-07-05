// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CognitiveSearch.UI.Models
{
    public class Suggestion
    {
        public string name { get; set; }
        public string target { get; set; }
        public string url { get; set; }
        public SuggestionTemplate template { get; set; }
    }

    public class SuggestionTemplate
    {
        public string header { get; set; }
    }
}