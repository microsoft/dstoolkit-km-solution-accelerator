// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Commons;
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
using System.Threading.Tasks;

namespace Metadata.Extraction
{
    public static class DurableMetadataExtractionSkill
    {
        [FunctionName("DurableMetadataExtractionSkill")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            DurableInputRecord record = context.GetInput<DurableInputRecord>();

            await context.CallActivityAsync("DurableMetadataExtractionSkill_Processing", record);

        }

        [FunctionName("DurableMetadataExtractionSkill_Processing")]
        public static async Task ProcessingAsync([ActivityTrigger] DurableInputRecord inObj, ILogger log)
        {
            try
            {
                IDocumentEntity docitem = new IDocumentEntity
                {
                    Id = (string)inObj.record.Data["document_id"],
                    Name = (string)inObj.record.Data["document_filename"],
                    WebUrl = (string)inObj.record.Data["document_url"]
                };

                string res = await RecursiveMetadataExtractionSkill.MetadataExtractionProcess(inObj.headers, docitem);

            }
            catch (Exception ex)
            {
                log.LogWarning("Tika Metadata Exception " + ex.Message);
                log.LogInformation(ex.StackTrace);
            }
        }

        [FunctionName("DurableMetadataExtractionSkill_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            //string requestBody = await req.Content.ReadAsStringAsync();
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
                    string instanceId = await starter.StartNewAsync("DurableMetadataExtractionSkill", null, inObj);

                    log.LogInformation($"Started orchestration with ID '{instanceId}' for file {(string)record.Data["document_url"]}");

                    WebApiResponseRecord responseRecord = new()
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

            HttpResponseMessage skillResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            skillResponse.Content = new StringContent(JsonConvert.SerializeObject(response));
            skillResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return skillResponse;
        }
    }
}