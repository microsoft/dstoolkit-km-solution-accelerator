// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Knowledge.Configuration.Answers;
using Knowledge.Models.Answers;

using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Distributed;

namespace Knowledge.Services.Answers
{
    public class AnswersService : AbstractService, IAnswersService
    {
        private new AnswersConfig config; 

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

                // If the configured answers service is not found, revert config to disabled.
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
