﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Configuration;
using Knowledge.Configuration.Graph;
using Knowledge.Configuration.WebSearch;
using Knowledge.Services;
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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;


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

            services.AddControllers();
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

            WebSearchConfig wsconfigData = Configuration.GetSection("WebSearchConfig").Get<WebSearchConfig>();
            services.AddSingleton<WebSearchConfig>(_ => wsconfigData);

            QnAConfig qconfigData = Configuration.GetSection("QnAConfig").Get<QnAConfig>();
            services.AddSingleton<QnAConfig>(_ => qconfigData);

            TranslationConfig tconfigData = Configuration.GetSection("TranslationConfig").Get<TranslationConfig>();
            services.AddSingleton<TranslationConfig>(_ => tconfigData);

            SpellCheckingConfig scconfigData = Configuration.GetSection("SpellCheckConfig").Get<SpellCheckingConfig>();
            services.AddSingleton<SpellCheckingConfig>(_ => scconfigData);

            // Services Singletons
            services.AddSingleton<IStorageService, StorageService>();
            services.AddSingleton<IQnAService, QnAService>();
            services.AddSingleton<ISpellCheckingService, SpellCheckingService>();
            services.AddSingleton<ITranslationService, TranslationService>();
            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<ISemanticSearchService, SemanticSearch>();
            services.AddSingleton<IWebSearchService, WebSearchService>();
            services.AddSingleton<IFacetGraphService, FacetGraphService>();
            services.AddSingleton<IAzureSearchService, AzureSearchSDKService>();
            services.AddSingleton<IAzureSearchSDKService, AzureSearchSDKService>();
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
