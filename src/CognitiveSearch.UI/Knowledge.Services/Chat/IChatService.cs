// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Knowledge.Services.Chat
{
    public interface IChatService 
    {
        public Task<string> ChatCompletion(ChatRequest request);

        public Task<string> Completion(ChatRequest request);
    }
}