// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Metadata
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Knowledge.Configuration.Chat;
    using Knowledge.Services;
    using Knowledge.Services.Chat;

    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Distributed;

    public class ChatService : AbstractService, IChatService
    {
        private readonly new ChatConfig config;

        private readonly IChatProvider provider;

        public ChatService(ChatConfig _config, IDistributedCache cache, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;
            this.config = _config;

            // Find all Chat providers...
            var tests = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.GetInterfaces().Contains(typeof(IChatProvider)))
                    .Select((t, i) => Activator.CreateInstance(t, config, cache, telemetry) as IChatProvider);

            foreach (var item in tests)
            {
                if (item.IsEnabled())
                {
                    provider = (IChatProvider)item;
                }
            }

            // If we found a configured provider, set the config enablement.
            this.config.IsEnabled = (provider == null);
        }

        public Task<string> ChatCompletion(ChatRequest request)
        {
            return this.provider.ChatCompletion(request);
        }
        public Task<string> Completion(ChatRequest request)
        {
            return this.provider.Completion(request);
        }
    }
}
