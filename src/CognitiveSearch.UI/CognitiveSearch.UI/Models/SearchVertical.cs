// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace CognitiveSearch.UI.Models
{
    public class SearchVertical : AbstractPage
    {
        public string placeHolder { get; set; }
        public string filter { get; set; }
        public bool infiniteScroll { get; set; }
        public bool isSemanticCapable { get; set;  }
        public string message { get; set; }
        public bool enableExcelExport { get; set; }
        public bool enableDateRange { get; set; }
        public bool enableDynamicFacets { get; set; }
        public bool enableOffcanvasNavigation { get; set; }

        public List<ClientAction> ResultsRenderings { get; set; }

        public List<Suggestion> suggestions { get; set; }

        public string Tags { get; set; }

        public SearchVertical()
        {
            // Defaults vertical options
            this.infiniteScroll = true;
            this.isSemanticCapable = true;

            this.enableOffcanvasNavigation = true;
            this.enableDateRange = true;
            this.enableDynamicFacets = true;
            this.enableExcelExport = true;

            // Initialize Lists
            ResultsRenderings = new List<ClientAction>();

            // Vertical specific suggestions
            suggestions = new List<Suggestion>();
        }
    }
}
