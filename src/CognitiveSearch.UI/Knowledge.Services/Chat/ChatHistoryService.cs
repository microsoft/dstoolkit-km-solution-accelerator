// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Knowledge.Configuration.Chat;
    using Knowledge.Models.Chat;
    using Knowledge.Services.Chat;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Distributed;

    public class ChatHistoryService : AbstractService, IChatHistoryService
    {
        private readonly new ChatConfig config;

        private readonly IChatService provider;

        public ChatHistoryService(ChatConfig _config, IDistributedCache cache, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;
            this.config = _config;
        }        

        public Task<IEnumerable<ChatMessage>> GetChatHistory(string userId, string sessionId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ChatSession>> GetChatSessions(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateChatSession(string userId, string sessionId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveChatSession(string userId, string sessionId)
        {
            throw new NotImplementedException();
        }

        public Task<string> AddChatMessageToHistory(string userId, string sessionId, string role, string content, DateTime timespan)
        {
            // return created messageId
            return Task.FromResult(string.Empty);
        }
    }
}
