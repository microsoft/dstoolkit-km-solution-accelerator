// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using CognitiveSearch.UI.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace CognitiveSearch.UI.Controllers
{
    public class GraphController : AbstractSearchViewController
    {
        public GraphController (UIConfig uiConfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;

            _uiConfig = uiConfig;
            _viewId = "graph";
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
