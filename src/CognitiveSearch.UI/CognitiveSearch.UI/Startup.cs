// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using Knowledge.Configuration;
using Knowledge.Configuration.AzureStorage;
using Knowledge.Configuration.Graph;
using Knowledge.Configuration.Maps;
using Knowledge.Configuration.SemanticSearch;
using Knowledge.Configuration.SpellChecking;
using Knowledge.Configuration.Translation;
using Knowledge.Configuration.WebSearch;
using Knowledge.Services.AzureSearch.SDK;
using Knowledge.Services.AzureStorage;
using Knowledge.Services.Graph.Facet;
using Knowledge.Services.Metadata;
using Knowledge.Services.QnA;
using Knowledge.Services.SemanticSearch;
using Knowledge.Services.SpellChecking;
using Knowledge.Services.Translation;
using Knowledge.Services.WebSearch;
using Knowledge.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.IO;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace CognitiveSearch.UI
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
            }

            //
            // Cache Service - Memory-based
            //
            services.AddDistributedMemoryCache();

            // The following line enables Application Insights telemetry collection.
            services.AddApplicationInsightsTelemetry();


            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
           
            // Organisation Config
            var orgConfig = Configuration.GetSection("OrganizationConfig").Get<OrganizationConfig>();
            services.AddSingleton(orgConfig);

            // UI Config
            var uiConfig = Configuration.GetSection("UIConfig").Get<UIConfig>();
            services.AddSingleton(uiConfig);

            // Microsoft Clarity Support
            ClarityConfig clarityConfig = Configuration.GetSection("ClarityConfig").Get<ClarityConfig>();
            services.AddSingleton<ClarityConfig>(_ => clarityConfig);

            MapConfig mapConfigData = Configuration.GetSection("MapConfig").Get<MapConfig>();
            services.AddSingleton<MapConfig>(_ => mapConfigData);

            WebSearchConfig wsconfigData = Configuration.GetSection("WebSearchConfig").Get<WebSearchConfig>();
            services.AddSingleton<WebSearchConfig>(_ => wsconfigData);

            GraphConfig gconfigData = Configuration.GetSection("GraphConfig").Get<GraphConfig>();
            services.AddSingleton<GraphConfig>(_ => gconfigData);

            WebAPIBackend webapiconfigData = Configuration.GetSection("WebAPIBackend").Get<WebAPIBackend>();
            services.AddSingleton<WebAPIBackend>(_ => webapiconfigData);


            // Global Configuration singleton 
            var appConfig = new AppConfig
            {
                Organization = orgConfig,
                Clarity = clarityConfig,
                UIConfig = uiConfig,
                MapConfig = mapConfigData,
                GraphConfig = gconfigData,
                WebSearchConfig = wsconfigData,
                WebAPIBackend = webapiconfigData,
                UIVersion = Configuration.GetValue("UIVersion", "1.0.0")
            };
            services.AddSingleton(appConfig);


            // Activate API backend services
            if (! webapiconfigData.IsEnabled)
            {
                ActivateBackendServices(services);
            }
            else
            {
                QueryServiceConfig queryconfigData = Configuration.GetSection("QueryServiceConfig").Get<QueryServiceConfig>();
                services.AddSingleton<QueryServiceConfig>(_ => queryconfigData);

                appConfig.QueryServiceConfig = queryconfigData; 

                // Semantic 
                SemanticSearchConfig semanticData = Configuration.GetSection("SemanticSearchConfig").Get<SemanticSearchConfig>();
                services.AddSingleton<SemanticSearchConfig>(_ => semanticData);

                TranslationConfig tconfigData = Configuration.GetSection("TranslationConfig").Get<TranslationConfig>();
                services.AddSingleton<TranslationConfig>(_ => tconfigData);

                SpellCheckingConfig scconfigData = Configuration.GetSection("SpellCheckConfig").Get<SpellCheckingConfig>();
                services.AddSingleton<SpellCheckingConfig>(_ => scconfigData);

            }

            services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

            services.AddMvc(options => options.EnableEndpointRouting = false).AddNewtonsoftJson(jsonOptions =>
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

            services.AddWebOptimizer(pipeline =>
            {
                pipeline.AddCssBundle("/css/bundle.css", "css/site.css","css/colors.css");
                pipeline.AddJavaScriptBundle("/js/bundle.js", "js/config.js", "js/site.js","js/utils.js","js/common.js", "js/commons/*.js", "js/graph/*.js", "js/details/*.js", "js/details.js","js/views/*.js","js/export.js");
            });
        }



        public void ActivateBackendServices(IServiceCollection services)
        {
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
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            //app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHttpsRedirection();

            app.UseWebOptimizer();

            // app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    const int durationInSeconds = 60 * 60 * 24;
                    context.Context.Response.Headers[HeaderNames.CacheControl] =
                        "public,max-age=" + durationInSeconds;
                }
            });

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add(HeaderNames.XContentTypeOptions, "nosniff");
                await next();
            });

            app.UseCookiePolicy();

            app.UseMvcWithDefaultRoute();
        }
    }
}
