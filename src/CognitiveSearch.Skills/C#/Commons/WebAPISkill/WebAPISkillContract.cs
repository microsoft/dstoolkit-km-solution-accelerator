// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Services.Common.WebApiSkills
{
    public class WebApiSkillRequest
    {
        public List<WebApiRequestRecord> Values { get; set; } = new List<WebApiRequestRecord>();
    }

    public class WebApiSkillResponse
    {
        public List<WebApiResponseRecord> Values { get; set; } = new List<WebApiResponseRecord>();
    }
   
    public class WebApiResponseError
    {
        public string Message { get; set; }
    }

    public class WebApiResponseWarning
    {
        public string Message { get; set; }
    }

    public class WebApiRequestRecord
    {
        [JsonProperty(PropertyName = "recordId")]
        public string RecordId { get; set; }

        [JsonProperty(PropertyName = "data")]
        public Dictionary<string, object> Data { get; set; }
    }

    public class WebApiResponseRecord
    {
        [JsonProperty(PropertyName = "recordId")]
        public string RecordId { get; set; }

        [JsonProperty(PropertyName = "data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        [JsonProperty(PropertyName = "errors")]
        public List<WebApiResponseError> Errors { get; set; } = new List<WebApiResponseError>();

        [JsonProperty(PropertyName = "warnings")]
        public List<WebApiResponseWarning> Warnings { get; set; } = new List<WebApiResponseWarning>();
    }

    public class WebApiEnricherResponse
    {
        [JsonProperty(PropertyName = "values")]
        public List<WebApiResponseRecord> Values { get; set; }
    }
    public class FileReference
    {
        public string data { get; set; }
    }
}
