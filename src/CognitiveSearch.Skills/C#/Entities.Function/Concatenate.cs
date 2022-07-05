// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Services.Common.WebApiSkills;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Entities.Skills
{
    public static class Concatenate
    {
        private static readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

        [FunctionName("concatenation")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext ctx)
        {
            log.LogInformation($"{ctx.FunctionName} function: C# HTTP trigger processed a request.");

            IEnumerable<WebApiRequestRecord> requestRecords = await WebApiSkillHelpers.GetRequestRecordsAsync(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{ctx.FunctionName} - Invalid request record array.");
            }

            WebApiSkillResponse response = WebApiSkillHelpers.ProcessRequestRecords(HeadersHelper.ConvertFunctionHeaders(req.Headers), ctx.FunctionName, requestRecords, Transform);

            return new OkObjectResult(response);

        }

        public static WebApiResponseRecord Transform(CustomHeaders headers, WebApiRequestRecord inRecord, WebApiResponseRecord outRecord)
        {
            List<string> strlist = new();

            foreach (var item in inRecord.Data.Keys)
            {
                if (inRecord.Data[item] != null)
                {
                    JArray list = (JArray)inRecord.Data[item];

                    strlist.AddRange(list.ToObject<List<string>>());
                }
            }

            if (strlist?.Count > 0)
            {
                strlist.Sort();
                outRecord.Data.Add("concatenated_property", DedupContent(strlist));
            }
            else
            {
                outRecord.Data.Add("concatenated_property", strlist);
            }

            return outRecord;
        }

        private static object DedupContent(dynamic v)
        {
            IComparer<string> comparer = StringComparer.Create(CultureInfo.InvariantCulture,true);

            SortedSet<string> outcomes = new(comparer: comparer);

            foreach (dynamic item in v)
            {
                string toAdd = (string)item;

                // Remove special characters from entity values
                //toAdd = toAdd.Replace('\n', ' ').Replace('\t',' ').Replace("  "," ").Trim();

                // If the item is already Upper cased we keep it that way
                // => this is the case for Acronyms. 
                //if (toAdd.Equals(toAdd.ToUpperInvariant()))
                //{
                outcomes.Add(toAdd);
                //}
                //else
                //{
                //outcomes.Add(toAdd.ToLowerInvariant());
                //}
            }
            return outcomes; 
        }
    }
}
