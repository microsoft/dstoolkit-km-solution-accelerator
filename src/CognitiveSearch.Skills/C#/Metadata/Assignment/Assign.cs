// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Commons;
using Metadata.Assignment;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Services.Common;
using Microsoft.Services.Common.WebApiSkills;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment
{
    public static class Assign
    {
        public static List<MappingEntry> mapping;
        public static List<SecurityEntry> security;
        public static List<SourceEntry> source;

        public static ConcurrentDictionary<string,Func<string, List<string>>> transformations= new ConcurrentDictionary<string,Func<string, List<string>>>();

        [FunctionName("Assign")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext ctx)
        {
            log.LogInformation("Metadata Assignment function: C# HTTP trigger function processed a request.");

            if (mapping == null || security == null || source == null)
            {
                // Metadata mapping
                var filePath = Path.Combine(ctx.FunctionAppDirectory, "mapping.json");
                mapping = JsonConvert.DeserializeObject<List<MappingEntry>>(File.ReadAllText(filePath));
                // Security mapping
                filePath = Path.Combine(ctx.FunctionAppDirectory, "security.json");
                security = JsonConvert.DeserializeObject<List<SecurityEntry>>(File.ReadAllText(filePath));
                // Source mapping
                filePath = Path.Combine(ctx.FunctionAppDirectory, "source.json");
                source = JsonConvert.DeserializeObject<List<SourceEntry>>(File.ReadAllText(filePath));

                // Load all Transformations
                transformations = new ConcurrentDictionary<string, Func<string, List<string>>>();
                transformations.TryAdd("DoNothing", DoNothing);
                transformations.TryAdd("SplitSemiColumn", SplitSemiColumn);
                transformations.TryAdd("SplitComma", SplitComma);
                transformations.TryAdd("SplitSPTaxonomy", SplitSPTaxonomy);
            }

            IEnumerable<WebApiRequestRecord> requestRecords = await WebApiSkillHelpers.GetRequestRecordsAsync(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{ctx.FunctionName} - Invalid request record array.");
            }

            WebApiSkillResponse response = WebApiSkillHelpers.ProcessRequestRecords(HeadersHelper.ConvertFunctionHeaders(req.Headers), ctx.FunctionName, requestRecords, Transform);

            return new OkObjectResult(response);

        }

        public static WebApiResponseRecord Transform(CustomHeaders headers, WebApiRequestRecord inRecord, WebApiResponseRecord outRecord)
        {
            // Extract metadata 
            string filename = (string)inRecord.Data["document_filename"];

            Dictionary<string, object> assignedMetadata = new Dictionary<string, object>();

            // Source Processing Date
            assignedMetadata[$"source_processing_date"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Document Url Segments from document_url
            string[] pathTokens = null;

            assignedMetadata[$"document_segments"] = new List<string>();

            if (inRecord.Data.ContainsKey("document_url"))
            {
                string furl = (string)inRecord.Data["document_url"];
                if (!String.IsNullOrEmpty(furl))
                {
                    string decoded_url = UrlUtility.UrlDecode(furl);

                    // TBD - Is lower necessary ? 
                    pathTokens = decoded_url.ToLowerInvariant().Split('/', StringSplitOptions.RemoveEmptyEntries);

                   if (pathTokens.Length > 4) {
                        assignedMetadata[$"document_segments"] = pathTokens[3..^1];
                   }
                }
            }

            // Content Group
            if (headers != null && headers.ContainsKey("content_group"))
            {
                assignedMetadata[$"content_group"] = headers["content_group"];
            }
            else
            {
                string content_group = "Other";
                string extension = Path.GetExtension(filename).ToLowerInvariant();

                switch (extension)
                {
                    case ".ppt":
                    case ".pptx":
                    case ".pptm":
                        content_group = "PowerPoint";
                        break;
                    case ".mp4":
                    case ".mov":
                    case ".wmv":
                        content_group = "Video";
                        break;
                    case ".mp3":
                        content_group = "Audio";
                        break;
                    case ".pdf":
                        content_group = "PDF";
                        break;
                    case ".doc":
                    case ".docx":
                        content_group = "Word";
                        break;
                    case ".xls":
                    case ".xlsx":
                    case ".xlsm":
                        content_group = "Excel";
                        break;
                    case ".jpg":
                    case ".jpeg":
                    case ".gif":
                    case ".tif":
                    case ".tiff":
                    case ".png":
                    case ".bmp":
                        content_group = "Image";
                        break;
                    case ".msg":
                    case ".eml":
                        content_group = Constants.email_content_group;
                        break;
                    default:
                        break;
                }

                assignedMetadata[$"content_group"] = content_group;

            }

            //Email Support 

            if (assignedMetadata[$"content_group"].ToString().ToLower().Contains(Constants.email_content_group.ToLower())){
                Dictionary<string, object> emailMetadata = new Dictionary<string, object>();

                if (inRecord.Data.ContainsKey(Constants.email_to))
                {
                    emailMetadata[Constants.email_to_output] = inRecord.Data[Constants.email_to];
                }
                if (inRecord.Data.ContainsKey(Constants.email_from))
                {
                    emailMetadata[Constants.email_from_output] = inRecord.Data[Constants.email_from];
                }
                if (inRecord.Data.ContainsKey(Constants.email_cc))
                {
                    emailMetadata[Constants.email_cc_output] = inRecord.Data[Constants.email_cc];
                }
                if (inRecord.Data.ContainsKey(Constants.email_bcc))
                {
                    emailMetadata[Constants.email_bcc_output] = inRecord.Data[Constants.email_bcc];
                }
                if (inRecord.Data.ContainsKey(Constants.email_subject))
                {
                    emailMetadata[Constants.email_subject_output] = inRecord.Data[Constants.email_subject];
                }
                assignedMetadata["email_metadata"] = emailMetadata;
            }

            // Security
            TransformSecurity(headers,inRecord,assignedMetadata);

            // Pages Count
            assignedMetadata[$"page_count"] = 0;

            if (inRecord.Data.ContainsKey("metadata_page_count"))
            {
                assignedMetadata[$"page_count"] = inRecord.Data["metadata_page_count"];
            }

            // Page Number
            assignedMetadata[$"page_number"] = 0;

            // Document Converted
            if (inRecord.Data.ContainsKey("document_converted"))
            {
                assignedMetadata[$"document_converted"] = inRecord.Data["document_converted"];
            }
            else
            {
                assignedMetadata[$"document_converted"] = false;
            }

            // Document Embedded
            if (inRecord.Data.ContainsKey("document_embedded"))
            {
                assignedMetadata[$"document_embedded"] = inRecord.Data["document_embedded"];
            }
            else
            {
                assignedMetadata[$"document_embedded"] = false;
            }

            // Translation flags
            if (inRecord.Data.ContainsKey("document_translated"))
            {
                assignedMetadata[$"document_translated"] = inRecord.Data["document_translated"];
            }
            else
            {
                assignedMetadata[$"document_translated"] = false;
            }
            if (inRecord.Data.ContainsKey("document_translatable"))
            {
                assignedMetadata[$"document_translatable"] = inRecord.Data["document_translatable"];
            }
            else
            {
                assignedMetadata[$"document_translatable"] = false;
            }

            // Is this an embedded document ?
            if (inRecord.Data.ContainsKey("parentid"))
            {
                //assignedMetadata[$"document_embedded"] = true;

                // Page Number
                try
                {
                    string[] tokens = filename.Split('-');

                    if (tokens.Length > 2)
                    {
                        assignedMetadata[$"page_number"] = Int32.Parse(tokens[1]);
                    }
                }
                catch (Exception)
                {
                    assignedMetadata[$"page_number"] = 0;
                }
            }

            // DEFAULT TITLE
            if (inRecord.Data.ContainsKey("metadata_title"))
            {
                string title = (string)inRecord.Data["metadata_title"];

                string target_title = title.Trim();

                if (String.IsNullOrEmpty(target_title))
                {
                    target_title = Path.GetFileNameWithoutExtension(filename);
                }
                else
                {
                    switch (target_title)
                    {
                        case "PowerPoint Presentation":
                        case "PowerPoint-Präsentation":
                        case "PowerPoint template":
                            target_title = Path.GetFileNameWithoutExtension(filename);
                            break;
                    }

                }
                assignedMetadata[$"title"] = target_title;
            }

            // Content Source
            TransformSource(headers, inRecord, assignedMetadata);

            // TOPICS from tags | separated 
            if (inRecord.Data.ContainsKey("tags"))
            {
                string tags = (string)inRecord.Data["tags"];
                string[] tokens = tags.Split('|', StringSplitOptions.RemoveEmptyEntries);

                assignedMetadata[$"topics"] = new List<string>(tokens);
            }

            // AUTHORS 
            if (inRecord.Data.ContainsKey("author"))
            {
                // Put the single-value author into a list of authors
                string author = (string)inRecord.Data["author"];

                if (String.IsNullOrEmpty(author))
                {
                    assignedMetadata[$"authors"] = new List<string>();
                }
                else
                {
                    assignedMetadata[$"authors"] = new List<string>() { author };
                }
            }
            else
            {
                assignedMetadata[$"authors"] = new List<string>();
            }

            // Part where we map file metadata into skill metadata for indexing.
            if (inRecord.Data.ContainsKey("file_metadata"))
            {
                JObject filemetadata = (JObject)inRecord.Data["file_metadata"];

                foreach (var mentry in mapping)
                {
                    if (filemetadata.ContainsKey(mentry.source))
                    {
                        foreach (var target in mentry.target)
                        {
                            if (String.IsNullOrEmpty(mentry.transform))
                            {
                                var metadata_value = filemetadata[mentry.source];

                                if (metadata_value.GetType() == typeof(JArray))
                                {
                                    foreach (JValue item in (JArray)metadata_value)
                                    {
                                        assignedMetadata[target] = String.Join(';', (JArray)metadata_value);
                                    }
                                }
                                else if (metadata_value.GetType() == typeof(JValue))
                                {
                                    if (metadata_value.ToString().Trim().Length > 0)
                                    {
                                        assignedMetadata[target] = metadata_value;
                                    }
                                }
                                else
                                {
                                    if (metadata_value.ToString().Trim().Length > 0)
                                    {
                                        assignedMetadata[target] = metadata_value;
                                    }
                                }
                            }
                            else
                            {
                                List<string> transformed_value = new List<string>();

                                var transformation_method = transformations["DoNothing"];

                                if (transformations.ContainsKey(mentry.transform))
                                {
                                    transformation_method = transformations[mentry.transform];
                                }

                                if (filemetadata[mentry.source].GetType() == typeof(JArray))
                                {
                                    foreach (JValue item in ((JArray)filemetadata[mentry.source]).Cast<JValue>())
                                    {
                                        transformed_value.AddRange(transformation_method(item.ToString()));
                                    }
                                }
                                else if (filemetadata[mentry.source].GetType() == typeof(JValue))
                                {
                                    transformed_value = transformation_method((string)filemetadata[mentry.source]);
                                }

                                transformed_value = transformed_value.Distinct().ToList();

                                if (assignedMetadata.ContainsKey(target))
                                {
                                    // Appending to existing list
                                    if (assignedMetadata[target].GetType() == typeof(List<string>)) {

                                        ((List<string>)assignedMetadata[target]).AddRange(transformed_value);

                                        assignedMetadata[target] = ((List<string>)assignedMetadata[target]).Distinct().ToList();
                                    }
                                    else {
                                        // Overwriting existing value
                                        assignedMetadata[target] = transformed_value;
                                    }
                                }
                                else
                                {
                                    assignedMetadata.Add(target, transformed_value);
                                }
                            }
                        }
                    }
                }
            }

            // TODO - Fallback values - factorize this
            if (assignedMetadata.ContainsKey("title"))
            {
                if (String.IsNullOrEmpty(assignedMetadata[$"title"].ToString().Trim()))
                {
                    assignedMetadata[$"title"] = Path.GetFileNameWithoutExtension(filename);
                }
            }
            else
            {
                assignedMetadata[$"title"] = Path.GetFileNameWithoutExtension(filename);
            }

            outRecord.Data["skill_metadata"] = assignedMetadata;

            return outRecord;
        }

        public static void TransformSecurity(CustomHeaders headers, WebApiRequestRecord inRecord, Dictionary<string, object> assignedMetadata)
        {
            // Default values
            bool restricted = false;
            List<string> permissions = new();

            string[] pathTokens = null;

            // document_url
            if (inRecord.Data.ContainsKey("document_url"))
            {
                string furl = (string)inRecord.Data["document_url"];
                if (!String.IsNullOrEmpty(furl))
                {
                    string decoded_url = UrlUtility.UrlDecode(furl);
                    pathTokens = decoded_url.ToLowerInvariant().Split('/', StringSplitOptions.RemoveEmptyEntries);
                }
            }
            // parent.url
            if (inRecord.Data.ContainsKey("parenturl"))
            {
                string furl = (string)inRecord.Data["parenturl"];
                if (!String.IsNullOrEmpty(furl))
                {
                    string decoded_url = UrlUtility.UrlDecode(IHelpers.Base64Decode(furl));
                    pathTokens = decoded_url.ToLowerInvariant().Split('/', StringSplitOptions.RemoveEmptyEntries);
                }
            }

            if (pathTokens != null && pathTokens.Length > 0)
            {
                foreach (var mentry in security)
                {
                    // If both path array and source array intersects (not perfect) then we restrict
                    if (mentry.source.Intersect(pathTokens).ToList().Count == mentry.source.Length)
                    {
                        restricted = true;
                        permissions.AddRange(mentry.target);
                    }
                }
            }

            // Default is unrestricted
            assignedMetadata[$"restricted"] = restricted;

            // Permissions
            assignedMetadata[$"permissions"] = permissions;
        }

        public static void TransformSource(CustomHeaders headers, WebApiRequestRecord inRecord, Dictionary<string, object> assignedMetadata)
        {
            if (headers != null && headers.ContainsKey("content_source"))
            {
                assignedMetadata[$"content_source"] = headers["content_source"];
            }
            else
            {
                string[] pathTokens = null;

                // document_url
                if (inRecord.Data.ContainsKey("document_url"))
                {
                    string furl = (string)inRecord.Data["document_url"];
                    if (! String.IsNullOrEmpty(furl))
                    {
                        string decoded_url = UrlUtility.UrlDecode(furl);
                        pathTokens = decoded_url.ToLowerInvariant().Split('/', StringSplitOptions.RemoveEmptyEntries);

                        if (pathTokens.Length > 3)
                        {
                            assignedMetadata[$"content_source"] = pathTokens[2];
                        }
                    }
                }
                // parent.url
                if (inRecord.Data.ContainsKey("parenturl"))
                {
                    string furl = (string)inRecord.Data["parenturl"];
                    if (!String.IsNullOrEmpty(furl))
                    {
                        string decoded_url = UrlUtility.UrlDecode(IHelpers.Base64Decode(furl));
                        pathTokens = decoded_url.ToLowerInvariant().Split('/', StringSplitOptions.RemoveEmptyEntries);

                        if (pathTokens.Length > 3)
                        {
                            assignedMetadata[$"content_source"] = pathTokens[2];
                        }
                    }
                }

                if (pathTokens != null && pathTokens.Length > 0)
                {
                    foreach (var mentry in source)
                    {
                        // If both path array and source array intersects (not perfect because of order...) then we restrict
                        if (mentry.source.Intersect(pathTokens).ToList().Count == mentry.source.Length)
                        {
                            assignedMetadata[$"content_source"] = mentry.target;

                            break;
                        }
                    }
                }
            }
        }
        
        public static List<string> SplitGeneric(string input, char separator)
        {
            List<string> results = new List<string>();

            string[] tokens = input.Split(separator);

            foreach (string token in tokens)
            {
                results.Add(token.Trim());
            }

            return results;
        }
        public static List<string> DoNothing(string input)
        {
            return new List<string>() { input };
        }
        public static List<string> SplitSemiColumn(string input)
        {
            return SplitGeneric(input, ';');
        }
        public static List<string> SplitComma(string input)
        {
            return SplitGeneric(input, ',');
        }
        public static List<string> SplitSPTaxonomy(string input)
        {
            List<string> results = new List<string>();

            string[] tokens = input.Split(';');

            foreach (string token in tokens)
            {
                string[] subtokens = token.Split('|');
                if (subtokens.Length > 1)
                {
                    results.Add(subtokens[0].Replace("#", String.Empty).Trim());                    
                }
            }
            return results;
        }
    }

    public class MappingEntry
    {
        public string source { get; set; }
        public string[] target { get; set; }
        public string transform { get; set; }
    }

    public class SecurityEntry
    {
        public string[] source { get; set; }
        public string[] target { get; set; }
        public string transform { get; set; }
    }
    public class SourceEntry
    {
        public string[] source { get; set; }
        public string target { get; set; }
        public string transform { get; set; }
    }
}

