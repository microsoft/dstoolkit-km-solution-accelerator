// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Knowledge.Configuration;
using Knowledge.Configuration.Graph;
using Knowledge.Services.AzureStorage;
using Knowledge.Services.Helpers;
using Knowledge.Services.Models;
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
            // Create an HTTP reference to the catalog index
            this._searchIndexClient = new SearchIndexClient(new Uri($"https://{this.serviceConfig.ServiceName}.search.windows.net/"), new AzureKeyCredential(this.serviceConfig.AdminKey));

            foreach (var index in this.serviceConfig.GetSearchIndexes())
            {
                try
                {
                    var schema = new SearchSchema().AddFields(_searchIndexClient.GetIndex(index).Value.Fields);

                    SearchModels.Add(new SearchModel(schema, this.serviceConfig, this.graphConfig, index));

                }
                catch (Exception)
                {
                }
            }
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
            List<string> s_containerAddresses = new();

            // We need to refresh the s_tokens every time or they will become invalid.
            Dictionary<string, string> s_tokens = new();

            string accountName = storageConfig.StorageAccountName;
            string accountKey = storageConfig.StorageAccountKey;

            StorageSharedKeyCredential storageSharedKeyCredential = new(accountName, accountKey);
            s_containerAddresses = this.storageConfig.GetStorageContainerAddresses();

            for (int i = 0; i < s_containerAddresses.Count; i++)
            {
                BlobContainerClient container = new(new Uri(s_containerAddresses[i]), new StorageSharedKeyCredential(accountName, accountKey));
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
                var sas = policy.ToSasQueryParameters(storageSharedKeyCredential);
                BlobUriBuilder sasUri = new BlobUriBuilder(container.Uri)
                {
                    Sas = sas
                };

                s_tokens.TryAdd(s_containerAddresses[i], "?" + sasUri.Sas.ToString());
            }

            return s_tokens;
        }
    }
}
