// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Configuration
{
    public abstract class AbstractServiceConfig : IServiceProvider
    {
        public bool IsEnabled { get; set; }

        public string? Name { get; set; }

        public string GetName()
        {
            return Name;
        }

        bool IServiceProvider.IsEnabled()
        {
            throw new NotImplementedException();
        }
    }
}
