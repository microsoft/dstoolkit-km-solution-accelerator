// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration.Answers.Language;
using Knowledge.Configuration.Answers.QnA;

namespace Knowledge.Configuration.Answers
{
    public class AnswersConfig : AbstractServiceConfig
    {
        public QnAConfig? qnaConfig { get; set; }

        public LanguageConfig? languageConfig { get; set; }
    }
}
