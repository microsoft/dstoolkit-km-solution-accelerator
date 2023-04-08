// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.API.Models;
using Knowledge.Configuration;
using Knowledge.Models;
using Knowledge.Services;
using Knowledge.Services.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Knowledge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LanguageController : AbstractApiController
    {
        private IMetadataService metadataService { get; set; }

        private const string DEFAULT_INDEX_NAME = "index";

        public LanguageController(IQueryService client, SearchServiceConfig svcconfig, IMetadataService msvc)
        {
            QueryService = client;
            Config = svcconfig;
            this.metadataService = msvc;
        }

        [HttpPost("question")]
        public async Task<IActionResult> AskQuestion(ApiSearchRequest request)
        {
            request.indexName = DEFAULT_INDEX_NAME;
            request.permissions = GetUserPermissions();

            var result = await QueryService.GetDocumentByIndexKey(request);

            return CreateContentResultResponse(result);
        }
    }
}
