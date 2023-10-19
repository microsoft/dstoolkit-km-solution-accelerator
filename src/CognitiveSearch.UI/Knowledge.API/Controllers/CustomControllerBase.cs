// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration;
using Knowledge.Models;
using Knowledge.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Knowledge.API.Controllers
{
    [Authorize]
    public class CustomControllerBase : ControllerBase
    {
        // Client logs all searches in Application Insights
        protected TelemetryClient telemetryClient;

        protected SearchServiceConfig Config { get; set; }

        protected IQueryService QueryService { get; set; }
        
        protected IActionResult CreateContentResultResponse(object result)
        {
            return new JsonResult(result);
        }

        protected SearchPermission[] GetUserPermissions()
        {
            List<SearchPermission> permissions = new()
            {
                new SearchPermission() { group = GetUserId() }
            };

            if (User.Claims.Any(c => c.Type == "groups"))
            {
                foreach (var item in User.Claims.Where(c => c.Type == "groups"))
                {
                    permissions.Add(new SearchPermission() { group = item.Value });
                }
            }

            return permissions.ToArray();
        }

        protected string? GetUserId()
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
