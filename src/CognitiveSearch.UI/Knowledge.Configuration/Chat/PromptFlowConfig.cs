// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Configuration.Chat
{
    public class PromptFlowConfig : AbstractServiceConfig
    {
        public string ApiKey { get; set; }
        public string MLEndpoint { get; set; }
        public List<LLMDataSource> LLMDataSources { get; set; }
        public List<LLMModel> LLMModels { get; set; }
    }

    public class LLMDataSource
    {
        public string Name { get; set; }
        public string Descriptor { get; set; }
    }

    public class LLMModel
    {
        public string Name { get; set; }
        public string DeploymentName { get; set; }
        public string Descriptor { get; set; }
    }
}
