// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Configuration.OpenAI
{
    public class OpenAIConfig : AbstractServiceConfig
    {
        public string ServiceEndpoint { get; set; }

        public string DeploymentName { get; set; }

        public string Version { get; set; }

        public string ChatServiceEndpoint { get; set; }

        public int CacheExpirationTime { get; set; }

    }
}
