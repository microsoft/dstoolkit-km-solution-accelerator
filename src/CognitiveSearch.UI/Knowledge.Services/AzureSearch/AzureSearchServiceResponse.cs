// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Knowledge.Services.AzureSearch
{
    public class AzureSearchServiceResponse
    {
        public AzureSearchServiceResponse()
        {
            results = new List<Dictionary<string, object>>();
        }

        public IList<Dictionary<string,object>> results { get; set; }
    }
}

