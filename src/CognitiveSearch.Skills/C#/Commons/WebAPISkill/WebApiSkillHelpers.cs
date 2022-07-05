// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Commons;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Services.Common.WebApiSkills
{
    public static class WebApiSkillHelpers
    {
        public static bool TestMode = false;
        public static Func<HttpRequestMessage, HttpResponseMessage> TestWww;

        public static async Task<IEnumerable<WebApiRequestRecord>> GetRequestRecordsAsync(HttpRequest req)
        {
            string jsonRequest = await new StreamReader(req.Body).ReadToEndAsync();
            if (String.IsNullOrEmpty(jsonRequest))
            {
                return null;
            }
            WebApiSkillRequest docs = JsonConvert.DeserializeObject<WebApiSkillRequest>(jsonRequest);
            return docs.Values;
        }

        public static IEnumerable<WebApiRequestRecord> GetRequestRecords(HttpRequest req)
        {
            string jsonRequest = new StreamReader(req.Body).ReadToEnd();
            WebApiSkillRequest docs = JsonConvert.DeserializeObject<WebApiSkillRequest>(jsonRequest);
            return docs.Values;
        }

        public static WebApiSkillResponse ProcessRequestRecords(CustomHeaders headers, string functionName, IEnumerable<WebApiRequestRecord> requestRecords, Func<CustomHeaders, WebApiRequestRecord, WebApiResponseRecord, WebApiResponseRecord> processRecord)
        {
            WebApiSkillResponse response = new WebApiSkillResponse();

            foreach (WebApiRequestRecord inRecord in requestRecords)
            {
                WebApiResponseRecord outRecord = new WebApiResponseRecord() { RecordId = inRecord.RecordId };

                try
                {
                    outRecord = processRecord(headers, inRecord, outRecord);
                }
                catch (Exception e)
                {
                    outRecord.Warnings.Add(new WebApiResponseWarning() { Message = $"{functionName} - Error processing the request record : {e}" });
                }
                response.Values.Add(outRecord);
            }

            return response;
        }

        public static async Task<WebApiSkillResponse> ProcessRequestRecordsAsync(CustomHeaders headers,
            string functionName,
            IEnumerable<WebApiRequestRecord> requestRecords,
            Func<CustomHeaders, WebApiRequestRecord, WebApiResponseRecord, Task<WebApiResponseRecord>> processRecord)
        {
            WebApiSkillResponse response = new WebApiSkillResponse();

            foreach (WebApiRequestRecord inRecord in requestRecords)
            {
                WebApiResponseRecord outRecord = new WebApiResponseRecord() { RecordId = inRecord.RecordId };

                try
                {
                    outRecord = await processRecord(headers, inRecord, outRecord);
                }
                catch (Exception e)
                {
                    outRecord.Warnings.Add(new WebApiResponseWarning() { Message = $"{functionName} - Error processing the request record : {e}" });
                }
                response.Values.Add(outRecord);
            }

            return response;
        }

        public static async Task<IEnumerable<T>> FetchAsync<T>(string uri, string collectionPath)
            => await FetchAsync<T>(uri, null, null, collectionPath, HttpMethod.Get);

        public static async Task<IEnumerable<T>> FetchAsync<T>(string uri, string apiKeyHeader, string apiKey, string collectionPath)
            => await FetchAsync<T>(uri, apiKeyHeader, apiKey, collectionPath, HttpMethod.Get);

        public static async Task<IEnumerable<T>> FetchAsync<T>(
            string uri,
            string collectionPath,
            HttpMethod method,
            byte[] postBody = null,
            string contentType = null)
            => await FetchAsync<T>(uri, null, null, collectionPath, method, postBody, contentType);

        public static async Task<IEnumerable<T>> FetchAsync<T>(
            string uri,
            string apiKeyHeader,
            string apiKey,
            string collectionPath,
            HttpMethod method,
            byte[] postBody = null,
            string contentType = null)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = method;
                request.RequestUri = new Uri(uri);
                if (postBody != null)
                {
                    request.Content = new ByteArrayContent(postBody);
                }
                if (contentType != null)
                {
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                }
                if (apiKeyHeader != null)
                {
                    request.Headers.Add(apiKeyHeader, apiKey);
                }

                using (HttpResponseMessage response = TestMode ? TestWww(request) : await client.SendAsync(request))
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject responseObject = JObject.Parse(responseBody);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"The remote service {uri} responded with a {response.StatusCode} error code: {responseObject["message"]?.ToObject<string>()}");
                    }

                    if (responseObject == null || !(responseObject.SelectToken(collectionPath) is JToken resultsToken))
                    {
                        return Array.Empty<T>();
                    }
                    return resultsToken
                        .Children()
                        .Select(token => token.ToObject<T>())
                        .ToList();
                }
            }
        }


        public static string CombineSasTokenWithUri(string uri, string sasToken)
        {
            // if this data is coming from blob indexer's metadata_storage_path and metadata_storage_sas_token
            // then we can simply concat them. But lets use uri builder to be safe and support missing characters

            UriBuilder uriBuilder = new UriBuilder(uri);
            NameValueCollection sasParameters = HttpUtility.ParseQueryString(sasToken ?? string.Empty);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            foreach (var key in sasParameters.AllKeys)
            {
                // override this url parameter if it already exists
                query[key] = sasParameters[key];
            }

            uriBuilder.Query = query.ToString();
            var finalUrl = uriBuilder.ToString();

            return finalUrl;
        }

    }
}
