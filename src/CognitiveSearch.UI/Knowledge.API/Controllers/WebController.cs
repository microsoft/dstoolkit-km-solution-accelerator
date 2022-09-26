// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.API.Models;
using Knowledge.Configuration;
using Knowledge.Services.WebSearch;
using Microsoft.AspNetCore.Mvc;

namespace Knowledge.API.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class WebController : AbstractApiController
    {
        public IWebSearchService webSearchService;

        public SearchServiceConfig searchConfig;

        public WebController(IWebSearchService client, SearchServiceConfig allconfig)
        {
            webSearchService = client;
            searchConfig = allconfig;
        }

        private string? GetIp()
        {
            return Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        [HttpPost("webresults")]
        public async System.Threading.Tasks.Task<IActionResult> GetWebResultsAsync(ApiWebRequest request)
        {
            request.clientip = GetIp();

            var response = await webSearchService.GetWebResults(request);

            return new JsonResult(response);
        }

        [HttpPost("newsresults")]
        public async System.Threading.Tasks.Task<IActionResult> GetNewsResultsAsync(ApiWebRequest request)
        {
            request.clientip = GetIp();

            var response = await webSearchService.GetNewsResults(request);

            return new JsonResult(response);
        }

        [HttpPost("imagesresults")]
        public async System.Threading.Tasks.Task<IActionResult> GetImagesResultsAsync(ApiWebRequest request)
        {
            request.clientip = GetIp();

            var response = await webSearchService.GetImagesResults(request);

            return new JsonResult(response);
        }

        [HttpPost("videosresults")]
        public async System.Threading.Tasks.Task<IActionResult> GetVideosResultsAsync(ApiWebRequest request)
        {
            request.clientip = GetIp();

            var response = await webSearchService.GetVideosResults(request);

            return new JsonResult(response);
        }
    }
}
