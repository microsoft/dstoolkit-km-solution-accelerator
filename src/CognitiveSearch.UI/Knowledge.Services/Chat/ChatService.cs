// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Metadata
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Knowledge.Configuration.Chat;
    using Knowledge.Models.Chat;
    using Knowledge.Services;
    using Knowledge.Services.Chat;

    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Distributed;

    public class ChatService : AbstractService, IChatService
    {
        private readonly new ChatConfig config;

        private readonly IChatService provider;

        public ChatService(ChatConfig _config, IDistributedCache cache, TelemetryClient telemetry)
        {
            this.telemetryClient = telemetry;
            this.config = _config;

            // Find all Chat providers...
            var tests = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.GetInterfaces().Contains(typeof(IChatService)))
                    .Select((t, i) => Activator.CreateInstance(t, config, cache, telemetry) as IChatService);

            foreach (var item in tests)
            {
                provider = item;
            }

            // If we found a configured provider, set the config enablement.
            this.config.IsEnabled = (provider == null);
        }

        public Task<ChatResponse> ChatCompletion(ChatRequest request, string userId = "", string sessionId = "")
        {
            return this.provider.ChatCompletion(request);
        }
        public Task<string> Completion(ChatRequest request)
        {
            return this.provider.Completion(request);
        }
    }
}
