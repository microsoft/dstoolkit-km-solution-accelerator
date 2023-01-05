// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration;
using Knowledge.Configuration.AzureStorage;
using Knowledge.Configuration.Graph;
using Knowledge.Configuration.Maps;
using Knowledge.Configuration.SemanticSearch;
using Knowledge.Configuration.SpellChecking;
using Knowledge.Configuration.Translation;
using Knowledge.Configuration.WebSearch;
using Knowledge.Services;
using Knowledge.Services.AzureSearch;
using Knowledge.Services.AzureSearch.SDK;
using Knowledge.Services.AzureStorage;
using Knowledge.Services.Graph.Facet;
using Knowledge.Services.Metadata;
using Knowledge.Services.QnA;
using Knowledge.Services.SemanticSearch;
using Knowledge.Services.SpellChecking;
using Knowledge.Services.Translation;
using Knowledge.Services.WebSearch;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Knowledge.API
{
    public class Startup
    {
        private readonly IHostEnvironment env;
        private readonly IConfiguration Configuration;

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            this.Configuration = configuration;
            this.env = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            if ( ! env.IsDevelopment())
            {
                services.AddMicrosoftIdentityWebAppAuthentication(Configuration);

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));
            }

            services.AddControllers().AddNewtonsoftJson(jsonOptions =>
            {
                jsonOptions.SerializerSettings.Converters.Add(new StringEnumConverter());
                jsonOptions.SerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    //NamingStrategy = new CamelCaseNamingStrategy()
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        OverrideSpecifiedNames = false
                    }
                };
                jsonOptions.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.None;
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "KnowledgeAPI", Version = "v1" });
            });

            //
            // Cache Service - Memory-based
            //
            services.AddDistributedMemoryCache();

            // The following line enables Application Insights telemetry collection.
            services.AddApplicationInsightsTelemetry();
         
            StorageConfig sconfigData = Configuration.GetSection("StorageConfig").Get<StorageConfig>();
            services.AddSingleton<StorageConfig>(_ => sconfigData);

            SearchServiceConfig configData = Configuration.GetSection("SearchServiceConfig").Get<SearchServiceConfig>();
            services.AddSingleton<SearchServiceConfig>(_ => configData);

            // Semantic 
            SemanticSearchConfig semanticData = Configuration.GetSection("SemanticSearchConfig").Get<SemanticSearchConfig>();
            services.AddSingleton<SemanticSearchConfig>(_ => semanticData);

            GraphConfig gconfigData = Configuration.GetSection("GraphConfig").Get<GraphConfig>();
            services.AddSingleton<GraphConfig>(_ => gconfigData);

            Neo4jConfig neo4jonfigData = Configuration.GetSection("Neo4jConfig").Get<Neo4jConfig>();
            services.AddSingleton<Neo4jConfig>(_ => neo4jonfigData);

            WebSearchConfig wsconfigData = Configuration.GetSection("WebSearchConfig").Get<WebSearchConfig>();
            services.AddSingleton<WebSearchConfig>(_ => wsconfigData);

            QnAConfig qconfigData = Configuration.GetSection("QnAConfig").Get<QnAConfig>();
            services.AddSingleton<QnAConfig>(_ => qconfigData);

            TranslationConfig tconfigData = Configuration.GetSection("TranslationConfig").Get<TranslationConfig>();
            services.AddSingleton<TranslationConfig>(_ => tconfigData);

            SpellCheckingConfig scconfigData = Configuration.GetSection("SpellCheckConfig").Get<SpellCheckingConfig>();
            services.AddSingleton<SpellCheckingConfig>(_ => scconfigData);

            MapConfig mapConfigData = Configuration.GetSection("MapConfig").Get<MapConfig>();
            services.AddSingleton<MapConfig>(_ => mapConfigData);

            QueryServiceConfig queryconfigData = Configuration.GetSection("QueryServiceConfig").Get<QueryServiceConfig>();
            services.AddSingleton<QueryServiceConfig>(_ => queryconfigData);


            // Services Singletons
            services.AddSingleton<IStorageService, StorageService>();
            services.AddSingleton<IQnAService, QnAService>();
            services.AddSingleton<ISpellCheckingService, SpellCheckingService>();
            services.AddSingleton<ITranslationService, TranslationService>();
            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<ISemanticSearchService, SemanticSearch>();
            services.AddSingleton<IWebSearchService, WebSearchService>();
            services.AddSingleton<IAzureSearchService, AzureSearchService>();

            services.AddSingleton<IAzureSearchSDKService, AzureSearchSDKService>();
            services.AddSingleton<IFacetGraphService, FacetGraphService>();

            services.AddSingleton<IQueryService, QueryService>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KnowledgeAPI v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // CORS policy
            app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true) // allow any origin
                .AllowCredentials()); // allow credentials

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
