// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Knowledge.Configuration.AzureStorage;
using Knowledge.Services;
using Knowledge.Services.Helpers;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using Newtonsoft.Json;
using System.Text;

namespace Knowledge.API.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StorageController : CustomControllerBase
    {
        private const string BLOB_RETRY_TAG = "AzureSearch_RetryTag";

        private readonly StorageConfig _storageConfig;

        private readonly HttpClient client = new();

        public StorageController(TelemetryClient telemetry, IQueryService client, StorageConfig storageConfig)
        {
            this.telemetryClient = telemetry;
            this.QueryService = client;
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

                        BlobHttpHeaders httpHeaders = new()
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

                            BlobHttpHeaders httpHeaders = new()
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

                            BlobHttpHeaders httpHeaders = new()
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

            QueryService.RunIndexers();

            return new JsonResult("Upload completed.");
        }

        [HttpPost("webupload")]
        public async Task<IActionResult> WebUpload(StorageRequest request)
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

                BlobHttpHeaders httpHeaders = new()
                {
                    ContentType = mimetype
                };

                IDictionary<string, string> metadata = new Dictionary<string, string>
                {
                    { "document_upload", "true" },
                    //Because the filename is not used to store the document (avoid special chars and long names issues) 
                    //We set the filename as custom Metadata
                    { "site_domain", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(host)) },

                    { "document_title", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(webpage.name)) },
                    { "document_snippet", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(webpage.snippet)) }
                };

                byte[] byteArray = await client.GetByteArrayAsync(webpage.url);

                if (byteArray.Length > 0)
                {
                    MemoryStream stream = new(byteArray);

                    await blockBlob.UploadAsync(stream, httpHeaders, metadata);
                }

                QueryService.RunIndexers();
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex); 
            }

            return new JsonResult("ok");
        }

        [HttpPost("urlupload")]
        public async Task<IActionResult> UrlUpload(StorageRequest request)
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

                BlobHttpHeaders httpHeaders = new()
                {
                    ContentType = mimetype
                };

                IDictionary<string, string> metadata = new Dictionary<string, string>
                {
                    { "document_upload", "true" },
                    //Because the filename is not used to store the document (avoid special chars and long names issues) 
                    //We set the filename as custom Metadata
                    { "site_domain", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(host)) }
                };

                byte[] byteArray = await client.GetByteArrayAsync(urltodownload);

                if (byteArray.Length > 0)
                {
                    MemoryStream stream = new(byteArray);

                    await blockBlob.UploadAsync(stream, httpHeaders, metadata);
                }

                QueryService.RunIndexers();
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex); 
            }

            return new JsonResult("ok");
        }

        private BlobContainerClient GetDefaultStorageContainer()
        {
            return this.GetStorageContainer();
        }

        private BlobContainerClient GetStorageContainer(int storageIndex = 0)
        {
            // the first container add. is the primary one where we should upload content
            var blobClient = new BlobServiceClient(new Uri($"https://{_storageConfig.StorageAccountName}.blob.core.windows.net/"), new DefaultAzureCredential());
            var client = blobClient.GetBlobContainerClient(_storageConfig.StorageContainers.Split(',')[storageIndex]);
            return client;
        }

        [HttpPost("tagblob")]
        public async Task<IActionResult> TagBlob(StorageRequest request)
        {
            if (! String.IsNullOrEmpty(request.path))
            {
                try
                {
                    BlobUriBuilder bloburi = new(new Uri(request.path));

                    var blobClient = new BlobServiceClient(new Uri($"https://{_storageConfig.StorageAccountName}.blob.core.windows.net/"), new DefaultAzureCredential());                    
                    var container = blobClient.GetBlobContainerClient(bloburi.BlobContainerName);         
                    BlobClient blockBlob = container.GetBlobClient(bloburi.BlobName);
                    BlobProperties blobprops = await blockBlob.GetPropertiesAsync();

                    IDictionary<string, string> metadata = new Dictionary<string, string>();

                    // Ensure we get the existing metadata
                    foreach (var metadataItem in blobprops.Metadata)
                    {
                        metadata.Add(metadataItem.Key, metadataItem.Value);
                    }
                    // Add our retry metadata
                    var now = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    if (metadata.ContainsKey(BLOB_RETRY_TAG))
                    {
                        metadata[BLOB_RETRY_TAG] = now;
                    }
                    else
                    {
                        metadata.Add(BLOB_RETRY_TAG, now);
                    }

                    await blockBlob.SetMetadataAsync(metadata);

                    QueryService.RunIndexers();
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);

                    return new EmptyResult();
                }

                return new OkResult();

            }
            else
            {
                return new BadRequestResult();
            }
        }
    }

    public class StorageRequest
    {
        public string? base64obj { get; set; }
        public string? path { get; set; }
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
