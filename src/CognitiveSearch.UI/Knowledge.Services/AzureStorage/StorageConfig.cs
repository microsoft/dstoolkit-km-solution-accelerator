// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Knowledge.Services.AzureStorage
{
    public class StorageConfig
    {
        // Storage Accounts settings for SAS Token handling

        public string StorageAccountName { get; set; }

        public string StorageAccountKey { get; set; }

        public string StorageContainerAddressesAsString { get; set; }

        private List<string> StorageContainerAddresses;

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
