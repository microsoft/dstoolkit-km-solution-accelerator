// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration;

namespace Knowledge.Configuration.SpellChecking
{
    public class SpellCheckingConfig : AbstractServiceConfig
    {
        public string Provider { get; set; }

        public string SubscriptionKey { get; set; }

        public string Endpoint { get; set; }
        public string SupportedLanguages { get; set; }
        public string Market { get; set; }
        public string Mode { get; set; }

        public int CacheExpirationTime { get; set; }
    }
}
