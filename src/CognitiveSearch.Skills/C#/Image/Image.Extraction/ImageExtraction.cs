// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Commons;
using Microsoft.Services.Common;
using Microsoft.Services.Common.WebApiSkills;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Image.Commons.Extraction
{
    public static class ImageExtraction
    {
        // Create a single, static HttpClient
        private static readonly HttpClient webclient = new HttpClient();

        private static readonly Dictionary<string, BlobContainerClient> containers = new Dictionary<string, BlobContainerClient>();

        private static readonly BlobContainerClient metadatacontainer = new BlobContainerClient(IConstants.ContainerConnectionString, IConstants.MetadataContainerName);

        static ImageExtraction()
        {
            foreach (var container in IConstants.ContainerNames)
            {
                containers.Add(container, new BlobContainerClient(IConstants.ContainerConnectionString, container));
            }

            webclient.Timeout = TimeSpan.FromMinutes(30);
        }

        public static async Task<WebApiResponseRecord> Transform(CustomHeaders headers, WebApiRequestRecord inRecord, WebApiResponseRecord outRecord)
        {
            try
            {
                IDocumentEntity docitem = new IDocumentEntity
                {
                    IndexKey = (string)inRecord.Data["document_index_key"],
                    Id = (string)inRecord.Data["document_id"],
                    Name = (string)inRecord.Data["document_filename"],
                    WebUrl = (string)inRecord.Data["document_url"]
                };

                outRecord.Data["extracted_images"] = await ExtractAsync(headers, docitem);
            }
            catch (Exception)
            {
            }

            return outRecord;
        }

        public static async Task<string> ExtractAsync(CustomHeaders headers, IDocumentEntity docitem)
        {
            string response = string.Empty;

            //container.Uri
            MemoryStream mstream = new MemoryStream();

            // Find the container from the docitem url
            BlobUriBuilder blobUriBuilder = new BlobUriBuilder(new Uri(HttpUtility.UrlDecode(docitem.WebUrl)));

            containers.TryGetValue(blobUriBuilder.BlobContainerName, out BlobContainerClient container);

            if (container != null)
            {
                if (await BlobHelper.IsBlobExistsAsync(container, IDocumentEntity.GetRelativeContentBlobPath(docitem, container.Uri.ToString())))
                {
                    // Open the Document
                    BlobClient docblob = container.GetBlobClient(IDocumentEntity.GetRelativeContentBlobPath(docitem, container.Uri.ToString()));

                    Stream documentstream = await docblob.OpenReadAsync();
                    documentstream.CopyTo(mstream);

                    if (documentstream != null)
                    {
                        if (documentstream.Length > 0)
                        {
                            mstream.Seek(0, SeekOrigin.Begin);

                            if (!string.IsNullOrEmpty(IConstants.tikaEndpoint))
                            {
                                try
                                {
                                    string metadataFileName = IDocumentEntity.GetRelativeMetadataPath(docitem, container.Name, container.Uri.ToString()).TrimStart('/') + ".json";

                                    BlobClient metadataBlob = metadatacontainer.GetBlobClient(metadataFileName);

                                    string tikaRequestUrl = IConstants.tikaEndpoint;
                                    bool isConvertible = false;

                                    if (IConstants.tikaConvertExtensions.Count > 0)
                                    {
                                        string extension = Path.GetExtension(docitem.WebUrl)[1..];

                                        isConvertible = !string.IsNullOrEmpty(IConstants.tikaConvertExtensions.Where(x => x.Contains(extension)).FirstOrDefault());

                                        if (isConvertible)
                                        {
                                            // Fix to handle pptm as pptx + macros
                                            if (extension.Equals("pptm")) {
                                                extension = "pptx"; 
                                            }

                                            tikaRequestUrl += IConstants.tikaConvertEndPoint + "/" + extension;
                                        }
                                        else
                                        {
                                            tikaRequestUrl += IConstants.tikaUnpackEndPoint;
                                        }
                                    }
                                    else
                                    {
                                        tikaRequestUrl += IConstants.tikaUnpackEndPoint;
                                    }

                                    HttpRequestMessage tikarequest = new(HttpMethod.Put, tikaRequestUrl);

                                    tikarequest.Headers.Add("Accept", "*/*");

                                    AddTikaHeader(tikarequest.Headers, "User-Agent", IConstants.UserAgent);

                                    // TIKA ootb option to guide images extraction in PDF
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFextractInlineImages", "true");

                                    // Visit https://github.com/puthurr/tika-fork for more details on the below options
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFAllPagesAsImages", "false");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFFirstPageAsCoverImage", "true");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFSinglePagePDFAsImage", "true");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFStripedImagesHandling", "true");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFStripedImagesThreshold", "5");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFGraphicsToImage", "true");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFGraphicsToImageThreshold", "1000000");

                                    AddTikaHeader(tikarequest.Headers, "X-TIKA-AZURE-CONTAINER", "images");
                                    AddTikaHeader(tikarequest.Headers, "X-TIKA-AZURE-CONTAINER-DIRECTORY", IHelpers.Base64Encode(IDocumentEntity.GetRelativeImagesPath(docitem, container.Name, container.Uri.ToString())));
                                    AddTikaHeader(tikarequest.Headers, "X-TIKA-AZURE-CONTAINER-DIRECTORY-BASE64ENCODED", "true");

                                    // Id is already base64 encoded. 
                                    tikarequest.Headers.Add("X-TIKA-AZURE-META-imageparentkey", docitem.IndexKey);
                                    tikarequest.Headers.Add("X-TIKA-AZURE-META-imageparentid", docitem.Id);
                                    tikarequest.Headers.Add("X-TIKA-AZURE-META-imageparentfilename", IHelpers.Base64Encode(docitem.Name));
                                    tikarequest.Headers.Add("X-TIKA-AZURE-META-imageparenturl", IHelpers.Base64Encode(docitem.WebUrl));

                                    tikarequest.Content = new ByteArrayContent(mstream.ToArray());
                                    tikarequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                                    HttpResponseMessage predictionresponse = await webclient.SendAsync(tikarequest);

                                    if (!predictionresponse.IsSuccessStatusCode)
                                    {
                                        response = $"Images Extraction - {docitem.WebUrl} sent to Tika with error. Code {predictionresponse.StatusCode}";
                                    }
                                    else
                                    {
                                        // Azure based Image Extraction return an array of metadata
                                        // which first entry is the document metadata.
                                        string tikares = await predictionresponse.Content.ReadAsStringAsync();

                                        if (tikares.Length > 0)
                                        {
                                            try
                                            {
                                                List<JObject> items = JsonConvert.DeserializeObject<List<JObject>>(tikares);

                                                // Only persist the first item representing the document metadata
                                                JObject item = items[0];

                                                BlobHttpHeaders httpHeaders = new BlobHttpHeaders();
                                                IDictionary<string, string> metadata = new Dictionary<string, string>();

                                                httpHeaders.ContentType = "application/json";
                                                //Because the filename is not used to store the document (avoid special chars and long names issues) 
                                                //We set the filename as custom Metadata
                                                metadata.Add("document_id", docitem.Id);
                                                metadata.Add("document_filename", IHelpers.Base64Encode(docitem.Name));
                                                metadata.Add("document_url", IHelpers.Base64Encode(docitem.WebUrl));

                                                byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
                                                MemoryStream stream = new MemoryStream(byteArray);

                                                await metadataBlob.UploadAsync(stream, httpHeaders, metadata);
                                            }
                                            catch (Exception)
                                            {
                                                //DO NOTHING
                                            }
                                        }

                                        response = $"Images Extraction - {docitem.WebUrl} sent to Tika successfully.";

                                    }
                                }
                                catch (Exception ex)
                                {
                                    response = "Tika Unreachable " + ex.Message;
                                }
                            }

                        }
                        else
                        {
                            throw new FileNotFoundException($"Empty Document Stream {docitem.WebUrl}. Sending to retry.");
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException($"Can't get Document Stream {docitem.WebUrl}. Sending to retry.");
                    }
                }
                else
                {
                    throw new FileNotFoundException($"Blob not found {docitem.WebUrl}. Sending to retry.");
                }
            }

            return response;
        }


        private static void AddTikaHeader(HttpRequestHeaders headers, string key, string defaultValue)
        {
            string value = FEnvironment.StringReader(key.Replace("-", "_"), defaultValue);

            headers.Add(key, value);
        }
    }
}
