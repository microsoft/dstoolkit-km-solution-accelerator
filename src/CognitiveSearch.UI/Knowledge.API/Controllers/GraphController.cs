// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.API.Models;
using Knowledge.Services;
using Knowledge.Configuration;
using Knowledge.Services.Graph.Facet;
using Knowledge.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Knowledge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class GraphController : AbstractApiController
    {
        private IFacetGraphService _facetGraphService;

        public GraphController(IQueryService client,
                                IFacetGraphService facetGraphService,
                                SearchServiceConfig svcConfig)
        {
            this._queryService = client;
            this._config = svcConfig;
            this._facetGraphService = facetGraphService;
        }

        [HttpPost("getgraphdata")]
        public ActionResult GetGraphData(ApiGraphRequest request)
        {
            // Support for common query transformations like spellcheck & translation
            _queryService.TransformQuery(request);

            List<string> facetNames = request.facets;

            if (facetNames == null || facetNames.Count == 0)
            {
                string facetsList = _config.GraphFacet;

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
