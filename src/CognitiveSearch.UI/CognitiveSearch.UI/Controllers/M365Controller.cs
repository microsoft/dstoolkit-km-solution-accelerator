// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CognitiveSearch.UI.Controllers
{
    [Authorize]
    public class M365Controller : AbstractViewController
    {
        public M365Controller(UIConfig uiConfig, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;
            this.uiConfig = uiConfig;
            this.viewId = "m365";
        }

        public IActionResult Index()
        {
            return View(this.GetViewModel());
        }
    }
}
