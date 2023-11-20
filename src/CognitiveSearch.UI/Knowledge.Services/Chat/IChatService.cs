// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Models.Chat;
using System.Threading.Tasks;

namespace Knowledge.Services.Chat
{
    public interface IChatService 
    {
        public Task<ChatResponse> ChatCompletion(ChatRequest request, string userId = "", string sessionId = "");

        public Task<string> Completion(ChatRequest request);
    }
}