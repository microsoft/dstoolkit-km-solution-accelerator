// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Models.Ingress;
using System.Collections.Generic;

namespace Knowledge.Services.Graph
{
    public class GraphRequest : IngressSearchRequest
    {
        public List<string> facets { get; set; }
        public string graphType { get; set; }
        public int maxLevels { get; set; }
        public int maxNodes { get; set; }
        public string model { get; set; }

        public GraphRequest()
        {
            model = "model2";
        }
    }
}
