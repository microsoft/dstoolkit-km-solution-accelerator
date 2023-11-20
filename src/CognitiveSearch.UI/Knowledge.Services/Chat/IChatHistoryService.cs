// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Models.Chat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Knowledge.Services.Chat
{
    public interface IChatHistoryService 
    {
        public Task<IEnumerable<ChatSession>> GetChatSessions(string userId);

        public Task<IEnumerable<ChatMessage>> GetChatHistory(string userId, string sessionId);

        public Task<bool> RemoveChatSession(string userId, string sessionId);
        public Task<string> CreateChatSession(string userId, string sessionId);
        public Task<string> AddChatMessageToHistory(string userId, string sessionId, string role, string content, DateTime timespan);
    }
}