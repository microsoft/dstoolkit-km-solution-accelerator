// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration;
using Knowledge.Services;
using Knowledge.Services.Graph;
using Knowledge.Services.Graph.Facet;
using Knowledge.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knowledge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GraphController : CustomControllerBase
    {
        private IFacetGraphService _facetGraphService;

        public GraphController(IQueryService client,
                                IFacetGraphService facetGraphService,
                                SearchServiceConfig svcConfig)
        {
            this.QueryService = client;
            this.Config = svcConfig;
            this._facetGraphService = facetGraphService;
        }

        [HttpPost("getgraphdata")]
        public IActionResult GetGraphData(GraphRequest request)
        {
            // Support for common query transformations like spellcheck & translation
            QueryService.TransformQuery(request);

            List<string> facetNames = request.facets;

            if (facetNames == null || facetNames.Count == 0)
            {
                string facetsList = Config.GraphFacet;

                facetNames = facetsList.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
            }
            if (request.queryText == null || request.queryText == "undefined")
            {
                request.queryText = QueryHelper.QUERY_ALL;
            }

            request.facets = facetNames.ToList<string>();
            request.permissions = GetUserPermissions();

            var graphJson = _facetGraphService.GetGraphData(request);

            return CreateContentResultResponse(graphJson);
        }
    }
}
