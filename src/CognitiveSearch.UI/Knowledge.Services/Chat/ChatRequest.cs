// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Chat
{
    public class ChatRequest
    {
        public string prompt { get; set; }

        public ChatMessage[] history { get; set; }

        public string[] stop { get; set; }
    }

    public class ChatMessage 
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}
