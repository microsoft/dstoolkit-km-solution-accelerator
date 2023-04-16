// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Answers
{
    public interface IAnswersProvider : IAnswersService
    {
        public string GetProviderName();
    }
}
