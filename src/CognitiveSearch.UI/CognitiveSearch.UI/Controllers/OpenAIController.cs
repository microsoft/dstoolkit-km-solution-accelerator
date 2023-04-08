// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CognitiveSearch.UI.Controllers {
    [Authorize]
    public class OpenAIController : AbstractViewController
    {
        public OpenAIController(UIConfig uiConfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;
            this.uiConfig = uiConfig;
            this.viewId = "chat";
        }

        public IActionResult Index()
        {
            return View(this.GetViewModel());
        }

        [HttpPost, HttpGet]
        public IActionResult Index(string q)
        {
            ViewBag.Message = uiConfig.GetVerticalById("chat").message;

            return View(this.GetViewModel(q));
        }
    }
}
