// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Graph.Facet
{
    public interface IFacetGraphService
    {
        public JsonGraphResponse GetGraphData(GraphRequest request);
    }
}