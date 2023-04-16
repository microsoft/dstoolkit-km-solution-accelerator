﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration.SpellChecking;
using System.Threading.Tasks;

namespace Knowledge.Services.SpellChecking.ACS
{
    public class ACSSpellCheckingService : AbstractService, ISpellCheckingService, ISpellCheckingProvider
    {
        public new SpellCheckingConfig config;

        public ACSSpellCheckingService(SpellCheckingConfig config)
        {
            this.config = config;
        }

        public Task<string> SpellCheckAsync(string text)
        {
            return Task.FromResult(text);
        }
    }
}
