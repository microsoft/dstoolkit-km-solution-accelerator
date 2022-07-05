// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.WebSearch
{
    using System.Threading.Tasks;

    public interface IWebSearchService
    {
        bool IsWebSearchEnable();

        Task<string> GetWebResults(WebSearchRequest request);
        Task<string> GetNewsResults(WebSearchRequest request);
        Task<string> GetImagesResults(WebSearchRequest request);
        Task<string> GetVideosResults(WebSearchRequest request);
    }
}
