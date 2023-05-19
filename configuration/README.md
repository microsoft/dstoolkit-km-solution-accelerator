![banner](../docs/media/banner.png)

# Configuration 

Before diving into the configuration piece of this solution accelerator, we would recommend to review the deployment & environment concept. 

This folder is where the solution accelerator holds all configuration. The entire Configuration folder is copied over to the solution deployment directory. 

You may add any data relevant to your solution.

1. [Environment](#init-environment)
1. [Configuration](#config)
1. [Monitoring (Optional)](#monitoring)
1. [Unit Testing (Optional)](#unit-testing)

## Environment Initialization

This script is initializing your solution environment by loading the core and vnet powershell modules, load the configuration and parameters. 

Once loaded, you can operate the solution through pre-defined powershell cmdlets. 

# Configuration 

## Pricing configuration 

Contains a list of pricing tiers to help you configure your Azure services pricing.

pricing.json
```json
{
    "pricing.consumption": "Y1",
    "pricing.standard": "S1",
    "pricing.premium": "P1V2",
    "pricing.elastic": "EP1"
}
```

## Services configuration 

The file services.json contains a pre-defined list of services names to deploy. 

**services.json (example)**
```json
{
    "searchServiceName": "{{config.name}}search",
    "cogSvcLanguage": "{{config.name}}coglanguage",
    "cogSvcVision": "{{config.name}}cogvision",
    "cogSvcForm": "{{config.name}}cogform",
    "cogSvcTranslate": "{{config.name}}cogtranslate",
    "appInsightsService": "{{config.name}}insights",
    "techStorageAccountName": "{{config.name}}tech",
    "dataStorageAccountName": "{{config.name}}data",
    "aks": "{{config.name}}aks",
    "acr": "{{config.name}}acr.azurecr.io",
    "acrName": "{{config.name}}acr",
    "maps": "{{config.name}}maps",
    "bing": "{{config.name}}bing",
    "keyvault": "{{config.name}}kv",
    "TikaContainerUrl":"https://{{config.name}}tikaserver.azurewebsites.net"
}
```

## Config folder

Configuration folder is distributed as following:  

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
            "Id":2,
            "Name": "microsoft/vision",
            "Path": "..\\src\\CognitiveSearch.Skills\\Python\\Vision"
        }
    ]
}
```

The images path are relative to the deployment directory. You may use full path when necessary. 

### Functions

Functions configurations file defines the list of function to create, build, deploy and maintain. 

Functions defined in the core configuration are mostly Azure Search custom skills. The core file is as follows: 

```json
{
    "AppPlans": [
        {
            "Id": "skillsplan",
            "Name": "{{config.name}}skillsplan",
            "Sku": "P1V2",
            "ResourceGroup": "{{config.resourceGroupName}}",
            "IsLinux": false,
            "FunctionApps": [
                {
                    "Id": "entitiescleansing",
                    "Name": "{{config.name}}entitiescleansing",
                    "Path": "..\\src\\CognitiveSearch.Skills\\C#\\Entities\\KeyPhrasesCleaner",
                    "Version": 3,
                    "Functions": [
                        {
                            "Name": "content-cleansing"
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
                },
                {
                    "Id": "geolocations",
                    "Name": "{{config.name}}geolocations",
                    "Path": "..\\src\\CognitiveSearch.Skills\\C#\\Geo\\GeoLocations",
                    "Version": 3,
                    "Functions": [
                        {
                            "Name": "locations"
                        }
                    ],
                    "vnetPrivateEndpoint": true,
                    "vnetIntegration": false
                },
                {
                    "Id": "textparagraphs",
                    "Name": "{{config.name}}textparagraphs",
                    "Path": "..\\src\\CognitiveSearch.Skills\\C#\\Text\\Paragraphs",
                    "Version": 3,
                    "Functions": [
                        {
                            "Name": "ParagraphsMerge"
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
            "Sku": "P1V2",
            "ResourceGroup": "{{config.resourceGroupName}}",
            "IsLinux": false,
            "FunctionApps": [
                {
                    "Id": "imgext",
                    "Name": "{{config.name}}imgext",
                    "Path": "..\\src\\CognitiveSearch.Skills\\C#\\Image\\Image.Extraction",
                    "Version": 3,
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
            "Sku": "P1V2",
            "ResourceGroup": "{{config.resourceGroupName}}",
            "IsLinux": false,
            "FunctionApps": [
                {
                    "Id": "mtda",
                    "Name": "{{config.name}}mtda",
                    "Path": "..\\src\\CognitiveSearch.Skills\\C#\\Metadata\\Assignment",
                    "Version": 3,
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
                    "Path": "..\\src\\CognitiveSearch.Skills\\C#\\Metadata\\Extraction",
                    "Version": 3,
                    "Functions": [
                        {
                            "Name": "DurableMetadataExtractionSkill_HttpStart"
                        },
                        {
                            "Name": "MetadataExtractionSkill"
                        }
                    ],
                    "vnetPrivateEndpoint": true,
                    "vnetIntegration": true
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
    "enable": true,
    "GroupId": "WebApps",
    "Apptype": "WebApp",
    "PrivateDNSZone": "privatelink.azurewebsites.net",
    "Parameters" : [],
    "AppPlans": [
        {
            "Name": "{{config.name}}uiplan",
            "Sku": "{{param.pricing.premium}}",
            "ResourceGroup":"{{config.ResourceGroupName}}",
            "IsLinux":false,
            "WebApps": [
                {
                    "Id":"webui",
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
                    "Image": "docker.io/puthurr/tika2:2.7.0",
                    "AccessIPRestriction": true,
                    "AccessSubnetRestriction": true,
                    "EnablePrivateAccess": true
                }
            ]
        }
    ]
}
```

### YAML

This folder contains multiple YAML files to help you deploy Azure Functions or Tika in Azure Kubernetes cluster. 

## Monitoring 

You may place here any monitoring scripts or tools you may want to use for monitoring the solution in Azure. 

## Unit Testing (tests)

Automatically deployed in the environment 99-{{config.name}}/tests folder. Contains any file you want to use for unit testing your services & functions endpoints.

The .http files are used with the Client REST API VS Code extension 

