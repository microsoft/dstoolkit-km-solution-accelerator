// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Configuration.Answers.Language
{
    public class LanguageConfig : AbstractServiceConfig
    {
        public string ServiceEndpoint { get; set; }

        public string ServiceKey { get; set; }

        public string ProjectName { get; set; }

        public string DeploymentName { get; set; }

        public int CacheExpirationTime { get; set; }

        public int ConfidenceThreshold { get; set; }

    }
}
