// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using CognitiveSearch.UI.Models;
using Knowledge.Services.Models;
using Knowledge.Services.WebSearch;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace CognitiveSearch.UI.Controllers
{
    public class WebController : AbstractViewController
    {
        public IWebSearchService webSearchService;

        public WebSearchConfig webConfig; 

        public WebController(UIConfig uiConfig, IWebSearchService client, WebSearchConfig inWebConfig, TelemetryClient telemetry)
        {
            this.webSearchService = client;
            this.webConfig = inWebConfig;

            this.telemetryClient = telemetry;
            this._uiConfig = uiConfig;
            this._viewId = "web";
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

            if (!this.webConfig.IsEnabled)
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
