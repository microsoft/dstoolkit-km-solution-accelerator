// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Knowledge.Services.Chat.PromptFlow
{
    public interface IPromptFlowChatService : IChatService
    {
        Task<List<LLMModel>> GetAvailableLLMModels();
        Task<List<LLMDataSource>> GetAvailableLLMDataSources();
    }
}