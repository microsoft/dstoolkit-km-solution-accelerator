// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.AzureSearch.REST
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    public class AzureSearchRESTService : AbstractSearchService
    {
        protected async Task<AzureSearchRESTResponse> AzureSearchRestAPI(string indexName, object request)
        {
            AzureSearchRESTResponse jsonresponse = new AzureSearchRESTResponse();

            string searchServiceUrl = "https://" + this.serviceConfig.ServiceName + ".search.windows.net/indexes/" + indexName + "/docs/search?api-version=" + this.serviceConfig.APIVersion;

            string azureSearchResult = null;

            string reqbody = Newtonsoft.Json.JsonConvert.SerializeObject(request); 

            using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, searchServiceUrl))
            {
                req.Headers.Add("Accept", "application/json");
                req.Headers.Add("api-key", this.serviceConfig.QueryKey);
                req.Content = new StringContent(reqbody);
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage similarResponse = await httpClient.SendAsync(req);

                if (similarResponse.IsSuccessStatusCode)
                {
                    azureSearchResult = await similarResponse.Content.ReadAsStringAsync();

                    if ( azureSearchResult != null) jsonresponse = Newtonsoft.Json.JsonConvert.DeserializeObject<AzureSearchRESTResponse>(azureSearchResult);
                }
            }

            return jsonresponse;
        }
    }
}
