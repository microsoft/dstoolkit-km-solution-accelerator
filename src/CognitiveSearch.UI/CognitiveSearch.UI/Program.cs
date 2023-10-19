// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace CognitiveSearch.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile(
                        "config.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile(
                        "graph.json", optional: false, reloadOnChange: true);
                    var root = config.Build();
                    config.AddAzureKeyVault(new Uri($"https://{root["KeyVault"]}.vault.azure.net/"), new DefaultAzureCredential());
                })
                .UseStartup<Startup>()
                .Build();
    }
}
