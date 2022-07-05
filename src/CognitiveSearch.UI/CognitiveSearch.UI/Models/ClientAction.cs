// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CognitiveSearch.UI.Models
{
    public class ClientAction
    {
        public string id { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string fonticon { get; set; }
        public string svgicon { get; set; }
        public string method { get; set; }
        public string filter { get;set; }
        public bool isDefault { get; set; }
    }
}