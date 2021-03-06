// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.SpellChecking
{
    public class SpellCheckingConfig : AbstractServiceConfig
{
        public string SubscriptionKey { get; set; }

        public string Endpoint { get; set; }

        public string SupportedLanguages { get; set; }
        public string Market { get; set; }
        public string Mode { get; set; }
        public int CacheExpirationTime { get; set; }
    }
}
