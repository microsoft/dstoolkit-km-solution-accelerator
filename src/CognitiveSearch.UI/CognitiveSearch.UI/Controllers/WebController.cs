// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using CognitiveSearch.UI.Models;
using Knowledge.Configuration.WebSearch;
using Knowledge.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using Microsoft.AspNetCore.Authorization;

namespace CognitiveSearch.UI.Controllers
{
    [Authorize]
    public class WebController : AbstractViewController
    {
        public WebSearchConfig webSearchConfig;
        public WebController(WebSearchConfig webSearchConfig, UIConfig uiConfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;
            this.uiConfig = uiConfig;
            this.viewId = "web";

            this.webSearchConfig = webSearchConfig;
        }

        public IActionResult Index()
        {
            return View(this.GetViewModel());
        }

        [HttpPost]
        public IActionResult Index([FromForm] string q, [FromForm] string facets = null)
        {
            SearchViewModel vm = this.GetViewModel(q); 

            if (!String.IsNullOrEmpty(facets))
            {
                vm.selectedFacets = JsonConvert.DeserializeObject<SearchFacet[]>(this.Base64Decode(facets));
            }

            if (!this.webSearchConfig.IsEnabled)
            {
                ViewBag.Message = "Web Search is disabled. Please contact your solution admin to enable it.";
                ViewBag.Style = "alert-danger";
            }
            else
            {
                if (!String.IsNullOrEmpty(q))
                {
                    if (q.Equals(QUERY_ALL) && String.IsNullOrEmpty(facets))
                    {
                        ViewBag.Message = $"Searching the Web with {QUERY_ALL} is too broad. Please refine your search query.";
                        ViewBag.Style = "alert-warning border-warning";
                    }
                }
            }

            return View(vm);
        }

    }
}
