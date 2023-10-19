// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.API.Models;
using Knowledge.Configuration;
using Knowledge.Models;
using Knowledge.Services;
using Knowledge.Services.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Knowledge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentController : CustomControllerBase
    {
        private IMetadataService metadataService { get; set; }

        private const string DEFAULT_INDEX_NAME = "index";

        public DocumentController(IQueryService client, SearchServiceConfig svcconfig, IMetadataService msvc)
        {
            QueryService = client;
            Config = svcconfig;
            this.metadataService = msvc;
        }

        [HttpPost("getbyindexkey")]
        public async Task<IActionResult> GetDocumentByIndexKeyAsync(ApiSearchRequest request)
        {
            request.indexName = DEFAULT_INDEX_NAME;
            request.permissions = GetUserPermissions();

            var result = await QueryService.GetDocumentByIndexKey(request);

            return CreateContentResultResponse(result);
        }

        [HttpPost("getbyid")]
        public async Task<IActionResult> GetDocumentByIdAsync(ApiSearchRequest request)
        {
            request.indexName = DEFAULT_INDEX_NAME;
            request.permissions = GetUserPermissions();

            var result = await QueryService.GetDocumentById(request);

            return CreateContentResultResponse(result);
        }

        [HttpPost("getcoverimage")]
        public async Task<IActionResult> GetDocumentCoverImageAsync(string document_id)
        {
            if (!string.IsNullOrEmpty(document_id))
            {
                ApiSearchRequest request = new()
                {
                    document_id = document_id,
                    indexName = DEFAULT_INDEX_NAME,
                    permissions = GetUserPermissions()
                };

                var result = await QueryService.GetDocumentCoverImage(request);

                if (result.Count > 0)
                {
                    JObject document = (JObject)result.Results[0]["Document"];

                    JObject? image = document.GetValue("image") as JObject;

                    if (! String.IsNullOrEmpty((string)image.GetValue("thumbnail_medium")))
                    {
                        return Content((string)image.GetValue("thumbnail_medium"));
                    }
                }
            }

            return new NotFoundResult();
        }

        [HttpPost("getcoverimagebyindexkey")]
        public async Task<IActionResult> GetDocumentCoverImageByIndexKeyAsync(string index_key)
        {
            if (!string.IsNullOrEmpty(index_key))
            {
                ApiSearchRequest request = new()
                {
                    index_key = index_key,
                    indexName = DEFAULT_INDEX_NAME,
                    permissions = GetUserPermissions()
                };

                var result = await QueryService.GetDocumentCoverImageByIndexKey(request);

                if (result.Count > 0)
                {
                    JObject document = (JObject)result.Results[0]["Document"];

                    JObject? image = document.GetValue("image") as JObject;

                    if (! String.IsNullOrEmpty((string)image.GetValue("thumbnail_medium")))
                    {
                        return Content(content: (string)image.GetValue("thumbnail_medium"));
                    }
                }
            }

            return new NotFoundResult();
        }

        [HttpPost("getmetadata")]
        public async Task<IActionResult> GetDocumentMetadataAsync(ApiMetadataRequest request)
        {
            if (!string.IsNullOrEmpty(request.path))
            {
                var result = await metadataService.GetDocumentMetadataAsync(request.path,IMetadataService.JsonMetadata);

                return CreateContentResultResponse(result);
            }

            return new BadRequestResult();
        }

        [HttpPost("gethtml")]
        public async Task<IActionResult> GetDocumentHTMLAsync(ApiMetadataRequest request)
        {
            if (!string.IsNullOrEmpty(request.path))
            {
                var result = await metadataService.GetDocumentMetadataAsync(request.path, IMetadataService.HtmlMetadata);
                ContentResult response = new()
                {
                    Content = result,
                    ContentType = "text/plain",
                    StatusCode = (int)HttpStatusCode.OK
                };
                return response;
            }

            return new BadRequestResult();
        }


        [HttpPost("getembedded")]
        public async System.Threading.Tasks.Task<IActionResult> GetDocumentEmbeddedAsync(ApiSearchRequest request)
        {
            request.indexName = DEFAULT_INDEX_NAME;
            request.permissions = GetUserPermissions();
            SearchResponse result = await QueryService.GetDocumentEmbedded(request);

            return CreateContentResultResponse(result);
        }

        [HttpPost("getsiblings")]
        public async System.Threading.Tasks.Task<IActionResult> GetDocumentSiblings(ApiSearchRequest request)
        {
            request.indexName = DEFAULT_INDEX_NAME;
            request.permissions = GetUserPermissions();
            SearchResponse result = await QueryService.GetDocumentSiblings(request);

            return CreateContentResultResponse(result);
        }
    }
}
