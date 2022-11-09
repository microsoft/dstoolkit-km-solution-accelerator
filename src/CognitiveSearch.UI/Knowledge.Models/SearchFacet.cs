// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Knowledge.Models
{
    public class SearchFacet
    {
        public string? Key { get; set; }
        public FacetValue[] Values { get; set; }
        public string? Type { get; set; }
        public string? Operator { get; set; }
        public string? Target{ get; set; }

        public SearchFacet()
        {
            Values = Array.Empty<FacetValue>();
            // Default Operator for multi-valued facet
            Operator = "any";
        }

        public string GetTarget()
        {
            if (String.IsNullOrEmpty(Target))
            {
                return Key;
            }
            else
            {
                return Target;
            }
        }

        public string GetOperator()
        {
            if (String.IsNullOrEmpty(Operator))
            {
                return "any";
            }
            else
            {
                return Operator;
            }
        }
    }
    public class FacetValue
    {
        public string? value { get; set; }

        [JsonProperty(PropertyName = "count", NullValueHandling = NullValueHandling.Ignore)]
        public long count { get; set; }

        [JsonProperty(PropertyName = "query", NullValueHandling = NullValueHandling.Ignore)]
        public string[] query { get; set; }

        [JsonProperty(PropertyName = "target", NullValueHandling = NullValueHandling.Ignore)]
        public string? target { get; set; }

        [JsonProperty(PropertyName = "logical", NullValueHandling = NullValueHandling.Ignore)]
        public string? logical { get; set; }

        [JsonProperty(PropertyName = "singlevalued", NullValueHandling = NullValueHandling.Ignore)]
        public bool singlevalued { get; set; }

        public FacetValue()
        {
            query = Array.Empty<string>();
        }
    }
}
