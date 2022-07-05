// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Knowledge.Services;
using Knowledge.Services.Configuration;
using CognitiveSearch.UI.Configuration;
using Microsoft.ApplicationInsights;
using CognitiveSearch.UI.Models;
using System;
using Newtonsoft.Json;
using Knowledge.Services.Models;

namespace CognitiveSearch.UI.Controllers
{
    public class ImagesController : AbstractSearchViewController
    {
        public ImagesController(UIConfig uiConfig, IQueryService client, SearchServiceConfig svcconfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;

            _queryService = client;
            _searchConfig = svcconfig;
            _uiConfig = uiConfig;
            _viewId = "images";
        }

        public IActionResult Index()
        {
            return View(this.GetViewModel());
        }

        [HttpPost]
        public IActionResult Index([FromForm] string q = "*", [FromForm] string facets = null)
        {
            SearchViewModel vm = this.GetViewModel(q);

            if (!String.IsNullOrEmpty(facets))
            {
                vm.selectedFacets = JsonConvert.DeserializeObject<SearchFacet[]>(this.Base64Decode(facets));
            }

            return View(vm);
        }

    }
}
