// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration.Translation;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Knowledge.Services.Translation
{
    public class TranslationService : AbstractService, ITranslationService
    {
        public TranslationConfig config;

        public TranslationService(IDistributedCache cache, TranslationConfig serviceConfig, TelemetryClient telemetry)
        {
            this.distCache = cache;
            this.config = serviceConfig;
            this.CachePrefix = this.GetType().Name;
            this.telemetryClient = telemetry;
        }

        // Async call to the Translator
        public async Task<Translation> TranslateSearchText(string route, string inputText)
        {
            /*
             * The code for your call to the translation service will be added to this
             * function in the next few sections.
             */
            object[] body = new object[] { new { Text = inputText } };

            var requestBody = JsonConvert.SerializeObject(body);

            using (var request = new HttpRequestMessage())
            {
                // In the next few sections you'll add code to construct the request.

                // Build the request.
                // Set the method to Post.
                request.Method = HttpMethod.Post;
                // Construct the URI and add headers.
                request.RequestUri = new Uri(this.config.Endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", this.config.SubscriptionKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", this.config.ServiceRegion);

                // Send the request and get response.
                HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    // Read response as a string.
                    string result = await response.Content.ReadAsStringAsync();

                    try
                    {
                        // Deserialize the response using the classes created earlier.
                        TranslationResult[] deserializedOutput = JsonConvert.DeserializeObject<TranslationResult[]>(result);
                        // Iterate over the deserialized results.
                        foreach (TranslationResult o in deserializedOutput)
                        {
                            // Iterate over the results and print each translation.
                            foreach (Translation t in o.Translations)
                            {
                                return t;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.telemetryClient.TrackException(ex);
                    }
                }

                return null;
            }
        }


        public string TranslateSearchText(string searchText)
        {
            string result = this.distCache.GetString(CachePrefix + searchText);

            if (!String.IsNullOrEmpty(result))
            {
                return result;
            }
            else
            {
                // Translation 
                string route = $"/translate?api-version=3.0&to={this.config.SuggestedTo}&suggestedFrom={this.config.SuggestedFrom}";

                var translation = TranslateSearchText(route, searchText);
                if (translation != null)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(translation.Result.Text))
                        {
                            searchText = translation.Result.Text;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return searchText;
        }
    }

    /// <summary>
    /// The C# classes that represents the JSON returned by the Translator.
    /// </summary>
    public class TranslationResult
    {
        public DetectedLanguage DetectedLanguage { get; set; }
        public TextResult SourceText { get; set; }
        public Translation[] Translations { get; set; }
    }

    public class DetectedLanguage
    {
        public string Language { get; set; }
        public float Score { get; set; }
    }

    public class TextResult
    {
        public string Text { get; set; }
        public string Script { get; set; }
    }

    public class Translation
    {
        public string Text { get; set; }
        public TextResult Transliteration { get; set; }
        public string To { get; set; }
        public Alignment Alignment { get; set; }
        public SentenceLength SentLen { get; set; }
    }

    public class Alignment
    {
        public string Proj { get; set; }
    }

    public class SentenceLength
    {
        public int[] SrcSentLen { get; set; }
        public int[] TransSentLen { get; set; }
    }
}
