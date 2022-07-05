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
using Commons;

namespace Text.Mesh
{
    public static class EntitiesMesh
    {
        public const char WORD_CESURE_CHARACTER = '-';
        public const char LINE_JOIN_CHAR = ' ';

        [FunctionName("EntitiesMesh")]
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
            string[] content = inRecord.Data["content"].ToString().Split('\n');

            List<int> indices = new List<int>();
            List<string> values = new List<string>();
            List<string> content_trim = new List<string>();

            for (int i = 0; i < content.Length; i++)
            {
                var linev = content[i].Trim('\n').Trim('\r').Trim();

                if (linev.Length > 0) {
                    indices.Add(i);
                    values.Add(linev);
                }
                //content_trim.Add(linev);
            }

            //outRecord.Data["content_sparse_indices"] = indices;
            //outRecord.Data["content_sparse_values"] = values;

            for (int i = 0; i < indices.Count; i++)
            {
                if (i > 0 && indices[i] == (indices[i-1] + 1))
                {
                    if ( content_trim[^1].EndsWith(WORD_CESURE_CHARACTER) )
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

            outRecord.Data["trimmed_content"] = String.Join(Environment.NewLine, content_trim.ToArray<string>());
            outRecord.Data["trimmed_content_lines"] = content_trim.Count;

            return outRecord;
        }
    }
}
