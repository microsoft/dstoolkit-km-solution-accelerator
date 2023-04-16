// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

using Knowledge.Models.Answers;

namespace Knowledge.Services.Answers
{
    public class QnAResponse
    {
        public IList<Answer> answers { get; set; }
        public bool activeLearningEnabled { get; set; }
    }
}
