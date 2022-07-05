// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Commons;
using Image.Commons.Extraction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Services.Common.WebApiSkills;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Image.Extraction
{
    public static class ImageExtractionSkill
    {
        [FunctionName("ImageExtractionSkill")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log, 
            ExecutionContext executionContext)
        {
            log.LogInformation("ImageExtractionSkill : HTTP trigger function processed a request.");

            IEnumerable<WebApiRequestRecord> requestRecords = await WebApiSkillHelpers.GetRequestRecordsAsync(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{executionContext.FunctionName} - Invalid request record array.");
            }

            WebApiSkillResponse response = await WebApiSkillHelpers.ProcessRequestRecordsAsync(HeadersHelper.ConvertFunctionHeaders(req.Headers), executionContext.FunctionName, requestRecords, ImageExtraction.Transform);

            return new OkObjectResult(response);
        }
    }
}
