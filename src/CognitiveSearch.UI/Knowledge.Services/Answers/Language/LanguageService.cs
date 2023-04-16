// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Answers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Azure;
    using Azure.AI.Language.QuestionAnswering;

    using Knowledge.Configuration.Answers;
    using Knowledge.Configuration.Answers.Language;
    using Knowledge.Models.Answers;
    using Knowledge.Services.Helpers;

    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Distributed;

    using Newtonsoft.Json;

    public class LanguageService : AbstractService, IAnswersProvider
    {
        private LanguageConfig config;
        private QuestionAnsweringClient client; 

        public LanguageService(AnswersConfig config, IDistributedCache cache, TelemetryClient telemetry)
        {
            this.distCache = cache;
            this.config = config.languageConfig;
            this.CachePrefix = this.GetType().Name;

            Uri endpoint = new Uri(this.config.ServiceEndpoint);
            AzureKeyCredential credential = new(this.config.ServiceKey);

            client = new QuestionAnsweringClient(endpoint, credential);
        }

        public string GetProviderName()
        {
            throw new NotImplementedException();
        }

        public async Task<IList<Answer>> GetAnswersAsync(string question, string docid, string doctext)
        {
            LoggerHelper.Instance.LogVerbose($"Start:Invoked GetAnswer method");

            string cacheKey = docid + question;

            IList<Answer> qnaResultItem = new List<Answer>();

            if (this.TryGetValue(cacheKey, out string qnaResultItemStr))
            {
                qnaResultItem = JsonConvert.DeserializeObject<IList<Answer>>(qnaResultItemStr);
                LoggerHelper.Instance.LogVerbose($"End:Invoked GetAnswer method. Return value from cache");
                return await Task.FromResult(qnaResultItem);
            }

            if (string.IsNullOrWhiteSpace(this.config.ServiceEndpoint) || string.IsNullOrWhiteSpace(this.config.ServiceKey))
            {
                return new List<Answer>();
            }

            // REST API

            try
            {
                using (var request = new HttpRequestMessage())
                {
                    // POST method
                    request.Method = HttpMethod.Post;
                    // Add host + service to get full URI
                    request.RequestUri = new Uri(this.config.ServiceEndpoint);
                    request.Headers.Add("Ocp-Apim-Subscription-Key", this.config.ServiceKey);

                    request.Content = new StringContent(question, Encoding.UTF8, "application/json");

                    // Send request to Azure service, get response
                    var response = await httpClient.SendAsync(request);
                    string strresponse = await response.Content.ReadAsStringAsync();

                    var jsonResponse = JsonConvert.DeserializeObject<QnAResponse>(strresponse);
                    if (jsonResponse != null && jsonResponse.answers != null)
                    {
                        LoggerHelper.Instance.LogEvent("QnAService", "QNAService", docid, this.config.ProjectName, question, jsonResponse.answers.Count);

                        qnaResultItem = this.FormatQnAResponse(jsonResponse);

                        if (qnaResultItem != null)
                        {
                            LoggerHelper.Instance.LogVerbose($"Set QnA Result into cache.");

                            this.AddCacheEntry(question, JsonConvert.SerializeObject(qnaResultItem), config.CacheExpirationTime);
                        }

                        LoggerHelper.Instance.LogVerbose($"End:Invoked GetAnswer method in QnaService. Return value from http endpint");
                    }

                    return qnaResultItem;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.LogError(ex, ex.Message, "API", "QnaService.GetAnswer");
            }

            LoggerHelper.Instance.LogVerbose($"End:Invoked GetAnswer method in QnaService. Return empty result");
            return new List<Answer>();
        }

        public bool IsDefault()
        {
            throw new NotImplementedException();
        }

        private IList<Answer> FormatQnAResponse(QnAResponse result)
        {
            var qnaResultItems = new List<Answer>();
            foreach (var answer in result.answers)
            {
                if (answer.id == -1 || answer.score < config.ConfidenceThreshold)
                {
                    continue;
                }

                qnaResultItems.Add(answer);
            }

            return qnaResultItems;
        }


        public async Task<IList<Answer>> GetProjectAnswersAsync(string question)
        {
            LoggerHelper.Instance.LogVerbose($"Start:Invoked GetAnswer method");

            IList<Answer> qnaResultItem = new List<Answer>();

            if (this.TryGetValue(question, out string qnaResultItemStr))
            {
                qnaResultItem = JsonConvert.DeserializeObject<IList<Answer>>(qnaResultItemStr);
                LoggerHelper.Instance.LogVerbose($"End:Invoked GetAnswer method. Return value from cache");
                return await Task.FromResult(qnaResultItem);
            }

            if (string.IsNullOrWhiteSpace(this.config.ServiceEndpoint) || string.IsNullOrWhiteSpace(this.config.ServiceKey))
            {
                return new List<Answer>();
            }

            // SDK - Project QnA
            try
            {

                string projectName = this.config.ProjectName;
                string deploymentName = this.config.DeploymentName;

                QuestionAnsweringProject project = new QuestionAnsweringProject(projectName, deploymentName);

                Response<AnswersResult> response = client.GetAnswers(question, project);

                foreach (KnowledgeBaseAnswer answer in response.Value.Answers)
                {
                    Console.WriteLine($"({answer.Confidence:P2}) {answer.Answer}");
                    Console.WriteLine($"Source: {answer.Source}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.LogError(ex, ex.Message, "API", "QnaService.GetAnswer");
            }

            LoggerHelper.Instance.LogVerbose($"End:Invoked GetAnswer method in QnaService. Return empty result");

            return new List<Answer>();
        }
    }
}
