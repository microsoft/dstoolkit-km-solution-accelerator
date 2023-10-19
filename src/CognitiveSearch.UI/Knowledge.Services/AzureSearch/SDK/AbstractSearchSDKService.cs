// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Search.Documents;
using System.Collections.Generic;

namespace Knowledge.Services.AzureSearch.SDK
{
    public class AbstractSearchSDKService : AbstractSearchService
    {
        protected List<SearchClient> _searchClients = new();

        protected override void InitSearchClients()
        {
            base.InitSearchClients();
            _searchClients.Add(_searchIndexClient.GetSearchClient(serviceConfig.IndexName));
        }

        protected SearchClient GetSearchClient(string indexName = null)
        {
            if (string.IsNullOrEmpty(indexName))
            {
                indexName = this.serviceConfig.IndexName;
            }

            foreach (var client in this._searchClients)
            {
                if (client.IndexName.IndexOf(indexName) > -1)
                {
                    return client;
                }
            }

            return _searchClients[0];
        }
    }
}
