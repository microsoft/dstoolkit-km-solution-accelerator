// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Services.Common.WebApiSkills;
using System.Linq;
using Newtonsoft.Json.Linq;
using Commons;
using System.Text.RegularExpressions;

namespace Text.Mesh
{
    public static class TextMesh
    {
        public const char WORD_CESURE_CHARACTER = '-';
        public const char LINE_JOIN_CHAR = ' ';

        public const string CONTENT_ALREADY_TRIMMED_HEADER = "content-already-trimmed";

        public static string pattern = @"^\b[0-9][A-Z0-9]{3}[A-Z0-9]{2}\b\W.*";

        // Create a Regex  
        public static Regex rg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [FunctionName("TextMesh")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext ctx)
        {
            log.LogInformation("TextMesh function: C# HTTP trigger function processed a request.");

            IEnumerable<WebApiRequestRecord> requestRecords = await WebApiSkillHelpers.GetRequestRecordsAsync(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{ctx.FunctionName} - Invalid request record array.");
            }

            WebApiSkillResponse response = WebApiSkillHelpers.ProcessRequestRecords(HeadersHelper.ConvertFunctionHeaders(req.Headers), ctx.FunctionName, requestRecords, Transform);

            return new OkObjectResult(response);

        }

        public static WebApiResponseRecord Transform(CustomHeaders headers,WebApiRequestRecord inRecord, WebApiResponseRecord outRecord)
        {
            List<string> content = new List<string>();

            if (inRecord.Data.ContainsKey("content")) 
            {
                content.AddRange(inRecord.Data["content"].ToString().Split('\n'));
            }

            if (inRecord.Data.ContainsKey("content_lines"))
            {
                foreach (var item in ((JArray)inRecord.Data["content_lines"]))
                {
                    content.AddRange(item.Value<string>().Split('\n'));
                }
            }

            outRecord.Data["original_content_lines_count"] = content.Count;

            List<int> indices = new List<int>();
            List<string> values = new List<string>();
            List<string> content_trim = new List<string>();

            if (headers.ContainsKey(CONTENT_ALREADY_TRIMMED_HEADER))
            {
                for (int i = 0; i < content.Count; i++)
                {
                    var linev = content[i].Trim('\n').Trim('\r').Trim();

                    if (linev.Length > 0)
                    {
                        content_trim.Add(linev);
                    }
                }
            }
            else
            {
                for (int i = 0; i < content.Count; i++)
                {
                    var linev = content[i].Trim('\n').Trim('\r').Trim();

                    if (linev.Length > 0)
                    {
                        indices.Add(i);
                        values.Add(linev);
                    }
                }

                for (int i = 0; i < indices.Count; i++)
                {
                    if (i > 0 && indices[i] == (indices[i - 1] + 1))
                    {
                        if (content_trim[^1].EndsWith(WORD_CESURE_CHARACTER))
                        {
                            content_trim[^1] = content_trim[^1].Substring(0, content_trim[^1].Length - 1);

                            if (values[i].StartsWith(WORD_CESURE_CHARACTER))
                            {
                                content_trim[^1] += values[i][1..^1];
                            }
                            else
                            {
                                content_trim[^1] += values[i];
                            }
                        }
                        else
                        {
                            content_trim[^1] += LINE_JOIN_CHAR + values[i];
                        }
                    }
                    else
                    {
                        content_trim.Add(values[i]);
                    }
                }
            }

            List<string> matches = new List<string>();

            // Apply a set of regex to match specific line of text like Title or else. 
            foreach (var item in content_trim)
            {
                if (rg.IsMatch(item))
                {
                    matches.Add(item);
                }
            }

            outRecord.Data["trimmed_content"] = String.Join(Environment.NewLine, content_trim.ToArray<string>());
            outRecord.Data["trimmed_content_lines_count"] = content_trim.Count;
            outRecord.Data["trimmed_content_lines_matches"] = matches;

            return outRecord;
        }
    }
}
