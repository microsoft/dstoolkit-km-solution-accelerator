﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Chat.FunctionChat
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Knowledge.Configuration.OpenAI;
    using Knowledge.Models.Chat;
    using Knowledge.Services.Chat;
    using Knowledge.Services.Helpers;

    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Distributed;

    using Newtonsoft.Json;

    public class FunctionChatService : AbstractService, IFunctionChatService
    {
        private readonly string[] StopSequence = new string[] { "|||||" };
        private OpenAIConfig _config { get; set; }

        public FunctionChatService(OpenAIConfig config, IDistributedCache cache, TelemetryClient telemetry)
        {
            distCache = cache;
            _config = config;
            CachePrefix = GetType().Name;
        }

        public async Task<string> Completion(ChatRequest request)
        {
            LoggerHelper.Instance.LogVerbose($"Start:Invoked Completion method in Open AI Chat service");

            var uri = _config.ChatServiceEndpoint;

            // JSON format for passing question to service

            string reqBody = "{\"values\":[{\"recordId\": \"1\",\"data\":{\"prompt\":" + JsonConvert.SerializeObject(request.prompt) + ",\"stop\":" + JsonConvert.SerializeObject(request.stop) + "}}]}";

            try
            {
                using (var httpRequest = new HttpRequestMessage())
                {
                    // POST method
                    httpRequest.Method = HttpMethod.Post;
                    // Add host + service to get full URI
                    httpRequest.RequestUri = new Uri(uri);
                    httpRequest.Content = new StringContent(reqBody, Encoding.UTF8, "application/json");

                    // Send request to Azure service, get response
                    var response = await httpClient.SendAsync(httpRequest);

                    return await response.Content.ReadAsStringAsync(); ;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.LogError(ex, ex.Message, "API", "OpenAIService.Completion");
            }

            LoggerHelper.Instance.LogVerbose($"End:Invoked Completion method in Open AI Chat service. Return empty result");

            return string.Empty;
        }

        public async Task<ChatResponse> ChatCompletion(ChatRequest request, string userId = "", string sessionId = "")
        {
            LoggerHelper.Instance.LogVerbose($"Start:Invoked ChatCompletion method in Open AI Chat service");

            var uri = _config.ChatServiceEndpoint;

            // JSON format for passing question to service

            string reqBody = "{\"values\":[{\"recordId\": \"1\",\"data\":{\"prompt\":" + JsonConvert.SerializeObject(request.prompt) + ",\"history\":" + JsonConvert.SerializeObject(request.history) + ",\"stop\":" + JsonConvert.SerializeObject(request.stop) + "}}]}";

            try
            {
                using (var httpRequest = new HttpRequestMessage())
                {
                    // POST method
                    httpRequest.Method = HttpMethod.Post;
                    // Add host + service to get full URI
                    httpRequest.RequestUri = new Uri(uri);
                    httpRequest.Content = new StringContent(reqBody, Encoding.UTF8, "application/json");

                    // Send request to Azure service, get response
                    var response = await httpClient.SendAsync(httpRequest);

                    return new ChatResponse
                    {
                        answer = await response.Content.ReadAsStringAsync()
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.LogError(ex, ex.Message, "API", "OpenAIService.ChatCompletion");
            }

            LoggerHelper.Instance.LogVerbose($"End:Invoked ChatCompletion method in Open AI Chat service. Return empty result");

            return new ChatResponse();
        }
    }
}