// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Models.Ingress
{
    //See https://docs.microsoft.com/en-us/rest/api/searchservice/suggestions#request-body 

    public class IngressSuggestionRequest
    {
        public string indexName { get; set; }
        public string term { get; set; } 
        public string targetField { get; set; }
        public string suggester { get; set; }
        public string filter { get; set; }
        public bool fuzzy { get; set; }
        public bool highlights { get; set; } 

        public IngressSuggestionRequest()
        {
            targetField = "key_phrases";
            suggester = "suggester1";
        }
    }
}
