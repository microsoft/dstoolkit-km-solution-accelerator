// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Models.Chat;
using Knowledge.Services.Chat;
using Knowledge.Services.Chat.PromptFlow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knowledge.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : CustomControllerBase {

        private readonly IPromptFlowChatService _promptFlowService;
        private readonly IChatHistoryService _chatHistory;


        public ChatController(IPromptFlowChatService promptFlowService, IChatHistoryService chatHistory)
        {
            _promptFlowService = promptFlowService;
            _chatHistory = chatHistory;
        }
               

        [HttpPost("Completion")]
        public async Task<IActionResult> ChatCompletion(ChatRequest request)
        {
            var user = GetUserActionMetadata();
            var result = await _promptFlowService.ChatCompletion(request, user.ObjectId, string.Empty); // manage current session id in memory (?)
            return CreateContentResultResponse(result);
        }

        [HttpGet("Models")]
        public async Task<IActionResult> GetAvailableLLMModels()
        {
            var result = await _promptFlowService.GetAvailableLLMModels();
            return CreateContentResultResponse(result);
        }


        [HttpGet("Sources")]
        public async Task<IActionResult> GetAvailableLLMDataSources()
        {
            var result = await _promptFlowService.GetAvailableLLMDataSources();
            return CreateContentResultResponse(result);
        }




        [HttpGet("Sessions")]
        public async Task<IActionResult> GetChatSessions()
        {
            var user = GetUserActionMetadata();
            var sessions = await _chatHistory.GetChatSessions(user.ObjectId);
            return CreateContentResultResponse(sessions);
        }


        [HttpGet("Session/{sessionId}/History")]
        public async Task<IActionResult> GetChatHistory(string sessionId = "")
        {
            var user = GetUserActionMetadata();
            var sessions = await _chatHistory.GetChatHistory(user.ObjectId, sessionId);
            return CreateContentResultResponse(sessions);
        }      

        [HttpPost("Sessions")]
        public async Task<IActionResult> CreateChatSession()
        {
            var user = GetUserActionMetadata();
            var sessionId = await _chatHistory.CreateChatSession(user.ObjectId, Guid.NewGuid().ToString());
            return CreateContentResultResponse(sessionId);
        }

        [HttpDelete("Sessions/{sessionId}")]
        public async Task<IActionResult> DeleteChatSession(string sessionId)
        {
            var user = GetUserActionMetadata();
            var result = await _chatHistory.RemoveChatSession(user.ObjectId, sessionId);
            return CreateContentResultResponse(result);
        }

    }
}
