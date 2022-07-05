// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.AzureSearch.REST
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class AzureSearchRESTResponse
    {
        public AzureSearchRESTResponse()
        {
            facets = new Dictionary<string, List<FacetEntry>>();
            documents = new List<Dictionary<string, object>>();
            answers = new List<Dictionary<string, object>>();
        }

        [JsonProperty(PropertyName = "@search.answers", NullValueHandling = NullValueHandling.Ignore)]
        public List<Dictionary<string, object>> answers { get; set; }

        [JsonProperty(PropertyName = "@odata.count", NullValueHandling = NullValueHandling.Ignore)]
        public int count { get; set; }

        [JsonProperty(PropertyName = "@search.facets", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, List<FacetEntry>> facets { get; set; }

        [JsonProperty(PropertyName = "tokens", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> tokens { get; set; }

        [JsonProperty(PropertyName = "value", NullValueHandling = NullValueHandling.Ignore)]
        public List<Dictionary<string, object>> documents { get; set; }
    }


    public class FacetEntry
    {
        public int count { get; set; }
        public string value { get; set; }
        public string from { get; set; }
        public string to { get; set; }
    }
}
