// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Models
{
    public class UserOptions
    {
        public bool isSemanticSearch { get; set; }
        public bool isQueryTranslation { get; set; }
        public bool isQuerySpellCheck { get; set; }
        public bool suggestionsAsFilter { get; set; }

        public UserOptions()
        {
        }
    }
}
