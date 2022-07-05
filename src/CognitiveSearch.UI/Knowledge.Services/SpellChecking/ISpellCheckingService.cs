// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Knowledge.Services.SpellChecking
{
    public interface ISpellCheckingService
    {
        public Task<string> SpellCheckAsync(string text);

    }
}
