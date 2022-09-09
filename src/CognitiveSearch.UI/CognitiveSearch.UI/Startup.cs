// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using Knowledge.Configuration;
using Knowledge.Configuration.Graph;
using Knowledge.Configuration.Maps;
using Knowledge.Configuration.SemanticSearch;
using Knowledge.Configuration.SpellChecking;
using Knowledge.Configuration.Translation;
using Knowledge.Configuration.WebSearch;
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

            QueryServiceConfig queryconfigData = Configuration.GetSection("QueryServiceConfig").Get<QueryServiceConfig>();
            services.AddSingleton<QueryServiceConfig>(_ => queryconfigData);

            // Semantic 
            SemanticSearchConfig semanticData = Configuration.GetSection("SemanticSearchConfig").Get<SemanticSearchConfig>();
            services.AddSingleton<SemanticSearchConfig>(_ => semanticData);

            TranslationConfig tconfigData = Configuration.GetSection("TranslationConfig").Get<TranslationConfig>();
            services.AddSingleton<TranslationConfig>(_ => tconfigData);

            SpellCheckingConfig scconfigData = Configuration.GetSection("SpellCheckConfig").Get<SpellCheckingConfig>();
            services.AddSingleton<SpellCheckingConfig>(_ => scconfigData);



            services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

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
                QueryServiceConfig = queryconfigData,
                UIVersion = Configuration.GetValue("UIVersion","1.0.0")
            };
            services.AddSingleton(appConfig);

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
                pipeline.AddJavaScriptBundle("/js/bundle.js", "js/config.js", "js/site.js","js/utils.js","js/search-common.js", "js/commons/*.js", "js/graph/*.js", "js/search-details.js","js/views/*.js","js/export.js");
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
