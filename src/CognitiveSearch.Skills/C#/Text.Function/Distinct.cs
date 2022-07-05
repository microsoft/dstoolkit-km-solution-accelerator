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

namespace Text.Distinct
{
    public static class Distinct
    {
        [FunctionName("Distinct")]
        public static IActionResult RunDistinct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext executionContext)
        {
            log.LogInformation("Distinct Custom Skill: C# HTTP trigger function processed a request.");

            string skillName = executionContext.FunctionName;
            IEnumerable<WebApiRequestRecord> requestRecords = WebApiSkillHelpers.GetRequestRecords(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{skillName} - Invalid request record array.");
            }

            Thesaurus thesaurus = null;
            try
            {
                thesaurus = new Thesaurus(executionContext.FunctionAppDirectory);
            }
            catch(Exception e)
            {
                throw new Exception("Failed to read and parse thesaurus.json.", e);
            }

            WebApiSkillResponse response = WebApiSkillHelpers.ProcessRequestRecords(HeadersHelper.ConvertFunctionHeaders(req.Headers), skillName, requestRecords,
                (headers, inRecord, outRecord) =>
                {
                    JArray wordsParameter = inRecord.Data.TryGetValue("words", out object wordsParameterObject) ?
                        wordsParameterObject as JArray : null;
                    if (wordsParameter is null)
                    {
                        throw new ArgumentException("Input data is missing a `words` array of words to de-duplicate.", "words");
                    }
                    var words = wordsParameter.Values<string>();
                    outRecord.Data["distinct"] = thesaurus.Dedupe(words);
                    return outRecord;
                });

            return new OkObjectResult(response);
        }
    }
}
