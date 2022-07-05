// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Services.Common.WebApiSkills;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Text.Mesh
{
    public static class TextMerge
    {
        [FunctionName("TextMerge")]
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
            StringBuilder sb = new StringBuilder();

            foreach (var key in inRecord.Data.Keys)
            {
                sb.AppendLine(inRecord.Data[key].ToString());
            }

            outRecord.Data[$"merged_text"] = sb.ToString();

            return outRecord;
        }
    }
}
