// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class BaseQueryEntity
    {
        [JsonProperty(PropertyName = "Language")]
        public string Language { get; set; }
    }

    public class SearchRequest : BaseQueryEntity
    {
        [JsonProperty(PropertyName = "QueryText")]
        public string QueryText { get; set; }

        public string EscapedQueryText { get; set; }

        public string EscapedTranslatedQueryText { get; set; }

        public string TranslatedQueryText { get; set; }
      
        public string IndexId { get; set; }

        [JsonProperty(PropertyName = "PageNumber")]
        public int PageNumber { get; set; }

        [JsonProperty(PropertyName = "PageSize")]
        public int PageSize { get; set; }
    }  
}
