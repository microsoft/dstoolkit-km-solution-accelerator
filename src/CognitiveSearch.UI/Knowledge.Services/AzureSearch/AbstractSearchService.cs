// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Knowledge.Configuration;
using Knowledge.Configuration.AzureStorage;
using Knowledge.Configuration.Graph;
using Knowledge.Models;
using Knowledge.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Net;

namespace Knowledge.Services.AzureSearch
{
    public abstract class AbstractSearchService : AbstractService
    {
        protected const string QUERY_ALL = QueryHelper.QUERY_ALL;

        protected SearchIndexClient _searchIndexClient;

        protected SearchServiceConfig serviceConfig;

        protected GraphConfig graphConfig;

        protected StorageConfig storageConfig;

        protected List<SearchModel> SearchModels = new();


        protected virtual void InitSearchClients()
        {
            _searchIndexClient = new SearchIndexClient(new Uri($"https://{this.serviceConfig.ServiceName}.search.windows.net/"), new DefaultAzureCredential(), new SearchClientOptions(SearchClientOptions.ServiceVersion.V2023_11_01));
            var schema = new SearchSchema().AddFields(_searchIndexClient.GetIndex(serviceConfig.IndexName).Value.Fields);
            SearchModels.Add(new SearchModel(schema, this.serviceConfig, this.graphConfig, serviceConfig.IndexName));
        }

        protected SearchModel GetModel(string indexName = null)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                indexName = this.serviceConfig.IndexName;
            }

            foreach (SearchModel item in SearchModels)
            {
                if (item.IndexName.IndexOf(indexName) > -1)
                {
                    return item;
                }
            }

            return SearchModels[0];
        }

        /// <summary>
        /// This will return all SAS tokens for the configured storage accounts
        /// </summary>
        /// <returns></returns>
        protected Dictionary<string, string> GetContainerSasUris()
        {
            // data source information. Currently supporting x data sources indexed by different indexers
            // We need to refresh the s_tokens every time or they will become invalid.
            Dictionary<string, string> s_tokens = new();

            var blobClient = new BlobServiceClient(new Uri($"https://{storageConfig.StorageAccountName}.blob.core.windows.net/"), new DefaultAzureCredential());

            var containers = storageConfig.StorageContainers.Split(',');
            for (int i = 0; i < containers.Length; i++)
            {
                var container = blobClient.GetBlobContainerClient(containers[0]);

                var policy = new BlobSasBuilder
                {
                    Protocol = SasProtocol.HttpsAndHttp,
                    BlobContainerName = container.Name,
                    Resource = "c",
                    StartsOn = DateTimeOffset.UtcNow,
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(24),
                    IPRange = new SasIPRange(IPAddress.None, IPAddress.None)
                };
                policy.SetPermissions(BlobSasPermissions.Read);

                var userDelegationKey = blobClient.GetUserDelegationKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(4));

                var sas = policy.ToSasQueryParameters(userDelegationKey, container.AccountName).ToString();
                s_tokens.TryAdd($"https://{storageConfig.StorageAccountName}.blob.core.windows.net/{containers[i]}", "?" + sas);
            }

            return s_tokens;
        }
    }
}
