// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Configuration.AzureStorage
{
    public class StorageConfig : AbstractServiceConfig
    {
        // Storage Accounts settings for SAS Token handling
        public string StorageAccountName { get; set; }

        public string StorageAccountKey { get; set; }

        public string StorageContainerAddressesAsString { get; set; }

        private List<string> StorageContainerAddresses;

        public string StorageConnectionString { get; set; }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        public List<string> GetStorageContainerAddresses()
        {
            if (StorageContainerAddresses == null)
            {
                StorageContainerAddresses = new List<string>(StorageContainerAddressesAsString.Split(','));
            }

            return StorageContainerAddresses;
        }
    }
}
