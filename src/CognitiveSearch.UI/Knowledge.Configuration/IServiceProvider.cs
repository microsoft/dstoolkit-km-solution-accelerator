// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


namespace Knowledge.Configuration
{
    public interface IServiceProvider
    {
        public bool IsEnabled();

        public string GetName();
    }
}
