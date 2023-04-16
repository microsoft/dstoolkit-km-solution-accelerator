// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Answers
{
    using Knowledge.Models.Answers;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IAnswersService
    {
        Task<IList<Answer>> GetAnswersAsync(string question, string docid, string doctext);

        Task<IList<Answer>> GetProjectAnswersAsync(string question);
    }
}
