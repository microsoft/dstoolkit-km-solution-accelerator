// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Models.Chat
{
    public class ChatRequest
    {
        public string prompt { get; set; }

        public ChatMessage[] history { get; set; }

        public ChatOptions options { get; set; }

        public string[] stop { get; set; } // ?
    }        
}
