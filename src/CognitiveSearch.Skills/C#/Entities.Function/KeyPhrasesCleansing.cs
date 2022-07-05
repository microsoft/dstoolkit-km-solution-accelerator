// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Entities.Skills
{
    using Commons;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Services.Common.WebApiSkills;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    public static class KeyPhrasesCleansing
    {
        public static List<string> stopwords = new List<string>();

        [FunctionName("keyphrases-cleansing")]
        public static async Task<IActionResult> RunKeyPhrasesCleanUp(
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

            WebApiSkillResponse response = WebApiSkillHelpers.ProcessRequestRecords(HeadersHelper.ConvertFunctionHeaders(req.Headers), ctx.FunctionName, requestRecords,
                 (headers, inRecord, outRecord) =>
                 {
                     if (stopwords.Count == 0)
                     {
                         stopwords = getStopWords(ctx);
                     }

                     // Get the KeyPhrases
                     List<string> cleanedkp = new List<string>();
                     List<string> acronyms = new List<string>();

                     List<string> entities = new List<string>();
                     entities.AddRange(stopwords);

                     foreach (var key in inRecord.Data.Keys)
                     {
                         if (key != "keyPhrases")
                         {
                             if (inRecord.Data[key] == null)
                             {
                                 continue;
                             }

                             dynamic entitiesList = inRecord.Data[key];

                             if (entitiesList?.HasValues == true)
                             {
                                 foreach (dynamic iloc in entitiesList)
                                 {
                                     entities.Add(((string)iloc).ToUpperInvariant().Trim());
                                 }
                             }
                         }
                     }

                     if (inRecord.Data.ContainsKey("keyPhrases"))
                     {
                         dynamic list = inRecord.Data["keyPhrases"];

                         if (list?.HasValues == true)
                         {
                             foreach (dynamic iloc in list)
                             {
                                 string value = (string)iloc;

                                 value = value.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");

                                 CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
                                 if (!entities.Contains(value.ToUpperInvariant().Trim()))
                                 {
                                     string[] content = value.Trim().Split(' ');
                                     if (content.Length > 1)
                                         // Changing text to title case
                                         cleanedkp.Add(currentCulture.TextInfo.ToTitleCase(value.ToLower()));
                                     //  else
                                     //      acronyms.Add(currentCulture.TextInfo.ToTitleCase(value.ToLower()));
                                 }
                             }
                         }
                         //removing empty and duplicate values
                         cleanedkp = cleanedkp.Where(s => !string.IsNullOrWhiteSpace(s))
                         .Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();

                         acronyms = acronyms.Where(s => !string.IsNullOrWhiteSpace(s))
                         .Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();

                     }

                     outRecord.Data[$"keyPhrases"] = cleanedkp;
                     outRecord.Data[$"acronyms"] = acronyms;

                     return outRecord;
                 });

            return new OkObjectResult(response);
        }

        private static List<string> getStopWords(ExecutionContext executionContext)
        {
            var config = new ConfigurationBuilder()
            .SetBasePath(executionContext.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
            return config["stopwords"].Split(',').Select(s => s.ToUpperInvariant().Trim()).ToList();
        }
    }
}