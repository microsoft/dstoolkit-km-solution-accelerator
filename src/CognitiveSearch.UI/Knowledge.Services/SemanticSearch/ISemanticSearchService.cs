// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.SemanticSearch
{
    using Knowledge.Services.AzureSearch.REST;
    using Knowledge.Models;
    using Knowledge.Models.Ingress;
    using System.Threading.Tasks;

    public interface ISemanticSearchService
    {
        Task<AzureSearchRESTResponse> GetSemanticRESTResults(IngressSearchRequest request);

        Task<SearchResponse> GetSemanticResults(IngressSearchRequest request);
    }
}
