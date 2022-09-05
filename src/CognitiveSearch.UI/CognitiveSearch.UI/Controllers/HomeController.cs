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
    public class HomeController : AbstractSearchViewController
    {
        public HomeController (UIConfig uiConfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;

            _uiConfig = uiConfig;
            _viewId = "search";
        }

        public IActionResult Index()
        {
            return View(this.GetLandingModel());
        }

        #region Search 
        public IActionResult Search()
        {
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
