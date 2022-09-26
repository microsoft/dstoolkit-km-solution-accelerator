// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using CognitiveSearch.UI.Models;
using Knowledge.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace CognitiveSearch.UI.Controllers
{
    public class AnswersController : AbstractSearchViewController
    {
        public AnswersController(UIConfig uiConfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;

            base.uiConfig = uiConfig;
            viewId = "answers";
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

            //if (!this._searchConfig.semanticSearchEnabled)
            //{
            //    ViewBag.Message = "Semantic Search is disabled. Please contact your solution admin to enable it.";
            //    ViewBag.Style = "alert-danger";
            //}

            return View(vm);
        }
    }
}
