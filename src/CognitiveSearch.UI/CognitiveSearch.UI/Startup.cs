// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;

using CognitiveSearch.UI.Configuration;

using Knowledge.Configuration;
using Knowledge.Configuration.Answers;
using Knowledge.Configuration.AzureStorage;
using Knowledge.Configuration.Chat;
using Knowledge.Configuration.Graph;
using Knowledge.Configuration.Maps;
using Knowledge.Configuration.OpenAI;
using Knowledge.Configuration.SemanticSearch;
using Knowledge.Configuration.Translation;
using Knowledge.Services;
using Knowledge.Services.Answers;
using Knowledge.Services.AzureSearch;
using Knowledge.Services.AzureSearch.SDK;
using Knowledge.Services.Chat;
using Knowledge.Services.Graph.Facet;
using Knowledge.Services.Metadata;
using Knowledge.Services.OpenAI;
using Knowledge.Services.SemanticSearch;
using Knowledge.Services.Translation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using WebOptimizer;

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
            if (!env.IsDevelopment())
            {
                if (Configuration.GetValue("Authentication:AzureEasyAuthIntegration", true)) {
                    // Easy Auth Integration
                    services.AddMicrosoftIdentityWebAppAuthentication(Configuration);
                    services.AddAuthorization(x =>
                    {
                        x.DefaultPolicy = new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .Build();
                    });
                }
                else {
                    string client_secret = Configuration.GetValue("AzureAD:ClientSecret", string.Empty);

                    if (! string.IsNullOrEmpty(client_secret)) {
                        // Azure AD Authentication - Token Acquisition
                        services.AddMicrosoftIdentityWebAppAuthentication(this.Configuration).EnableTokenAcquisitionToCallDownstreamApi(new string[] { "user.read" }).AddDistributedTokenCaches();

                        services.AddCors();
                        services.AddHttpClient();
                        services.AddAuthorization(x =>
                        {
                            x.DefaultPolicy = new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .Build();
                        });
                    }
                    else {
                        if (Configuration.GetValue("Authentication:AllowAnonymous", false))
                        {
                            // Anonymous Access for PROD
                            services.AddAuthorization(x =>
                            {
                                x.DefaultPolicy = new AuthorizationPolicyBuilder()
                                    .RequireAssertion(_ => true)
                                    .Build();
                            });
                        }
                    }
                }
            }
            else
            {
                // Anonymous Access for DEV - No Auth and always true Authorize
                services.AddAuthorization(x =>
                {
                    x.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAssertion(_ => true)
                        .Build();
                });
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

            GraphConfig gconfigData = Configuration.GetSection("GraphConfig").Get<GraphConfig>();
            services.AddSingleton<GraphConfig>(_ => gconfigData);

            Neo4jConfig neo4jonfigData = Configuration.GetSection("Neo4jConfig").Get<Neo4jConfig>();
            services.AddSingleton<Neo4jConfig>(_ => neo4jonfigData);

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
                WebAPIBackend = webapiconfigData,
                UIVersion = Configuration.GetValue("UIVersion", "1.0.0")
            };
            services.AddSingleton(appConfig);


            // Activate API backend services
            if (!webapiconfigData.IsEnabled)
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
                pipeline.AddCssBundle("/css/bundle.css", "css/site.css", "css/colors.css", "css/tags.css");

                IAsset jsBundle = pipeline.AddJavaScriptBundle("/js/bundle.js", "js/config.js", "js/site.js", "js/utils.js", "js/common.js", "js/commons/*.js", "js/graph/*.js", "js/details/*.js", "js/details.js", "js/views/*.js");
                //AssetExtensions.ExcludeFiles(jsBundle, "js/commons/actions.js");
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "KnowledgeAPI", Version = "v1" });
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

            ChatConfig chatConfigData = Configuration.GetSection("ChatConfig").Get<ChatConfig>();
            services.AddSingleton<ChatConfig>(_ => chatConfigData);

            OpenAIConfig oaiConfigData = Configuration.GetSection("OpenAIConfig").Get<OpenAIConfig>();
            services.AddSingleton<OpenAIConfig>(_ => oaiConfigData);

            AnswersConfig qconfigData = Configuration.GetSection("AnswersConfig").Get<AnswersConfig>();
            services.AddSingleton<AnswersConfig>(_ => qconfigData);

            TranslationConfig tconfigData = Configuration.GetSection("TranslationConfig").Get<TranslationConfig>();
            services.AddSingleton<TranslationConfig>(_ => tconfigData);

            MapConfig mapConfigData = Configuration.GetSection("MapConfig").Get<MapConfig>();
            services.AddSingleton<MapConfig>(_ => mapConfigData);

            QueryServiceConfig queryconfigData = Configuration.GetSection("QueryServiceConfig").Get<QueryServiceConfig>();
            services.AddSingleton<QueryServiceConfig>(_ => queryconfigData);


            // Services Singletons
            services.AddSingleton<IAnswersService, AnswersService>();
            services.AddSingleton<IChatService, ChatService>();
            services.AddSingleton<IOpenAIService, OpenAIService>();
            services.AddSingleton<ITranslationService, TranslationService>();
            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<ISemanticSearchService, SemanticSearch>();
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
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }


                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KnowledgeAPI v1"));

            app.UseHttpsRedirection();

            //// Make sure you call this before calling app.UseMvc()
            app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true) // allow any origin
                .AllowCredentials()); // allow credentials

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseWebOptimizer();
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
