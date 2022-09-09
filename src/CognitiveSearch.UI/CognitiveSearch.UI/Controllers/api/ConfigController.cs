using CognitiveSearch.UI.Configuration;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;

namespace CognitiveSearch.UI.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : Controller
    {
        private AppConfig config;

        private static DefaultContractResolver contractResolver = new DefaultContractResolver
        {
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

        public ConfigController(AppConfig config)
        {
            this.config = config;
        }

        [HttpGet("get")]
        public async Task<IActionResult> GetAppConfigAsync()
        {
            return Content(JsonConvert.SerializeObject(config, settings), "application/json");
        }
    }
}
