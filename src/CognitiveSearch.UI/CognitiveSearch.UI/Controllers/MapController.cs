// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Knowledge.Services;
using Knowledge.Services.Configuration;
using Knowledge.Services.Maps;
using CognitiveSearch.UI.Configuration;
using Microsoft.ApplicationInsights;
using CognitiveSearch.UI.Models;
using System;
using Newtonsoft.Json;
using Knowledge.Services.Models;

namespace CognitiveSearch.UI.Controllers
{
    public class MapController : AbstractSearchViewController
    {
        private readonly MapConfig _mapConfig;

        public MapController(UIConfig uiConfig, IQueryService client, SearchServiceConfig searchConfig, MapConfig svcconfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;

            _queryService = client;
            _searchConfig = searchConfig;
            _uiConfig = uiConfig;
            _viewId = "map";

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
