// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
using Knowledge.Services.Chat.FunctionChat;
using Knowledge.Services.Chat.PromptFlow;
using Knowledge.Services.Graph.Facet;
using Knowledge.Services.Metadata;
using Knowledge.Services.SemanticSearch;
using Knowledge.Services.Translation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

        public void ConfigureServices(IServiceCollection services)
        {
            var azureAd = new AzureAdConfig();
            Configuration.GetSection("AzureAd").Bind(azureAd);

            if (true/*!env.IsDevelopment()*/)
            {
                //services.AddMicrosoftIdentityWebAppAuthentication(Configuration);

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));

                services.AddAuthorization(x =>
                {
                    x.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                });
            }
            else
            {
                services.AddAuthorization(x =>
                {
                    x.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAssertion(_ => true)
                        .Build();
                });
            }

            services.AddControllers().AddNewtonsoftJson(jsonOptions =>
                        {
                            jsonOptions.SerializerSettings.Converters.Add(new StringEnumConverter());
                            jsonOptions.SerializerSettings.ContractResolver = new DefaultContractResolver
                            {
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
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Description = "OAuth2.0 Auth Code with PKCE",
                    Name = "oauth2",
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{azureAd.Instance}{azureAd.TenantId}/oauth2/v2.0/authorize"),
                            TokenUrl = new Uri($"{azureAd.Instance}{azureAd.TenantId}/oauth2/v2.0/token"), 
                            Scopes = azureAd.Scopes?.ToDictionary(x => x, x => x)
                        }
                    }
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            azureAd.Scopes
        }
    });
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

            ChatConfig chatConfigData = Configuration.GetSection("ChatConfig").Get<ChatConfig>();
            services.AddSingleton<ChatConfig>(_ => chatConfigData);

            OpenAIConfig oaiConfigData = Configuration.GetSection("OpenAIConfig").Get<OpenAIConfig>();
            services.AddSingleton<OpenAIConfig>(_ => oaiConfigData);

            PromptFlowConfig promptFlowConfigData = Configuration.GetSection("PromptFlowConfig").Get<PromptFlowConfig>();
            services.AddSingleton<PromptFlowConfig>(_ => promptFlowConfigData);

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
            services.AddSingleton<ITranslationService, TranslationService>();
            services.AddSingleton<IChatService, ChatService>();
            services.AddSingleton<IChatHistoryService, ChatHistoryService>();
            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<ISemanticSearchService, SemanticSearch>();
            services.AddSingleton<IAzureSearchService, AzureSearchService>();
            services.AddSingleton<IAzureSearchSDKService, AzureSearchSDKService>();
            services.AddSingleton<IFacetGraphService, FacetGraphService>();
            services.AddSingleton<IFunctionChatService, FunctionChatService>();
            services.AddSingleton<IPromptFlowChatService, PromptFlowChatService>();

            services.AddSingleton<IQueryService, QueryService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // if (env.IsDevelopment())
            // {
            app.UseDeveloperExceptionPage();
            var clientid = Configuration.GetSection("AzureAd:ClientId").Value;
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "KnowledgeAPI v1");
                c.OAuthClientId(clientid);
                c.OAuthUsePkce();
                c.OAuthScopeSeparator(" ");
            });
            //}

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
