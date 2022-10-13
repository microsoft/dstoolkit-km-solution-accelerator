![banner](../media/banner.png)

# Configuration 

Before diving into the configuration piece of this solution accelerator, we would recommend to review the deployment & enviroment concept. 

This folder is where the solution accelerator holds all core configuration. The entire Configuration folder is copied over to the solution deployment directory. 

You may add any data relevant to your solution.

1. [Environment (Core)](#init-environment)
1. [Parameters (Core/Configurable)](#parameters-configuration)
1. [Configuration (Core/Configurable)](#config)
1. [Monitoring (Optional)](#monitoring)
1. [Unit Testing (Optional)](#unit-testing)

## Environment Initialization

This script is initializing your solution environment by loading the core and vnet powershell modules, load the configuration and parameters. 

Once loaded, you can operate the solution through pre-defined powershell cmdlets. 


| Configuration | 99-{{config.name}} | Description | 
|--|--|
|config|config| contains all search, apps configuration | 
|monitoring|monitoring| Any script,tools you want to use for monitoring your solution}
|tests|tests|Any testing file to validate your solution deployment or troubleshoot any function.|
|init_env.ps1|init_env.ps1|Script you will use to load your environment.|
|pricing.json|pricing.json|List the pricing tiers you could leverage in configuration files if needed|
|services.json|services.json|Service configuration file listing all services names you need for deployment|


## Parameters configuration 

The file services.json contains a pre-defined list of services names to deploy. 

```json
{
    "searchServiceName": "{{config.name}}search",
    "apimServiceName": "{{config.name}}apim",
    "webappname": "{{config.name}}app",
    "webappplan": "{{config.name}}appplan",
    "cogServicesName": "{{config.name}}cog",
    "appInsightsName": "{{config.name}}insights",
    "techStorageAccountName": "{{config.name}}tech",
    "dataStorageAccountName": "{{config.name}}data",
    "vnetnsg": "{{config.name}}nsg",
    "appgwip": "{{config.name}}appgwip",
    "appgw": "{{config.name}}appgw",
    "aks": "{{config.name}}aks",
    "acr": "{{config.name}}acr.azurecr.io",
    "acr_prefix": "{{config.name}}acr",
    "maps": "{{config.name}}maps",
    "bing": "{{config.name}}bing",
    "keyvault": "{{config.name}}kv",
    "qnaCogServicesName": "{{config.name}}qna"
}
```

## Config

Configuration is distributed as following:  

- **App Settings** : where Web Apps and Functions application core settings are defined  
- **Docker** : where we defined what docker images to build and publish to Azure Container Registry. This is used for building images you require. 
- **Functions** : where we define what functions to build, configure and deploy. 
- **Search** : where all the necessary core search related configuration files are persisted. 
- **Web Apps** : containing the configuration files to create, build and deploy web applications. 
- **YAML** : for use to deploy workloads in AKS 

### Application Settings 

Contains functions and web applications settings. 

To assign a set of settings to a function or web app, we use a naming standard. 

```
<function or web app id>.json 
```
If we have defined a function with id **entitiescleansing**, then the corresponding settings file would be **entitiescleansing.json**.

The corresponding functions and web apps ids are defined in their respective configuration files.

### Docker 

The **config.json** holds the list of Docker images to build for the solution. 

The built images will be build and published to the solution designated Azure Container Registry ACR. 

```json
{
    "Images": [
        {
            "Id":1,
            "Name": "microsoft/tikaserver",
            "Path": "..\\src\\CognitiveSearch.Skills\\Java\\ApacheTika"
        }
    ]
}
```

The images path are relative to the deployment directory. You may use full path when necessary. 

### Azure Functions

Functions configurations file defines the list of function to create, build, deploy and maintain. 

Functions defined in the core configuration are mostly Azure Search custom skills. The core file is as follows: 

```json
{
    "AppPlans": [
        {
            "Id": "skillsplan",
            "Name": "{{config.name}}skillsplan",
            "Sku": "{{param.pricing.premium}}",
            "ResourceGroup": "{{config.resourceGroupName}}",
            "IsLinux": false,
            "FunctionApps": [
                {
                    "Id": "geolocations",
                    "Name": "{{config.name}}geolocations",
                    "Path": "src\\CognitiveSearch.Skills\\C#\\Geo\\GeoLocations",
                    "Version": 4,
                    "Functions": [
                        {
                            "Name": "locations"
                        }
                    ],
                    "vnetPrivateEndpoint": true,
                    "vnetIntegration": false
                },
                {
                    "Id": "text",
                    "Name": "{{config.name}}text",
                    "Path": "src\\CognitiveSearch.Skills\\C#\\Text.Function",
                    "Version": 4,
                    "Functions": [
                        {
                            "Name": "TextMesh"
                        },
                        {
                            "Name": "TextMerge"
                        },
                        {
                            "Name": "TranslationMerge"
                        },
                        {
                            "Name": "HtmlConversion"
                        }
                    ],
                    "vnetPrivateEndpoint": true,
                    "vnetIntegration": false
                },
                {
                    "Id": "entities",
                    "Name": "{{config.name}}entities",
                    "Path": "src\\CognitiveSearch.Skills\\C#\\Entities.Function",
                    "Version": 4,
                    "Functions": [
                        {
                            "Name": "concatenation"
                        },
                        {
                            "Name": "deduplication"
                        },
                        {
                            "Name": "keyphrases-cleansing"
                        }
                    ],
                    "vnetPrivateEndpoint": true,
                    "vnetIntegration": false
                }
            ]
        },
        {
            "Id": "imageplan",
            "Name": "{{config.name}}imageplan",
            "Sku": "{{param.pricing.premium}}",
            "ResourceGroup": "{{config.resourceGroupName}}",
            "IsLinux": false,
            "FunctionApps": [
                {
                    "Id": "imgext",
                    "Name": "{{config.name}}imgext",
                    "Path": "src\\CognitiveSearch.Skills\\C#\\Image\\Image.Extraction",
                    "Version": 4,
                    "Functions": [
                        {
                            "Name": "DurableImageExtractionSkill_HttpStart"
                        }
                    ],
                    "vnetPrivateEndpoint": true,
                    "vnetIntegration": true
                }
            ]
        },
        {
            "Id": "metadataplan",
            "Name": "{{config.name}}metadataplan",
            "Sku": "{{param.pricing.premium}}",
            "ResourceGroup": "{{config.resourceGroupName}}",
            "IsLinux": false,
            "FunctionApps": [
                {
                    "Id": "mtda",
                    "Name": "{{config.name}}mtda",
                    "Path": "src\\CognitiveSearch.Skills\\C#\\Metadata\\Assignment",
                    "Version": 4,
                    "Functions": [
                        {
                            "Name": "Assign"
                        }
                    ],
                    "vnetPrivateEndpoint": true,
                    "vnetIntegration": false
                },
                {
                    "Id": "mtdext",
                    "Name": "{{config.name}}mtdext",
                    "Path": "src\\CognitiveSearch.Skills\\C#\\Metadata\\Extraction",
                    "Version": 4,
                    "Functions": [
                        {
                            "Name": "MetadataExtractionSkill"
                        }
                    ],
                    "vnetPrivateEndpoint": true,
                    "vnetIntegration": true
                }
            ]
        },
        {
            "Id": "visionplan",
            "Name": "{{config.name}}visionplan",
            "Sku": "{{param.pricing.premium}}",
            "ResourceGroup": "{{config.resourceGroupName}}",
            "IsLinux": true,
            "FunctionApps": [
                {
                    "Id": "vision",
                    "Name": "{{config.name}}vision",
                    "Path": "src\\CognitiveSearch.Skills\\Python\\Vision",
                    "Version": 4,
                    "PythonVersion": 3.9,
                    "Functions": [
                        {
                            "Name": "Analyze"
                        },
                        {
                            "Name": "AnalyzeDomain"
                        },
                        {
                            "Name": "azureocrlayout"
                        },
                        {
                            "Name": "Describe"
                        },
                        {
                            "Name": "AnalyzeDocument"
                        },
                        {
                            "Name": "Normalize"
                        },
                        {
                            "Name": "Read"
                        }
                    ],
                    "vnetPrivateEndpoint": true,
                    "vnetIntegration": false
                }
            ]
        },
        {
            "Id": "languageplan",
            "Name": "{{config.name}}languageplan",
            "Sku": "{{param.pricing.premium}}",
            "ResourceGroup": "{{config.resourceGroupName}}",
            "IsLinux": true,
            "FunctionApps": [
                {
                    "Id": "language",
                    "Name": "{{config.name}}language",
                    "Version": 4,
                    "PythonVersion": 3.9,
                    "Path": "src\\CognitiveSearch.Skills\\Python\\Language",
                    "Functions": [
                        {
                            "Name": "EntityLinking"
                        },
                        {
                            "Name": "EntityRecognition"
                        },
                        {
                            "Name": "KeyPhrasesExtraction"
                        },
                        {
                            "Name": "LanguageDetection"
                        },
                        {
                            "Name": "Summarization"
                        },
                        {
                            "Name": "Translator"
                        }
                    ],
                    "vnetPrivateEndpoint": true,
                    "vnetIntegration": false
                }
            ]
        }
    ]
}
```
The configuration file supports Windows or Linux App Plans, Sku or Vnet configuration. 


### Search 

Contains the Azure Cognitive Search configuration files. 

The search configuration is as follows

A single index 

| Name | Description |
|---|---|
| aliases | Aliases definition per index. |
| datasources | Datasource definition| 
| indexers | Datasource definition. By default, we use a documents & images (those extracted) sources.| 
| indexes | Index definition(s). By default we use a single index for documents and images.| 
| skillsets | By default, 2 skillsets are defined : one for document and one for images.| 
| synonyms | Synonyms maps you might want to use in your index fields. Semantic Search is not compatible with synonyms.| 

The search configuration is such that you can add your datasources, indexers and indexes as you see fit. 

**NOTE** Our solution accelerator supports multiple indexes in the backend. 

Add a new index definition in the indexes folder, initialize your environment and run

```ps
Initialize-Search
```
This cmdlet will re-configure your ACS service. 

Check the parameter $params.searchIndexes as it should hold a list of your indexes. 

Publish the new appsettings 
```ps
Publish-WebAppsSettings -WindowsOnly
```
The flag is to target the UI Web Application only. Tika is using a Linux-based Web Applications. 

### Web Apps

The **config.json** holds the list of Web Applications to build for the solution. 

```json
{
    "AppPlans": [
        {
            "Name": "{{config.name}}uiplan",
            "Sku": "{{param.pricing.premium}}",
            "ResourceGroup":"{{config.ResourceGroupName}}",
            "IsLinux":false,
            "WebApps": [
                {
                    "Id":"webappui",
                    "Name": "{{config.name}}ui",
                    "Path": "src\\CognitiveSearch.UI\\CognitiveSearch.UI",
                    "vnetPrivateEndpoint":false,
                    "vnetIntegration":true,
                    "slots":[
                        "staging"
                    ]
                }
            ]
        },
        {
            "Name": "{{config.name}}tikaplan",
            "Sku": "{{param.pricing.premium}}",
            "ResourceGroup":"{{config.ResourceGroupName}}",
            "IsLinux":true,
            "WebApps": [
                {
                    "Id":"tikaserver",
                    "Name": "{{config.name}}tikaserver",
                    "Image": "microsoft/tikaserver:latest",
                    "vnetPrivateEndpoint":true,
                    "vnetIntegration":true
                }
            ]
        }
    ]
}
```

### YAML

This folder contains multiple YAML files to help you deploy Azure Functions or Tika in Azure Kubernetes cluster. 

# About .http file extension

We use a Visual Code extension named [REST Client by Huaochao Mao](https://marketplace.visualstudio.com/items?itemName=humao.rest-client).

We found this extension very useful to stay within VS Code and quickly test a local function or search management endpoints. 

You may want to use more familiar like Postman. 

# Monitoring 

You may place here any monitoring scripts or tools you may want to use for monitoring the solution in Azure. 

By placing any script/tooling here, you wil benefit from the templates framework. 

As an example of a .http located in the configuration/monitoring/search folder

```http
### Services Stats
GET https://{{param.searchServiceName}}.search.windows.net/servicestats?api-version={{param.searchVersion}}
content-type: application/json;charset=utf-8
api-key: {{param.searchServiceKey}}
```

Upon initializing the environment, a converted file will be available holding the correct url, version and key. 

99-{{config.name}}/monitoring/search

```http
### Services Stats
GET https://XXXXXXXXX.search.windows.net/servicestats?api-version=2021-04-30-Preview
content-type: application/json;charset=utf-8
api-key: XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

```

Using the 

# Unit Testing (tests)

Automatically deployed in the environment 99-{{config.name}}/tests folder.


