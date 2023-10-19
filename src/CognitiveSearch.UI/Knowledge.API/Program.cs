// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Identity;
using Microsoft.AspNetCore;

namespace Knowledge.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .ConfigureAppConfiguration((context, config) =>
                    {
                        var root = config.Build();
                        config.AddAzureKeyVault(new Uri($"https://{root["KeyVault"]}.vault.azure.net/"), new DefaultAzureCredential());
                    })
                    .UseStartup<Startup>();
    }
}
