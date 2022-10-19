// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Services.Common;
using Microsoft.Services.Common.WebApiSkills;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Metadata.Extraction
{
    public static class MetadataExtractionSkill
    {
        private const string MetadataFileExtension = ".json";

        // Create a single, static HttpClient
        private static readonly HttpClient webclient = new HttpClient();

        private static readonly Dictionary<string, BlobContainerClient> containers = new Dictionary<string, BlobContainerClient>();
        //private static readonly BlobContainerClient documentsContainer = new BlobContainerClient(IConstants.ContainerConnectionString, IConstants.ContainerNames);

        private static readonly BlobContainerClient imagesContainer = new BlobContainerClient(IConstants.ContainerConnectionString, IConstants.ImageContainerName);
        private static readonly BlobContainerClient metadatacontainer = new BlobContainerClient(IConstants.ContainerConnectionString, IConstants.MetadataContainerName);

        static MetadataExtractionSkill()
        {
            foreach (var container in IConstants.ContainerNames)
            {
                containers.Add(container, new BlobContainerClient(IConstants.ContainerConnectionString, container));
            }
        }

        [FunctionName("MetadataExtractionSkill")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext executionContext)
        {
            log.LogInformation("Metadata Assignment function: C# HTTP trigger function processed a request.");

            IEnumerable<WebApiRequestRecord> requestRecords = await WebApiSkillHelpers.GetRequestRecordsAsync(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{executionContext.FunctionName} - Invalid request record array.");
            }

            WebApiSkillResponse response = await WebApiSkillHelpers.ProcessRequestRecordsAsync(HeadersHelper.ConvertFunctionHeaders(req.Headers), executionContext.FunctionName, requestRecords, Transform);

            return new OkObjectResult(response);
        }

        public static async Task<WebApiResponseRecord> Transform(CustomHeaders headers,WebApiRequestRecord inRecord, WebApiResponseRecord outRecord)
        {
            IDocumentEntity docitem = new IDocumentEntity
            {
                Id = (string)inRecord.Data["document_id"],
                Name = (string)inRecord.Data["document_filename"],
                WebUrl = (string)inRecord.Data["document_url"]
            };

            // Find the right source container 
            BlobUriBuilder blobUriBuilder = new(new Uri(UrlUtility.UrlDecode(docitem.WebUrl)));

            containers.TryGetValue(blobUriBuilder.BlobContainerName, out BlobContainerClient container);

            bool IsExtractedImageFile = false;

            if (container is null)
            {
                if (blobUriBuilder.BlobContainerName.Equals(imagesContainer.Name))
                {
                    container = imagesContainer;

                    IsExtractedImageFile = true;
                }
            }

            if ( container != null)
            {
                // check if the metadata file already exists or not.
                string metadataFileName = IDocumentEntity.GetRelativeMetadataPath(docitem, IsExtractedImageFile? String.Empty : container.Name, container.Uri.ToString()).TrimStart('/') + ".json";

                // Skip the metadata piece if this is an image with -99999 in filename
                if (docitem.IsPageImage())
                {
                    if (inRecord.Data.ContainsKey("imageparenturl"))
                    {
                        //// Take the parent metadata file here so we have consistency
                        string parentUrl = (string)inRecord.Data["imageparenturl"];
                        docitem.ParentUrl = IHelpers.Base64Decode(parentUrl);

                        BlobUriBuilder parentBlobUriBuilder = new(new Uri(UrlUtility.UrlDecode(docitem.ParentUrl)));

                        containers.TryGetValue(parentBlobUriBuilder.BlobContainerName, out BlobContainerClient parentContainer);

                        if (parentContainer != null)
                        {
                            metadataFileName = IDocumentEntity.GetRelativeMetadataPathByUrl(docitem.ParentUrl, parentContainer.Name, parentContainer.Uri.ToString()).TrimStart('/') + MetadataFileExtension;
                        }
                    }
                }

                bool noExtraction = docitem.IsPageImage() && (await BlobHelper.IsBlobExistsAsync(metadatacontainer, metadataFileName)); 

                if (noExtraction)
                {
                    BlobClient metadataBlob = metadatacontainer.GetBlobClient(metadataFileName);

                    Stream metadataStream = await metadataBlob.OpenReadAsync();
                    StreamReader sr = new StreamReader(metadataStream);
                    string tikares = await sr.ReadToEndAsync();

                    JObject resobj = JsonConvert.DeserializeObject<JObject>(tikares);
                    JObject outobj = new JObject();

                    foreach (var prop in resobj.Properties())
                    {
                        string newName = ConvertMetadataName(prop.Name);

                        outobj.Add(newName, prop.Value);
                    }
                    outRecord.Data[$"file_metadata"] = outobj;

                }
                else
                {
                    //Revert the metadata file name as we didn't find its own or parent. 
                    metadataFileName = IDocumentEntity.GetRelativeMetadataPath(docitem, IsExtractedImageFile ? String.Empty : container.Name, container.Uri.ToString()).TrimStart('/') + ".json";

                    MemoryStream mstream = new();

                    if (await BlobHelper.IsBlobExistsAsync(container, blobUriBuilder.BlobName))
                    {
                        // Open the original Document
                        BlobClient docblob = container.GetBlobClient(blobUriBuilder.BlobName);
                        Stream documentstream = await docblob.OpenReadAsync();
                        documentstream.CopyTo(mstream);

                        if (documentstream != null)
                        {
                            if (documentstream.Length > 0)
                            {
                                mstream.Seek(0, SeekOrigin.Begin);

                                if (!String.IsNullOrEmpty(IConstants.tikaEndpoint))
                                {
                                    try
                                    {
                                        HttpRequestMessage tikarequest = new(HttpMethod.Put, IConstants.tikaEndpoint + "/meta");

                                        tikarequest.Headers.Add("Accept", "application/json;charset=utf-8");
                                        tikarequest.Headers.Add("User-Agent", IConstants.UserAgent);

                                        tikarequest.Content = new ByteArrayContent(mstream.ToArray());
                                        tikarequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                                        HttpResponseMessage predictionresponse = await webclient.SendAsync(tikarequest);

                                        if (predictionresponse.IsSuccessStatusCode)
                                        {
                                            string tikares = await predictionresponse.Content.ReadAsStringAsync();

                                            if (tikares.Length > 0)
                                            {
                                                // Create the file_metadata output for it 
                                                JObject resobj = JsonConvert.DeserializeObject<JObject>(tikares);

                                                JObject outobj = new JObject();

                                                foreach (var prop in resobj.Properties())
                                                {
                                                    string newName = ConvertMetadataName(prop.Name);

                                                    outobj.Add(newName, prop.Value);
                                                }
                                                outRecord.Data[$"file_metadata"] = outobj;

                                                // Save to blob store 
                                                try
                                                {
                                                    BlobClient metadataBlob = metadatacontainer.GetBlobClient(metadataFileName);
                                                    BlobHttpHeaders httpHeaders = new BlobHttpHeaders();
                                                    IDictionary<string, string> metadata = new Dictionary<string, string>();

                                                    httpHeaders.ContentType = "application/json";
                                                    //Because the filename is not used to store the document (avoid special chars and long names issues) 
                                                    //We set the filename as custom Metadata
                                                    metadata.Add("document_id", docitem.Id);
                                                    metadata.Add("document_filename", IHelpers.Base64Encode(docitem.Name));
                                                    metadata.Add("document_url", IHelpers.Base64Encode(docitem.WebUrl));

                                                    byte[] byteArray = Encoding.UTF8.GetBytes(tikares);
                                                    MemoryStream stream = new MemoryStream(byteArray);

                                                    await metadataBlob.UploadAsync(stream, httpHeaders, metadata);
                                                }
                                                catch (Exception)
                                                {
                                                }
                                            }
                                            else
                                            {
                                                outRecord.Warnings.Add(new WebApiResponseWarning
                                                {
                                                    Message = ($"Tika Metadata response is empty. {docitem.WebUrl}.")
                                                });
                                            }
                                        }
                                        else
                                        {
                                            outRecord.Warnings.Add(new WebApiResponseWarning
                                            {
                                                Message = ($"Tika Metadata response code is {predictionresponse.StatusCode} for document {docitem.WebUrl}.")
                                            });
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        outRecord.Warnings.Add(new WebApiResponseWarning
                                        {
                                            Message = ($"Tika Metadata exception {ex.Message} for document {docitem.WebUrl}.")
                                        });
                                    }
                                }
                            }
                            else
                            {
                                outRecord.Warnings.Add(new WebApiResponseWarning
                                {
                                    Message = ($"Empty Document Stream {docitem.WebUrl}.")
                                });
                            }
                        }
                        else
                        {
                            outRecord.Warnings.Add(new WebApiResponseWarning
                            {
                                Message = ($"Can't get Document Stream {docitem.WebUrl}.")
                            });
                        }
                    }
                    else
                    {
                        outRecord.Warnings.Add(new WebApiResponseWarning
                        {
                            Message = ($"Blob not found {docitem.WebUrl}.")
                        });
                    }

                }
            }

            return outRecord;
        }

        private static string ConvertMetadataName (string oldname)
        {
            return oldname.Replace(" ","-").Replace(":","-"); 
        }
    }
}

