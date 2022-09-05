// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Models.Ingress;

namespace Knowledge.Services.WebSearch
{
    public class WebSearchRequest : IngressSearchRequest
    {
        public string clientip { get; set; }

        public int count { get; set; }

        public WebSearchRequest()
        {
            count = 20;
        }
    }
}
