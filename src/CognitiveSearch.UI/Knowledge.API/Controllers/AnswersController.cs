// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Services;
using Knowledge.Services.AzureSearch.REST;
using Knowledge.Configuration;
using Knowledge.Services.SemanticSearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Knowledge.Models;
using Knowledge.Models.Ingress;

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
        public async Task<IActionResult> GetAnswersAsync(IngressSearchRequest request)
        {
            request.permissions = GetUserPermissions();

            AzureSearchRESTResponse result = await semanticService.GetSemanticRESTResults(request);

            return CreateContentResultResponse(result);
        }

    }
}
