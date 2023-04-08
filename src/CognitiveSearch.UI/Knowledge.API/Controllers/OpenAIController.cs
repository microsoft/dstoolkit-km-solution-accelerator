// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.API.Models;
using Knowledge.Services.OpenAI;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knowledge.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OpenAIController : AbstractApiController {

        private readonly IOpenAIService oaiService;

        public OpenAIController(IOpenAIService client) {
            this.oaiService = client;
        }

        // [HttpPost("ask")]
        // public async Task<IActionResult> Completion(ApiChatRequest request) {

        //     var result = await this.chatService.Ask(request);

        //     return CreateContentResultResponse(result);
        // }

        [HttpPost("chat")]
        public async Task<IActionResult> ChatCompletion(ApiChatRequest request)
        {

            var result = await this.oaiService.ChatCompletion(request);

            return CreateContentResultResponse(result);
        }

        [HttpPost("completion")]
        public async Task<IActionResult> Completion(ApiChatRequest request) {

            var result = await this.oaiService.Completion(request);

            return CreateContentResultResponse(result);
        }
    }
}
