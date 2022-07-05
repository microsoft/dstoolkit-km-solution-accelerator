// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using CognitiveSearch.UI.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace CognitiveSearch.UI.Controllers
{
    public class AbstractViewController : Controller
    {
        protected const string QUERY_ALL = "*";

        protected TelemetryClient telemetryClient;

        protected UIConfig _uiConfig { get; set; }

        protected string _configurationError { get; set; }

        protected string _viewId ;

        private static DefaultContractResolver contractResolver = new()
        {
            //NamingStrategy = new CamelCaseNamingStrategy()
            NamingStrategy = new CamelCaseNamingStrategy
            {
                OverrideSpecifiedNames = false
            }
        };

        public string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private JsonSerializerSettings settings = new()
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.Indented
        };

        protected string GetViewTitle()
        {
            return this._uiConfig.GetVerticalById(this._viewId).title;
        }

        protected virtual SearchViewModel GetViewModel(string query = null)
        {
            var searchidId = Guid.NewGuid().ToString();

            SearchViewModel vm = new()
            {
                searchId = searchidId,
                currentQuery = query ?? QUERY_ALL,
                config = _uiConfig.GetVerticalById(this._viewId)
            };
            return vm;
        }

        protected ContentResult CreateContentResultResponse(object result)
        {
            return Content(JsonConvert.SerializeObject(result, settings), "application/json");
        }

        protected SearchViewModel GetLandingModel(string query = null)
        {
            var searchidId = Guid.NewGuid().ToString();

            SearchViewModel vm = new()
            {
                searchId = searchidId,
                currentQuery = query ?? QUERY_ALL,
                config = this._uiConfig.LandingPage
            };
            return vm;
            //return this._uiConfig.LandingPage;
        }

    }

}
