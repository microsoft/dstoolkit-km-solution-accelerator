using Knowledge.Configuration.SpellChecking;
using Knowledge.Services.SpellChecking.ACS;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Knowledge.Services.SpellChecking
{
    public class SpellCheckingService : AbstractService, ISpellCheckingService
    {
        public SpellCheckingConfig config;

        public ISpellCheckingService provider = null;

        public SpellCheckingService(SpellCheckingConfig config, IDistributedCache cache, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;
            this.distCache = cache;
            this.config = config;

            this.CachePrefix = this.GetType().Name;

            // Find all SpellChecking providers...
            var tests = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.GetInterfaces().Contains(typeof(ISpellCheckingProvider)))
                    .Select((t, i) => Activator.CreateInstance(t, config) as ISpellCheckingProvider);

            foreach (var item in tests)
            {
                if ( config.Provider.Equals(item.GetProvider()))
                {
                    provider = (ISpellCheckingService)item;
                }
            }

            // If the configured spellchecking service is not found, revert config to disabled.
            if (provider == null)
            {
                config.IsEnabled = false;
            }

        }

        public async Task<string> SpellCheckAsync(string text)
        {
            string result = this.distCache.GetString(CachePrefix + text);

            if (!String.IsNullOrEmpty(result))
            {
                return result;
            }
            else
            {
                result = await provider.SpellCheckAsync(text);

                if (String.IsNullOrEmpty(result))
                {
                    // Add Telemetry event SpellChecking event

                    return text;
                }
                else
                {
                    this.distCache.SetString(CachePrefix + text, result, new DistributedCacheEntryOptions()
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(config.CacheExpirationTime)
                    });

                    return result;
                }
            }
        }
    }
}
