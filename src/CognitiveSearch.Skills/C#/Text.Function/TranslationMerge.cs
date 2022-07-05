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
using System.Text;
using System.Threading.Tasks;

namespace Text.TranslationMerge
{
    public static class TranslationMerge
    {
        [FunctionName("TranslationMerge")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext executionContext)
        {
            log.LogInformation("Merge Translations Pages function: C# HTTP trigger function processed a request.");

            IEnumerable<WebApiRequestRecord> requestRecords = await WebApiSkillHelpers.GetRequestRecordsAsync(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{executionContext.FunctionName} - Invalid request record array.");
            }

            WebApiSkillResponse response = WebApiSkillHelpers.ProcessRequestRecords(HeadersHelper.ConvertFunctionHeaders(req.Headers), executionContext.FunctionName, requestRecords,
                 (headers, inRecord, outRecord) =>
                 {
                     inRecord.Data.TryGetValue("fromLanguageCode", out object fromLanguage);
                     inRecord.Data.TryGetValue("toLanguageCode", out object toLanguage);

                     if (fromLanguage.Equals(toLanguage))
                     {
                         // Skipping populating any translation as there were any...
                         outRecord.Data[$"merged_translation"] = String.Empty;
                     }
                     else
                     {
                         StringBuilder sb = new StringBuilder();

                         JArray values = (JArray)inRecord.Data["translated_pages"];

                         foreach (var item in values)
                         {
                             sb.AppendLine(item.ToString());
                         }

                         outRecord.Data[$"merged_translation"] = sb.ToString();
                     }

                     return outRecord;
                 });

            return new OkObjectResult(response);
        }
    }
}
