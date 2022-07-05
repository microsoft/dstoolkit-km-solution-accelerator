// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Knowledge.Services.Graph
{
    public class GraphConfig : AbstractServiceConfig
    {
        public int MAX_LEVELS = 3;
        public int MAX_NODES = 10;

        public string DefaultGraphEntity { get; set; }

        public string PrimaryFilterFieldForGraph { get; set; }

        public string FallbackFilterFieldForGraph { get; set; }

        public string IgnoreFieldsForEntityNode { get; set; }

        public bool FilterByLinksForGraph { get; set; }

        public string GraphFacet { get; set; }
        private List<string> GraphFacets { get; set; }

        public List<string> GetGraphFacets()
        {
            if (GraphFacets == null)
            {
                GraphFacets = new List<string>(GraphFacet.Split(','));
            }

            return GraphFacets;
        }

        public List<GraphType> GraphTypes { get; set; }

    }
}
