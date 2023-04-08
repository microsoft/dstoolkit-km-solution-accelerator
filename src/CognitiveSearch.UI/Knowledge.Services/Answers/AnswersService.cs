using Knowledge.Configuration.Answers;
using Knowledge.Models.Answers;
using Knowledge.Services.Answers;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Knowledge.Services.SpellChecking
{
    public class AnswersService : AbstractService, IAnswersService
    {
        private AnswersConfig config; 

        public List<IAnswersProvider> provider = new();

        public AnswersService(AnswersConfig config, IDistributedCache cache, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;
            this.distCache = cache;
            this.config = config;
            this.CachePrefix = this.GetType().Name;

            if (config.IsEnabled)
            {
                // Find all Answers providers...
                var tests = Assembly.GetExecutingAssembly().GetTypes()
                        .Where(t => t.GetInterfaces().Contains(typeof(IAnswersProvider)))
                        .Select((t, i) => Activator.CreateInstance(t, config, cache, telemetry) as IAnswersProvider);

                foreach (var item in tests)
                {
                    provider.Add((IAnswersProvider)item);
                }

                // If the configured spellchecking service is not found, revert config to disabled.
                if (provider.Count == 0)
                {
                    config.IsEnabled = false;
                }
            }
        }

        public Task<IList<Answer>> GetAnswersAsync(string question, string docid, string doctext)
        {
            //TODO
            throw new NotImplementedException();
        }

        public Task<IList<Answer>> GetProjectAnswersAsync(string question)
        {
            //TODO
            throw new NotImplementedException();
        }
    }
}
