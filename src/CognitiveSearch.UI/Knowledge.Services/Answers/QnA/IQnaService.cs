// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.QnA
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IQnAService
    {
        Task<IList<Answer>> GetQnaAnswersAsync(string question, string queryId);
    }
}
