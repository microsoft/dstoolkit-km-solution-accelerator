// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.API.Models;
using Knowledge.Services.Configuration;
using Knowledge.Services.Maps;
using Microsoft.AspNetCore.Mvc;

namespace Knowledge.API.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class MapController : AbstractApiController
    {
        public MapConfig mapService;

        public MapController(MapConfig client)
        {
            mapService = client;
        }

        public class MapCredentials
        {
            public string MapKey { get; set; }
        }

        [HttpPost("getmapcredentials")]
        public IActionResult GetMapCredentials()
        {
            string mapKey = mapService.AzureMapsSubscriptionKey;

            return new JsonResult(
                new MapCredentials
                {
                    MapKey = mapKey
                });
        }
    }
}
