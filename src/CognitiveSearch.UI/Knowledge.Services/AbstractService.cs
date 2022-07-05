// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Net.Http;

namespace Knowledge.Services
{
    public abstract class AbstractService
    {
        protected IDistributedCache distCache;

        protected string CachePrefix;

        protected HttpClient httpClient = new();

        protected TelemetryClient telemetryClient;

        protected bool TryGetValue(string key, out string result)
        {
            result = this.distCache.GetString(this.CachePrefix + key);

            return (result == null);
        }
        protected string GetCacheEntry(string key)
        {
            return this.distCache.GetString(this.CachePrefix + key);
        }

        protected void AddCacheEntry(string key, string value, int CacheExpirationTime)
        {
            this.distCache.SetString(this.CachePrefix + key, value, new DistributedCacheEntryOptions()
            {
                SlidingExpiration = TimeSpan.FromMinutes(CacheExpirationTime)
            });
        }
    }
}
