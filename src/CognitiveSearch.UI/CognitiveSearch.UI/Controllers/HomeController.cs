// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Knowledge.Services;
using Knowledge.Services.Configuration;
using Knowledge.Services.Models;
using Newtonsoft.Json;
using System;
using CognitiveSearch.UI.Configuration;
using CognitiveSearch.UI.Models;
using Microsoft.ApplicationInsights;

namespace CognitiveSearch.UI.Controllers
{
    public class HomeController : AbstractSearchViewController
    {
        public HomeController (UIConfig uiConfig, IQueryService client, SearchServiceConfig svcconfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;

            _queryService = client;
            _searchConfig = svcconfig;
            _uiConfig = uiConfig;
            _viewId = "search";
        }

        public IActionResult Index()
        {
            CheckDocSearchInitialized();

            return View(this.GetLandingModel());
        }

        #region Search 
        public IActionResult Search()
        {
            CheckDocSearchInitialized();

            return View(this.GetViewModel());
        }

        [HttpPost]
        public IActionResult Search([FromForm] string q = "*", [FromForm] string facets = null)
        {
            SearchViewModel vm = this.GetViewModel(q);

            if (!String.IsNullOrEmpty(facets))
            {
                try
                {
                    vm.selectedFacets = JsonConvert.DeserializeObject<SearchFacet[]>(this.Base64Decode(facets));
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Exception from backend "+ex.Message;
                    ViewBag.Style = "alert-danger";
                }
            }

            return View("Search",vm);
        }

        #endregion
    }
}
