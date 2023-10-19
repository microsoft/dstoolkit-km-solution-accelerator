// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.AzureSearch
{
    using Knowledge.Configuration;
    using Knowledge.Configuration.AzureStorage;
    using Knowledge.Configuration.Graph;
    using Knowledge.Configuration.SemanticSearch;
    using Knowledge.Services.AzureSearch.SDK;
    using Knowledge.Services.SemanticSearch;
    using Microsoft.ApplicationInsights;

    public class AzureSearchService : AzureSearchSDKService, IAzureSearchService
    {
        public AzureSearchService(TelemetryClient telemetry, SearchServiceConfig configuration, SemanticSearchConfig semanticCfg, StorageConfig strCfg, ISemanticSearchService semanticSvc, GraphConfig graphConfig) : base(telemetry, configuration, semanticCfg, strCfg, semanticSvc, graphConfig)
        {
            this.telemetryClient = telemetry;
            this.serviceConfig = configuration;
            this.storageConfig = strCfg;

            this.semanticConfig = semanticCfg;
            this.semanticSearch = semanticSvc;

            this.graphConfig = graphConfig;

            InitSearchClients();
        }        

    }
}
