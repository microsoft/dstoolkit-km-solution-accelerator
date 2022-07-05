// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Graph.Custom
{
    using System.Threading.Tasks;

    public interface IGraphCustomService
    {
        Task<GraphResponse> GenerateGraph(GraphRequest graphEntity);
    }
}
