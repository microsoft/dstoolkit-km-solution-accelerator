// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using CognitiveSearch.UI.Models;
using Knowledge.Configuration.Maps;
using Knowledge.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace CognitiveSearch.UI.Controllers
{
    public class MapController : AbstractSearchViewController
    {
        private readonly MapConfig _mapConfig;

        public MapController(UIConfig uiConfig, MapConfig svcconfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;

            base.uiConfig = uiConfig;
            viewId = "map";

            _mapConfig = svcconfig;
        }

        public IActionResult Index()
        {
            if (!this._mapConfig.IsEnabled)
            {
                ViewBag.Message = "Azure Maps is disabled. Please contact your solution admin to enable it.";
                ViewBag.Style = "alert-danger";
            }

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

            if (!this._mapConfig.IsEnabled)
            {
                ViewBag.Message = "Azure Maps is disabled. Please contact your solution admin to enable it.";
                ViewBag.Style = "alert-danger";
            }

            return View(vm);
        }

    }
}
