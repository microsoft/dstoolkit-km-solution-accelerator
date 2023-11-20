// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Models.Chat
{
    public class PromptFlowChatRequest : PromptFlowChatBaseRequest
    {
        public PFChatHistoryTurn[] chat_history { get; set; }
    }

    public class PromptFlowChatBaseRequest
    {
        public string question { get; set; }
        public string model { get; set; }
        public string source { get; set; }
    }

    public class PromptFlowChatResponse
    {
        public string answer { get; set; }
    }

    public class PFChatHistoryTurn
    {
        public PromptFlowChatBaseRequest inputs { get; set; }
        public PromptFlowChatResponse outputs { get; set; }
    }
}
