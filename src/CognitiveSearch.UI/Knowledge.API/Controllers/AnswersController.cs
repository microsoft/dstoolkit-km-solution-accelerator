// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.API.Models;
using Knowledge.Services;
using Knowledge.Services.AzureSearch.REST;
using Knowledge.Configuration;
using Knowledge.Services.SemanticSearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Knowledge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnswersController : CustomControllerBase
    {
        ISemanticSearchService semanticService;

        public AnswersController(IQueryService client, ISemanticSearchService semantic, SearchServiceConfig svcconfig)
        {
            QueryService = client;
            semanticService = semantic;
            Config = svcconfig;
        }


        [HttpPost("getanswers")]
        public async Task<IActionResult> GetAnswersAsync(ApiSearchRequest request)
        {
            request.permissions = GetUserPermissions();

            AzureSearchRESTResponse result = await semanticService.GetSemanticRESTResults(request);

            return CreateContentResultResponse(result);
        }

    }
}
