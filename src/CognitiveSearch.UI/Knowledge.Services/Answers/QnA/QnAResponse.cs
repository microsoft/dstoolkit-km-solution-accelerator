// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Models.Answers;
using System.Collections.Generic;

namespace Knowledge.Services.QnA
{
    public class QnAResponse
    {
        public IList<Answer> answers { get; set; }
        public bool activeLearningEnabled { get; set; }
    }
}
