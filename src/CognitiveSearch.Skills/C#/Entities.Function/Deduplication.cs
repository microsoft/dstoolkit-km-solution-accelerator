// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Services.Common.WebApiSkills;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Entities.Skills
{
    public static class Deduplication
    {
        private static readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

        [FunctionName("deduplication")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext ctx)
        {
            log.LogInformation($"{ctx.FunctionName} function: C# HTTP trigger processed a request.");

            string recordId = null;
            string requestBody = new StreamReader(req.Body).ReadToEnd();

            if (requestBody.Length <= 0)
            {
                return new BadRequestObjectResult(" Could not find values array");
            }

            log.LogInformation(requestBody);

            WebApiSkillRequest data = JsonConvert.DeserializeObject<WebApiSkillRequest>(requestBody);

            WebApiEnricherResponse response = new WebApiEnricherResponse();
            response.Values = new List<WebApiResponseRecord>();

            foreach (var record in data?.Values)
            {
                try
                {
                    recordId = record.RecordId as string;

                    // Put together response.
                    WebApiResponseRecord responseRecord = new WebApiResponseRecord();
                    responseRecord.Data = new Dictionary<string, object>();
                    responseRecord.RecordId = recordId;

                    foreach (var item in record.Data.Keys)
                    {
                        log.LogInformation(item);

                        if (record.Data[item] != null)
                        {
                            JArray list = (JArray) record.Data[item];

                            List<string> strlist = list.ToObject<List<string>>();

                            if (strlist?.Count > 0)
                            {
                                strlist.Sort();                            
                                responseRecord.Data.Add(item, DedupContent(strlist));
                            }
                            else
                            {
                                responseRecord.Data.Add(item, strlist);
                            }
                        }
                    }

                    response.Values.Add(responseRecord);

                }
                catch (Exception ex)
                {
                    log.LogInformation(ex.StackTrace);

                    WebApiResponseRecord responseRecord = new WebApiResponseRecord
                    {
                        Data = new Dictionary<string, object>(),
                        RecordId = recordId
                    };

                    log.LogInformation(JsonConvert.SerializeObject(responseRecord.Data));

                    response.Values.Add(responseRecord);
                }
            }
            // End Loop 

            return new OkObjectResult(response);
        }

        private static object DedupContent(dynamic v)
        {
            IComparer<string> comparer = StringComparer.Create(CultureInfo.InvariantCulture,true);

            SortedSet<string> outcomes = new SortedSet<string>(comparer: comparer);

            foreach (dynamic item in v)
            {
                string toAdd = (string)item;

                // Remove special characters from entity values
                toAdd = toAdd.Replace('\n', ' ').Replace('\t',' ').Replace("  "," ").Trim();

                // If the item is already Upper cased we keep it that way
                // => this is the case for Acronyms. 
                if (toAdd.Equals(toAdd.ToUpperInvariant()))
                {
                    outcomes.Add(toAdd);
                }
                else
                {
                    outcomes.Add(textInfo.ToTitleCase(toAdd.ToLowerInvariant()));
                }
            }
            return outcomes; 
        }
    }


}
