﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Configuration.Translation
{
    public class TranslationConfig : AbstractServiceConfig
    {
        public string SubscriptionKey { get; set; }

        public string Endpoint { get; set; }

        public string ServiceRegion { get; set; }

        public string SupportedLanguages { get; set; }

        public string SuggestedFrom { get; set; }
        public string SuggestedTo { get; set; }

        public int CacheExpirationTime { get; set; }
    }
}