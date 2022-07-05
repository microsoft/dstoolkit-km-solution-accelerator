// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Commons;
using Image.Commons.Extraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Services.Common;
using Microsoft.Services.Common.WebApiSkills;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Image.Extraction
{
    public static class DurableImageExtractionSkill
    {
        private static readonly HttpClient webclient = new();

        private readonly static bool RemoteTransformation = FEnvironment.BooleanReader("RemoteTransformationEnabled", false);
        private readonly static string  RemoteTransformationEndpoint = FEnvironment.StringReader("RemoteTransformationEndpoint");

        [FunctionName("DurableImageExtractionSkill_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            WebApiEnricherResponse response = new WebApiEnricherResponse
            {
                Values = new List<WebApiResponseRecord>()
            };

            WebApiSkillRequest data = JsonConvert.DeserializeObject<WebApiSkillRequest>(requestBody);

            foreach (var record in data?.Values)
            {
                if (record.Data.ContainsKey("document_url"))
                {
                    DurableInputRecord inObj = new DurableInputRecord()
                    {
                        headers = HeadersHelper.ConvertFunctionHeaders(req.Headers),
                        record = record
                    };

                    // Function input comes from the request content.
                    string instanceId = await starter.StartNewAsync("DurableImageExtractionSkill", null, inObj);

                    log.LogInformation($"Started orchestration with ID '{instanceId}' for file {(string)record.Data["document_url"]}");

                    WebApiResponseRecord responseRecord = new WebApiResponseRecord
                    {
                        Data = new Dictionary<string, object>
                        {
                            ["message"] = "Durable function instance id is " + instanceId
                        },
                        RecordId = record.RecordId
                    };
                    response.Values.Add(responseRecord);
                }
                else
                {
                    WebApiResponseRecord responseRecord = new()
                    {
                        Data = new Dictionary<string, object>
                        {
                            ["message"] = "Empty record. Skipping"
                        },
                        RecordId = record.RecordId
                    };
                    response.Values.Add(responseRecord);
                }
            }

            HttpResponseMessage skillResponse = new(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(response))
            };
            skillResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return skillResponse;
        }

        [FunctionName("DurableImageExtractionSkill")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            DurableInputRecord record = context.GetInput<DurableInputRecord>();

            await context.CallActivityAsync("DurableImageExtractionSkill_Processing", record);
        }

        [FunctionName("DurableImageExtractionSkill_Processing")]
        public static async Task ProcessingAsync([ActivityTrigger] DurableInputRecord inObj, ILogger log)
        {
            try
            {
                IDocumentEntity docitem = new IDocumentEntity
                {
                    IndexKey = (string)inObj.record.Data["document_index_key"],
                    Id = (string)inObj.record.Data["document_id"],
                    Name = (string)inObj.record.Data["document_filename"],
                    WebUrl = (string)inObj.record.Data["document_url"]
                };

                log.LogInformation($"Source {docitem.Id} {docitem.Name} {docitem.WebUrl}");

                string res = String.Empty; 

                // Call the sync image extraction backend function
                if (RemoteTransformation)
                {
                    if (String.IsNullOrEmpty(RemoteTransformationEndpoint))
                    {
                        //Fail
                        log.LogError($"Missing RemoteTransformationEndpoint env. variable");
                    }
                    else
                    {
                        HttpRequestMessage remoteReq = new(HttpMethod.Post, RemoteTransformationEndpoint);

                        WebApiSkillRequest req = new();
                        req.Values.Add(inObj.record);

                        var json = JsonConvert.SerializeObject(req);
                        //construct content to send
                        remoteReq.Content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");


                        HttpResponseMessage response = await webclient.SendAsync(remoteReq);

                        if (!response.IsSuccessStatusCode)
                        {
                            res = $"Remote Image Transformation - {docitem.WebUrl} sent with error. Code {response.StatusCode}";
                        }
                        else
                        {
                            res = await response.Content.ReadAsStringAsync(); 
                        }
                    }
                }
                else
                {
                    res = await ImageExtraction.ExtractAsync(inObj.headers, docitem);
                }

                log.LogInformation($"Response {res}");
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                log.LogInformation(ex.StackTrace);
            }
        }
    }
}