// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Knowledge.Services.SpellChecking.Bing
{
    /// <summary>
    /// The Suggestion entity class.
    /// </summary>
    public class Suggestion
    {
        /// <summary>
        /// Gets or sets the suggestion value.
        /// </summary>
        /// <value>
        /// The suggestion value.
        /// </value>
        [JsonProperty("suggestion")]
        public string SuggestionValue { get; set; }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>
        /// The score.
        /// </value>
        [JsonProperty("score")]
        public double Score { get; set; }
    }

    /// <summary>
    /// The Flagged Token entity class.
    /// </summary>
    public class FlaggedToken
    {
        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>
        /// The offset.
        /// </value>
        [JsonProperty("offset")]
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the type of the token.
        /// </summary>
        /// <value>
        /// The type of the token.
        /// </value>
        [JsonProperty("type")]
        public string SuggestionType { get; set; }

        /// <summary>
        /// Gets or sets the suggestions.
        /// </summary>
        /// <value>
        /// The suggestions.
        /// </value>
        [JsonProperty("suggestions")]
        public List<Suggestion> Suggestions { get; set; }
    }

    /// <summary>
    /// The spell check response entity class.
    /// </summary>
    public class SpellcheckResponse
    {
        /// <summary>
        /// Gets or sets the type of the response.
        /// </summary>
        /// <value>
        /// The type of the response.
        /// </value>
        [JsonProperty("_type")]
        public string ResponseType { get; set; }

        /// <summary>
        /// Gets or sets the flagged tokens.
        /// </summary>
        /// <value>
        /// The flagged tokens.
        /// </value>
        [JsonProperty("flaggedTokens")]
        public List<FlaggedToken> FlaggedTokens { get; set; }
    }

    public class BingSpellCheckingService : AbstractService, ISpellCheckingService, ISpellCheckingProvider
    {
        public SpellCheckingConfig config;

        /// <summary>
        /// The HTTP client
        /// </summary>
        static string path = "/v7.0/spellcheck";
        //static string market = "en-US";
        //static string mode = "spell";

        public BingSpellCheckingService(SpellCheckingConfig config)
        {
            this.config = config;
            this.CachePrefix = this.GetType().Name;

            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config.SubscriptionKey);
        }

        public string GetProvider()
        {
            return "Bing";
        }

        public async Task<string> SpellCheckAsync(string text)
        {
            string url = config.Endpoint + path;

            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("setLang", config.SupportedLanguages),
                new KeyValuePair<string, string>("mkt", config.Market),
                new KeyValuePair<string, string>("mode", config.Mode),
                new KeyValuePair<string, string>("text", text)
            };

            HttpResponseMessage response = new();

            using (FormUrlEncodedContent content = new FormUrlEncodedContent(values))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                response = await httpClient.PostAsync(url, content);
            }

            string responseStr = await response.Content.ReadAsStringAsync();

            // Handling Too many requests / Quota Exceeded ... 
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ReasonPhrase == "Too Many Requests")
                {
                    return null;
                }
                else if (response.ReasonPhrase == "Quota Exceeded")
                {
                    return null;
                }
                else
                {
                    string errorStr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    return null;
                }
            }

            // Deserialize the JSON response from the API
            SpellcheckResponse spellResponse = JsonConvert.DeserializeObject<SpellcheckResponse>(responseStr);

            if (spellResponse == null)
            {
                return text;
            }

            // Apply spelling corrections
            var isCorrected = false;
            char[] splitchar = { ' ' };
            string[] correctedQueryArray = text.Split(splitchar);

            // go through spelling suggestions and apply them
            if ((spellResponse != null) && (spellResponse.FlaggedTokens != null) && (spellResponse.FlaggedTokens.Count > 0))
            {
                isCorrected = true;

                var replacedIndex = 0;

                for (var i = 0; i < spellResponse.FlaggedTokens.Count; i++)
                {
                    for (var j = replacedIndex; j < correctedQueryArray.Length; j++)
                    {
                        if (correctedQueryArray[j] == spellResponse.FlaggedTokens[i].Token)
                        {
                            replacedIndex = j + 1;
                            correctedQueryArray[j] = spellResponse.FlaggedTokens[i].Suggestions[0].SuggestionValue;
                            break;
                        }
                    }
                }
            }

            if (isCorrected)
            {
                return string.Join(" ", correctedQueryArray);
            }
            else
            {
                return text;
            }        
        }
    }
}
