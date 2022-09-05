// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Services;
using Knowledge.Configuration;
using Knowledge.Services.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Knowledge.API.Controllers
{
    public class AbstractApiController : ControllerBase
    {
        // Client logs all searches in Application Insights
        protected TelemetryClient telemetryClient;

        protected SearchServiceConfig _config { get; set; }

        protected IQueryService _queryService { get; set; }

        protected string _configurationError { get; set; }

        private static DefaultContractResolver contractResolver = new DefaultContractResolver
        {
            //NamingStrategy = new CamelCaseNamingStrategy()
            NamingStrategy = new CamelCaseNamingStrategy
            {
                OverrideSpecifiedNames = false
            }
        };

        private JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.Indented
        };

        protected ContentResult CreateContentResultResponse(object result)
        {
            return Content(JsonConvert.SerializeObject(result, settings), "application/json");
        }

        protected SearchPermission[] GetUserPermissions()
        {
            List<SearchPermission> permissions = new();

            permissions.Add(new SearchPermission() { group = GetUserId() });

            if (User.Claims.Any(c => c.Type == "groups"))
            {
                foreach (var item in User.Claims.Where(c => c.Type == "groups"))
                {
                    permissions.Add(new SearchPermission() { group = item.Value });
                }
            }

            return permissions.ToArray();
        }

        protected string GetUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                return User.FindFirst(ClaimTypes.Name)?.Value;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
