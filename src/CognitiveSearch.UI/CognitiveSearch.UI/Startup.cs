// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using Knowledge.Services;
using Knowledge.Services.AzureSearch.SDK;
using Knowledge.Services.AzureStorage;
using Knowledge.Services.Configuration;
using Knowledge.Services.Graph;
using Knowledge.Services.Graph.Facet;
using Knowledge.Services.Maps;
using Knowledge.Services.Metadata;
using Knowledge.Services.QnA;
using Knowledge.Services.SemanticSearch;
using Knowledge.Services.SpellChecking;
using Knowledge.Services.Translation;
using Knowledge.Services.WebSearch;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Net.Http.Headers;
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

            services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

            // Global Configuration singleton 
            var appConfig = new AppConfig
            {
                Organization = orgConfig,
                UIConfig = uiConfig,
                SearchConfig = configData,
                GraphConfig = gconfigData,
                WebSearchConfig = wsconfigData,
                MapConfig = mapConfigData,
                UIVersion = Configuration.GetValue("UIVersion","1.0.0")
            };
            services.AddSingleton(appConfig);

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

            services.AddMvc(options => options.EnableEndpointRouting = false).AddNewtonsoftJson();

            services.AddWebOptimizer(pipeline =>
            {
                pipeline.AddCssBundle("/css/bundle.css", "css/site.css","css/colors.css");
                pipeline.AddJavaScriptBundle("/js/bundle.js", "js/site.js","js/search-common.js", "js/commons/*.js", "js/graph/*.js", "js/search-details.js","js/views/*.js");
            });
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
