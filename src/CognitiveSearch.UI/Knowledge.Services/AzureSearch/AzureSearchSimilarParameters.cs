// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.AzureSearch
{
    public class AzureSearchSimilarParameters{

        public string moreLikeThis {get;set;}

        public string searchFields {get;set;}

        public int top {get; set;}
    }
}
