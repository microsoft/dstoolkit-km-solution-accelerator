// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;

namespace CognitiveSearch.UI.Controllers
{
    public class NewsController : AbstractViewController
    {
        public NewsController(UIConfig uiConfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;
            this.uiConfig = uiConfig;
            this.viewId = "news";
        }

        public IActionResult Index()
        {
            return View(this.GetViewModel());
        }

        [HttpPost, HttpGet]
        public IActionResult Index(string q)
        {
            ViewBag.Message = uiConfig.GetVerticalById("news").message;

            return View(this.GetViewModel(q));
        }

    }
}
