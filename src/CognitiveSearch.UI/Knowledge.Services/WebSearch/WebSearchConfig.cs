// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.WebSearch
{
    public class WebSearchConfig : AbstractServiceConfig
    {
        public string SubscriptionKey { get; set; }
        public string Endpoint { get; set; }
        public string ResponseFilter { get; set; }
        public string Market { get; set; }
        public int CacheExpirationTime { get; set; }
    }
}
