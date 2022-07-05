// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.API.Models;
using Knowledge.Services;
using Knowledge.Services.AzureSearch.REST;
using Knowledge.Services.Configuration;
using Knowledge.Services.SemanticSearch;
using Microsoft.AspNetCore.Mvc;

namespace Knowledge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class AnswersController : AbstractApiController
    {
        ISemanticSearchService semanticService;

        public AnswersController(IQueryService client, ISemanticSearchService semantic, SearchServiceConfig svcconfig)
        {
            _queryService = client;
            semanticService = semantic;
            _config = svcconfig;
        }


        [HttpPost("getanswers")]
        public async System.Threading.Tasks.Task<IActionResult> GetAnswersAsync(ApiSearchRequest request)
        {
            request.permissions = GetUserPermissions();

            AzureSearchRESTResponse result = await semanticService.GetSemanticRESTResults(request);

            return CreateContentResultResponse(result);
        }

    }
}
