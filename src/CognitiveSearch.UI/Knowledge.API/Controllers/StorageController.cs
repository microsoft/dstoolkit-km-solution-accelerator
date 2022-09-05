// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Knowledge.Services;
using Knowledge.Services.AzureStorage;
using Knowledge.Configuration;
using Knowledge.Services.Helpers;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Knowledge.API.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageController : AbstractApiController
    {
        private StorageConfig _storageConfig;

        private readonly HttpClient client = new();

        public StorageController(TelemetryClient telemetry, IQueryService client, StorageConfig storageConfig)
        {
            this.telemetryClient = telemetry;
            this._queryService = client;
            this._storageConfig = storageConfig;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload()
        {
            if (Request.Form.Files.Any())
            {
                var container = GetDefaultStorageContainer();

                foreach (var formFile in Request.Form.Files)
                {
                    if (formFile.Length > 0)
                    {
                        var blob = container.GetBlobClient(formFile.FileName);

                        var extension = Path.GetExtension(formFile.FileName);
                        var mimetype = MimeTypeMap.GetMimeType(extension);

                        BlobHttpHeaders httpHeaders = new BlobHttpHeaders
                        {
                            ContentType = mimetype
                        };

                        IDictionary<string, string> metadata = new Dictionary<string, string>
                        {
                            { "document_upload", "true" }
                        };

                        await blob.UploadAsync(formFile.OpenReadStream(),httpHeaders,metadata);
                    }
                }
            }

            if (Request.Form.Keys.Any())
            {
                var container = GetDefaultStorageContainer();

                foreach (var key in Request.Form.Keys)
                {
                    var value = Request.Form[key].ToString();

                    if (value.Length > 0)
                    {
                        if ( key.Contains("Image") )
                        {
                            var blob = container.GetBlobClient(Guid.NewGuid() + ".png");

                            var mimetype = MimeTypeMap.GetMimeType("png");

                            BlobHttpHeaders httpHeaders = new BlobHttpHeaders
                            {
                                ContentType = mimetype
                            };

                            IDictionary<string, string> metadata = new Dictionary<string, string>
                            {
                                { "document_upload", "true" }
                            };

                            MemoryStream content = new();
                            await content.WriteAsync(Convert.FromBase64String(value.Replace("data:image/png;base64,", String.Empty)));
                            content.Seek(0, 0);
                            await blob.UploadAsync(content, httpHeaders, metadata);
                        }
                        else
                        {
                            var blob = container.GetBlobClient(Guid.NewGuid() + ".txt");

                            var mimetype = MimeTypeMap.GetMimeType("txt");

                            BlobHttpHeaders httpHeaders = new BlobHttpHeaders
                            {
                                ContentType = mimetype
                            };

                            IDictionary<string, string> metadata = new Dictionary<string, string>
                            {
                                { "document_upload", "true" }
                            };

                            MemoryStream content = new();
                            await content.WriteAsync(System.Text.Encoding.UTF8.GetBytes(value));
                            content.Seek(0, 0);
                            await blob.UploadAsync(content, httpHeaders, metadata);

                        }
                    }
                }
            }

            _queryService.RunIndexers();

            return new JsonResult("Upload completed.");
        }

        [HttpPost("webupload")]
        public async Task<IActionResult> WebUpload(UploadRequest request)
        {
            try
            {
                WebResult webpage = JsonConvert.DeserializeObject<WebResult>(Encoding.UTF8.GetString(Convert.FromBase64String(request.base64obj)));

                // We can only upload into the documents container
                var container = GetDefaultStorageContainer();

                var host = new System.Uri(webpage.url).Host.ToLower();
                string metadataFileName = "web/" + host + "/" + Path.GetFileName(webpage.url).ToString();

                BlobClient blockBlob = container.GetBlobClient(metadataFileName);

                var mimetype = MimeTypeMap.GetMimeType(Path.GetExtension(webpage.url));

                if (String.IsNullOrEmpty(mimetype))
                {
                    mimetype = "application/pdf";
                }

                BlobHttpHeaders httpHeaders = new BlobHttpHeaders
                {
                    ContentType = mimetype
                };

                IDictionary<string, string> metadata = new Dictionary<string, string>
                {
                    { "document_upload", "true" }
                };
                //Because the filename is not used to store the document (avoid special chars and long names issues) 
                //We set the filename as custom Metadata
                metadata.Add("site_domain", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(host)));

                metadata.Add("document_title", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(webpage.name)));
                metadata.Add("document_snippet", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(webpage.snippet)));

                byte[] byteArray = await client.GetByteArrayAsync(webpage.url);

                if (byteArray.Length > 0)
                {
                    MemoryStream stream = new MemoryStream(byteArray);

                    await blockBlob.UploadAsync(stream, httpHeaders, metadata);
                }

                _queryService.RunIndexers();
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex); 
            }

            return new JsonResult("ok");
        }

        [HttpPost("urlupload")]
        public async Task<IActionResult> UrlUpload(UploadRequest request)
        {
            try
            {
                string urltodownload = Encoding.UTF8.GetString(Convert.FromBase64String(request.base64obj));

                // We can only upload into the documents container
                var container = GetDefaultStorageContainer();

                var host = new System.Uri(urltodownload).Host.ToLower();
                string metadataFileName = "web/" + host + "/" + Path.GetFileName(urltodownload).ToString();

                BlobClient blockBlob = container.GetBlobClient(metadataFileName);

                var mimetype = MimeTypeMap.GetMimeType(Path.GetExtension(urltodownload));

                if (String.IsNullOrEmpty(mimetype))
                {
                    mimetype = "application/pdf";
                }

                BlobHttpHeaders httpHeaders = new BlobHttpHeaders
                {
                    ContentType = mimetype
                };

                IDictionary<string, string> metadata = new Dictionary<string, string>
                {
                    { "document_upload", "true" }
                };

                //Because the filename is not used to store the document (avoid special chars and long names issues) 
                //We set the filename as custom Metadata
                metadata.Add("site_domain", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(host)));

                byte[] byteArray = await client.GetByteArrayAsync(urltodownload);

                if (byteArray.Length > 0)
                {
                    MemoryStream stream = new MemoryStream(byteArray);

                    await blockBlob.UploadAsync(stream, httpHeaders, metadata);
                }

                _queryService.RunIndexers();
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex); 
            }

            return new JsonResult("ok");
        }

        /// <summary>
        ///  Returns the requested document with an 'inline' content disposition header.
        ///  This hints to a browser to show the file instead of downloading it.
        /// </summary>
        /// <param name="storageIndex">The storage connection string index.</param>
        /// <param name="fileName">The storage blob filename.</param>
        /// <param name="mimeType">The expected mime content type.</param>
        /// <returns>The file data with inline disposition header.</returns>
        [HttpGet("preview/{storageIndex}/{fileName}/{mimeType}")]
        public async Task<FileContentResult> GetDocumentInline(int storageIndex, string fileName, string mimeType)
        {
            var decodedFilename = HttpUtility.UrlDecode(fileName);
            var container = GetStorageContainer(storageIndex);
            var blob = container.GetBlobClient(decodedFilename);
            using (var ms = new MemoryStream())
            {
                var downlaodInfo = await blob.DownloadAsync();
                await downlaodInfo.Value.Content.CopyToAsync(ms);
                Response.Headers.Add("Content-Disposition", "inline; filename=" + decodedFilename);
                return File(ms.ToArray(), HttpUtility.UrlDecode(mimeType));
            }
        }

        private BlobContainerClient GetDefaultStorageContainer()
        {
            return this.GetStorageContainer();
        }

        private BlobContainerClient GetStorageContainer(int storageIndex = 0)
        {
            string accountName = _storageConfig.StorageAccountName;
            string accountKey = _storageConfig.StorageAccountKey;

            // the first container add. is the primary one where we should upload content
            var container = new BlobContainerClient(new Uri(_storageConfig.GetStorageContainerAddresses()[storageIndex]), new StorageSharedKeyCredential(accountName, accountKey));
            return container;
        }
    }

    public class UploadRequest
    {
        public string base64obj { get; set; }
    }

    public class WebResult
    {
        public string id { get; set; }
        public string url { get; set; }
        public string name { get; set; }
        public string language { get; set; }
        public string snippet { get; set; }
        public string last_Crawled{ get; set; }
    }
}
