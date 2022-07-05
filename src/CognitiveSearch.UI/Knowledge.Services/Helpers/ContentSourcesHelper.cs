// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Knowledge.Services.Helpers
{
    public class ContentSourcesHelper
    {
        public static string GenerateFilter(string[] sources)
        {
            string filter = String.Empty;

            //https://docs.microsoft.com/en-us/azure/search/search-filters#filter-usage-patterns
            if (sources != null && sources.Length > 0)
            {
                foreach (var source in sources)
                {
                    if (String.IsNullOrEmpty(filter))
                    {
                        filter += $"(content_source eq '{source}')";
                    }
                    else
                    {
                        filter += $" or (content_source eq '{source}')";
                    }
                }
            }
            return filter;
        }
    }
}
