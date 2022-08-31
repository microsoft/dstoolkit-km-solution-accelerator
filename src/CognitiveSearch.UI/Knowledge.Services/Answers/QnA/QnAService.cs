// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.QnA
{
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Knowledge.Services.Helpers;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    public class QnAService : AbstractService, IQnAService
    {
        private QnAConfig config;

        public QnAService(IDistributedCache cache, QnAConfig serviceConfig)
        {
            this.distCache = cache;
            this.config = serviceConfig;
            this.CachePrefix = this.GetType().Name;
        }

        public async Task<IList<Answer>> GetQnaAnswersAsync(string questionText, string queryId)
        {
            LoggerHelper.Instance.LogVerbose($"Start:Invoked GetAnswer method in QnaService");
            IList<Answer> qnaResultItem = new List<Answer>();

            if (this.TryGetValue(questionText,out string qnaResultItemStr))
            {
                qnaResultItem = JsonConvert.DeserializeObject<IList<Answer>>(qnaResultItemStr);
                LoggerHelper.Instance.LogVerbose($"End:Invoked GetAnswer method in QnaService. Return value from cache");
                return await Task.FromResult(qnaResultItem);
            }

            var uri = this.config.QNAServiceEndpoint;
            var databaseId = this.config.KnowledgeDatabaseId;
            if (string.IsNullOrWhiteSpace(this.config.QNAServiceEndpoint) || string.IsNullOrWhiteSpace(this.config.KnowledgeDatabaseId))
            {
                return new List<Answer>();
            }

            uri = uri.Replace("<kbId>", databaseId);

            // JSON format for passing question to service
            string question = @"{'question': '" + questionText + "','top': 5}";

            try
            {
                using (var request = new HttpRequestMessage())
                {
                    // POST method
                    request.Method = HttpMethod.Post;
                    // Add host + service to get full URI
                    request.RequestUri = new Uri(uri);
                    // set question
                    request.Content = new StringContent(question, Encoding.UTF8, "application/json");
                    // QnA Maker v1 Authorization
                    request.Headers.Add("Authorization", "EndpointKey " + this.config.QNAserviceKey);
                    // QnA Maker v2 Preview is now a Cognitive Service
                    request.Headers.Add("Ocp-Apim-Subscription-Key", this.config.QNAserviceKey);

                    // Send request to Azure service, get response
                    var response = await httpClient.SendAsync(request);
                    string strresponse = await response.Content.ReadAsStringAsync(); 

                    var jsonResponse = JsonConvert.DeserializeObject<QnAResponse>(strresponse);
                    if ( jsonResponse != null && jsonResponse.answers != null)
                    {
                        LoggerHelper.Instance.LogEvent("QnAService", "QNAService", queryId, this.config.KnowledgeDatabaseId, questionText, jsonResponse.answers.Count);

                        qnaResultItem = this.FormatQnAResponse(jsonResponse);
                        if (qnaResultItem != null)
                        {
                            LoggerHelper.Instance.LogVerbose($"Set QnA Result into cache.");

                            this.AddCacheEntry(questionText, JsonConvert.SerializeObject(qnaResultItem), config.CacheExpirationTime);
                        }

                        LoggerHelper.Instance.LogVerbose($"End:Invoked GetAnswer method in QnaService. Return value from http endpint");
                    }

                    return qnaResultItem;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.LogError(ex, ex.Message, "SCIO.API", "QnaService.GetAnswer");
            }

            LoggerHelper.Instance.LogVerbose($"End:Invoked GetAnswer method in QnaService. Return empty result");
            return new List<Answer>();
        }

        private IList<Answer> FormatQnAResponse(QnAResponse result)
        {
            var qnaResultItems = new List<Answer>();
            foreach (var answer in result.answers)
            {
                if (answer.id == -1 || answer.score < config.QNAScoreThreshold)
                {
                    continue;
                }

                qnaResultItems.Add(answer);
            }

            return qnaResultItems;
        }
    }
}
