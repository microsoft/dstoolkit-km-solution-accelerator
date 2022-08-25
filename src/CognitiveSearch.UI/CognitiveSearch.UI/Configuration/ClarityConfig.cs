// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CognitiveSearch.UI.Configuration
{
    public class ClarityConfig
    {
        public string ProjectId { get; set; }

        public bool isEnabled()
        {
            return !string.IsNullOrEmpty(ProjectId);
        }
    }
}