// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.API.Models;
using Knowledge.Configuration;
using Knowledge.Models;
using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knowledge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SearchController : AbstractApiController
    {
        private const string DEFAULT_INDEX_NAME = "index";

        public SearchController(IQueryService client, SearchServiceConfig svcconfig)
        {
            QueryService = client;
            Config = svcconfig;
        }

        [HttpPost("getdocuments")]
        public async Task<IActionResult> GetDocumentsAsync(ApiSearchRequest request)
        {
            request.indexName = DEFAULT_INDEX_NAME;
            request.permissions = GetUserPermissions();
            SearchResponse result = await QueryService.GetDocumentsAsync(request);

            return CreateContentResultResponse(result);
            //return new JsonResult(result);
        }

        [HttpPost("getlatestdocuments")]
        public async System.Threading.Tasks.Task<IActionResult> GetLatestDocumentsAsync(ApiSearchRequest request)
        {
            request.indexName = DEFAULT_INDEX_NAME;
            request.permissions = GetUserPermissions();
            SearchResponse result = await QueryService.GetLatestDocumentsAsync(request);

            return CreateContentResultResponse(result);
        }

        [Route("autocomplete")]
        [HttpPost]
        public async Task<IActionResult> AutoCompleteAsync(string term,
                                                           string? targetField = "key_phrases",
                                                           string? suggester = "suggester1",
                                                           string? indexName = DEFAULT_INDEX_NAME)
        {
            List<string> candidates = new List<string>();

            if (!string.IsNullOrEmpty(term))
            {
                ApiSuggestionRequest request = new ApiSuggestionRequest
                {
                    indexName = indexName,
                    term = term,
                    targetField=targetField,
                    suggester=suggester
                };

                // Change to _docSearch.Suggest if you would prefer to have suggestions instead of auto-completion
                var response = await QueryService.AutocompleteAsync(request);

                if (response != null)
                {
                    candidates = response;
                }
            }

            return CreateContentResultResponse(candidates);
        }

        [Route("suggest")]
        [HttpPost]

        public async Task<IActionResult> SuggestAsync(string term,
                                                      string? targetField = "key_phrases",
                                                      bool fuzzy = true,
                                                      bool highlights = true,
                                                      string? suggester = "suggester1",
                                                      string? indexName = DEFAULT_INDEX_NAME,
                                                      string? filter = null)
        {
            List<string> uniqueItems = new();

            if (!string.IsNullOrEmpty(term))
            {
                ApiSuggestionRequest request = new ApiSuggestionRequest
                {
                    indexName = indexName,
                    term = term,
                    targetField = targetField,
                    fuzzy=fuzzy,
                    highlights=highlights,
                    suggester = suggester,
                    filter = filter
                };

                // Change to _docSearch.Suggest if you would prefer to have suggestions instead of auto-completion
                var response = await QueryService.SuggestAsync(request);

                if (response != null)
                {
                    uniqueItems = response.Distinct().ToList();
                }
            }

            return CreateContentResultResponse(uniqueItems);
        }

        [HttpPost("getimages")]
        public async System.Threading.Tasks.Task<IActionResult> GetImagesAsync(ApiSearchRequest request)
        {
            request.indexName = DEFAULT_INDEX_NAME;
            request.permissions = GetUserPermissions();
            SearchResponse result = await QueryService.GetImagesAsync(request);

            return CreateContentResultResponse(result);
        }

        [HttpPost("getlatestimages")]
        public async System.Threading.Tasks.Task<IActionResult> GetLatestImagesAsync(ApiSearchRequest request)
        {
            request.indexName = DEFAULT_INDEX_NAME;
            request.permissions = GetUserPermissions();
            SearchResponse result = await QueryService.GetLatestImagesAsync(request);

            return CreateContentResultResponse(result);
        }

        [HttpPost("getvideos")]
        public async System.Threading.Tasks.Task<IActionResult> GetVideosAsync(ApiSearchRequest request)
        {
            request.indexName = DEFAULT_INDEX_NAME;
            request.permissions = GetUserPermissions();
            SearchResponse result = await QueryService.GetVideosAsync(request);

            return CreateContentResultResponse(result);
        }
    }
}
