// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Commons;
using Microsoft.Services.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Metadata.Extraction
{
    public static class RecursiveMetadataExtractionSkill
    {
        // Create a single, static HttpClient
        private static readonly HttpClient webclient = new HttpClient();

        private static readonly Dictionary<string, BlobContainerClient> containers = new Dictionary<string, BlobContainerClient>();
        //private static readonly BlobContainerClient container = new BlobContainerClient(IConstants.ContainerConnectionString, IConstants.ContainerNames);
        private static readonly BlobContainerClient metadatacontainer = new BlobContainerClient(IConstants.ContainerConnectionString, IConstants.MetadataContainerName);

        static RecursiveMetadataExtractionSkill()
        {
            foreach (var container in IConstants.ContainerNames)
            {
                containers.Add(container, new BlobContainerClient(IConstants.ContainerConnectionString, container));
            }
        }

        public static async Task<string> MetadataExtractionProcess(CustomHeaders headers, IDocumentEntity docitem)
        {
            string response = String.Empty;

            //container.Uri
            MemoryStream mstream = new MemoryStream();

            // Find the right source container 
            BlobUriBuilder blobUriBuilder = new BlobUriBuilder(new Uri(UrlUtility.UrlDecode(docitem.WebUrl)));

            containers.TryGetValue(blobUriBuilder.BlobContainerName, out BlobContainerClient container);

            if (container != null)
            {
                if (await BlobHelper.IsBlobExistsAsync(container, docitem))
                {
                    // Open the Document
                    BlobClient docblob = container.GetBlobClient(IDocumentEntity.GetRelativeContentBlobPath(docitem, container.Uri.ToString()));

                    Stream documentstream = await docblob.OpenReadAsync();
                    documentstream.CopyTo(mstream);

                    if (documentstream != null)
                    {
                        if (documentstream.Length > 0)
                        {
                            int extractionCounter = 0;

                            mstream.Seek(0, SeekOrigin.Begin);

                            if (!String.IsNullOrEmpty(IConstants.tikaEndpoint))
                            {
                                try
                                {
                                    HttpRequestMessage tikarequest = new HttpRequestMessage(HttpMethod.Put, IConstants.tikaEndpoint + "/rmeta");

                                    tikarequest.Headers.Add("Accept", "application/json;charset=utf-8");

                                    AddTikaHeader(tikarequest.Headers, "User-Agent", IConstants.UserAgent);

                                    // TIKA ootb option to guide images extraction in PDF
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFextractInlineImages", "true");

                                    // Visit https://github.com/puthurr/tika for more details on the below options
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFAllPagesAsImages", "false");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFFirstPageAsCoverImage", "true");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFSinglePagePDFAsImage", "true");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFStripedImagesHandling", "true");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFStripedImagesThreshold", "5");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFGraphicsToImage", "true");
                                    AddTikaHeader(tikarequest.Headers, "X-Tika-PDFGraphicsToImageThreshold", "1000000");

                                    tikarequest.Content = new ByteArrayContent(mstream.ToArray());
                                    tikarequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                                    HttpResponseMessage predictionresponse = await webclient.SendAsync(tikarequest);

                                    if (predictionresponse.IsSuccessStatusCode)
                                    {
                                        string tikares = await predictionresponse.Content.ReadAsStringAsync();

                                        if (tikares.Length > 0)
                                        {
                                                List<JObject> items = JsonConvert.DeserializeObject<List<JObject>>(tikares);

                                                int ZipCounter = 0;

                                                //Look for the media directory within the pptx and extract all 
                                                foreach (JObject item in items)
                                                {
                                                    if (ZipCounter == 0)
                                                    {
                                                        //Document JSON Tika
                                                        string metadataFileName = IDocumentEntity.GetRelativeMetadataPath(docitem, container.Name, container.Uri.ToString()).TrimStart('/') + ".json";
 
                                                        BlobClient blockBlob = metadatacontainer.GetBlobClient(metadataFileName);
                                                        //CloudBlockBlob blockBlob = metadatacontainer.GetBlockBlobReference(metadataFileName);

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

                                                        await blockBlob.UploadAsync(stream, httpHeaders, metadata);
                                                    }
                                                    else
                                                    {
                                                        //string metadataFileName = docitem.GetRelativeMetadataPath() + "/" + item["resourceName"] + ".json";
                                                        string metadataFileName = IDocumentEntity.GetRelativeMetadataPath(docitem, container.Name, container.Uri.ToString()).TrimStart('/') + "/" + item["resourceName"] + ".json";

                                                        //CloudBlockBlob imageblob = metadatacontainer.GetBlockBlobReference(metadataFileName);
                                                        BlobClient imageblob = metadatacontainer.GetBlobClient(metadataFileName);

                                                        BlobHttpHeaders httpHeaders = new BlobHttpHeaders();
                                                        IDictionary<string, string> metadata = new Dictionary<string, string>();

                                                        httpHeaders.ContentType = "application/json";
                                                        //Because the filename is not used to store the document (avoid special chars and long names issues) 
                                                        //We set the filename as custom Metadata
                                                        metadata.Add("parentid", docitem.Id);
                                                        metadata.Add("parentfilename", IHelpers.Base64Encode(docitem.Name));
                                                        metadata.Add("parenturl", IHelpers.Base64Encode(docitem.WebUrl));

                                                        byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
                                                        MemoryStream stream = new MemoryStream(byteArray);

                                                        await imageblob.UploadAsync(stream, httpHeaders, metadata);

                                                        extractionCounter++;
                                                    }

                                                    //Embedded Objects Tika
                                                    ZipCounter++;
                                                }
                                        }
                                        else
                                        {
                                            throw new Exception($"Tika Metadata response is empty  {docitem.WebUrl}.");
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception($"Tika Metadata response code is {predictionresponse.StatusCode} for document {docitem.WebUrl}.");
                                    }
                                }
                                catch (Exception)
                                {
                                    throw new Exception($"Tika Metadata endpoint is unreachable.");
                                }
                            }

                            response = ($"Metadata Extraction - {docitem.WebUrl} sent to Tika successfully.");
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
