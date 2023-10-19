// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Configuration.AzureStorage
{
    public class StorageConfig : AbstractServiceConfig
    {
        public string StorageAccountName { get; set; }
        
        public string StorageContainers { get; set; }        
    }
}
