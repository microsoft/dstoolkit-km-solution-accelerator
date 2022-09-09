// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using CognitiveSearch.UI.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CognitiveSearch.UI.Controllers
{
    public class AbstractViewController : Controller
    {
        protected const string QUERY_ALL = "*";

        protected TelemetryClient telemetryClient;

        protected UIConfig uiConfig { get; set; }

        protected string configurationError { get; set; }

        protected string viewId ;

        public string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        protected string GetViewTitle()
        {
            return this.uiConfig.GetVerticalById(this.viewId).title;
        }

        protected virtual SearchViewModel GetViewModel(string query = null)
        {
            var searchidId = Guid.NewGuid().ToString();

            SearchViewModel vm = new()
            {
                searchId = searchidId,
                currentQuery = query ?? QUERY_ALL,
                config = uiConfig.GetVerticalById(this.viewId)
            };
            return vm;
        }
        protected IActionResult CreateContentResultResponse(object result)
        {
            return new JsonResult(result);
        }

        protected SearchViewModel GetLandingModel(string query = null)
        {
            var searchidId = Guid.NewGuid().ToString();

            SearchViewModel vm = new()
            {
                searchId = searchidId,
                currentQuery = query ?? QUERY_ALL,
                config = this.uiConfig.LandingPage
            };
            return vm;
        }
    }
}
