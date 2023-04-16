using CognitiveSearch.UI.Configuration;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Azure.Core;

namespace CognitiveSearch.UI.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConfigController : Controller
    {
        private readonly AppConfig config;

        private static readonly DefaultContractResolver contractResolver = new()
        {
            NamingStrategy = new CamelCaseNamingStrategy
            {
                OverrideSpecifiedNames = false
            }
        };

        private readonly JsonSerializerSettings settings = new()
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.Indented
        };

        public ConfigController(AppConfig config)
        {
            this.config = config;
        }

        [HttpGet("get")]
        public async Task<IActionResult> GetAppConfigAsync()
        {
            return await Task.Run(() => Content(JsonConvert.SerializeObject(config, settings), "application/json"));
        }
    }
}
