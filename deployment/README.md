# Deployment

This knowledge mining solution accelerator deployment is done through the execution of PowerShell scripts. 

_Deployment Steps_

1. [Install Pre-Requisites](#1-install-pre-requisites)
1. [Create a new environment configuration](#2-create-a-new-environment-configuration)
1. [Environment initialization](#3-initialize-your-environment)
1. [Deploy Azure core Services](#4-deploy-azure-core-services)
1. [Deploy Azure Functions](#5-deploy-azure-functions)
1. [Deploy Azure Web Applications](#6-deploy-azure-web-applications)
1. [Initialize your Azure Cognitive Search](#7-initialize-azure-cognitive-search)
1. [Restart the solution](#8-restart-the-solution)
1. [Validate the solution deployment](#9-validate-the-solution-deployment)

**An environment represents a single deployment of the solution accelerator.** 

## 1 Install Pre-Requisites 

To deploy & publish the solution accelerator from a local or remote environment, you would need to install the following technologies: 
- [.NET Core 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [PowerShell Core 7](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.1)
- [AZ CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Azure Functions Core tools v4.x](https://github.com/Azure/azure-functions-core-tools)

### Azure requirement

Ensure you have access to an Azure subscription ideally with Contributor access. The minimum Azure requirement would be a Contributor permission on a defined Resource Group. 

Some resources providers are tested against your subscription when starting the deployment. 

__Required__ 
- "microsoft.cognitiveservices"
- "microsoft.insights"
- "microsoft.search"
- "microsoft.storage"
- "Microsoft.KeyVault"

__Optional__
- "microsoft.maps"
- "microsoft.bing"

If you have no permissions on the subscription you're planning to use, ask your Azure Admin(s) to enable the listed resource providers to get full experience of our solution accelerator.

## 2 Create a new environment configuration

Deployment configurations shall be located under the __deployment/config__ folder. 

The below **contoso.json** environment configuration file you can easily adjust to your Azure context.

```json
{
    "id":"contoso",
    "name":"kmcontoso",
    "cloud":"AzureCloud",
    "overlayPath":"<YOUR OVERLAY DIRECTORY IF RELEVANT>",
    "domain":"<YOUR ORG DOMAIN>",
    "tenantId":"<YOUR TENANT ID>",
    "clientId":"00000000-0000-0000-0000-000000000000",
    "subscriptionId": "<YOUR SUBSCRIPTION ID>",
    "organisation": "Contoso Ltd.",
    "organisationWebsite": "https://www.contoso.com/",
    "organisationLogoUrl": "~/images/logos/microsoft-logo.png",
    "organisationLogoProviderUrl":"~/images/logos/provider-logo.png",
    "adminUser": "admin@contoso.com",
    "resourceGroupName": "kmcontoso-rg",
    "searchDefaultFromLanguageCode": "en",
    "searchDefaultToLanguageCode": "en",
    "spellCheckEnabled":false,
    "spellCheckProvider":"Bing",
    "webSearchEnabled": false,
    "webMarket": "en-uk",
    "mapSearchEnabled": false,
    "location": "YOUR AZURE LOCATION",
    "searchSku": "Standard",
    "searchVersion": "2021-04-30-Preview",
    "searchManagementVersion": "2021-04-30-Preview",
    "semanticSearchEnabled":false,
    "storageContainers":["documents", "images", "metadata"],
    "searchIndexerEnvironment":"standard",
    "X_Tika_PDFAllPagesAsImages":true,
    "vnetEnable":false
}

```
Below the list of most common entries your configuration file should contains:

| Key | Description|
| ------------- | ----------- |
|id | Unique id of your environment  |
|name   | Environment name. It is used as prefix to all Azure services deployed as part of this solution.  |
|cloud | Azure Cloud name. |
|overlayPath | Path where to find the environment specific configuration |
|domain | Your Azure AD organization domain name |
|tenantId | Your Azure AD tenant id |
|clientId | Your Azure AD Enterprise Application used to secure the UI App service.|
|subscriptionId | Target Subscription Id |
|organisation|Name of your customer |
|organisationWebsite| URL of your organization internet or intranet site|
|organisationLogoUrl| Relative or Absolute URL of your solution logo|
|organisationLogoProviderUrl|Relative or Absolute URL of your organization logo|
|adminUser|(Optional)|
|resourceGroupName|Resource Group the KM solution would be deployed|
|searchDefaultFromLanguageCode|Default FROM language code for translation|
|searchDefaultToLanguageCode|Default TO language code for translation|
|spellCheckEnabled|Boolean indicating if your solution supports a spellChecking service.|
|spellCheckProvider|String indicating which SpellChecking service you will use. By deafult we support "Bing". You may add your own.|
|webSearchEnabled|Boolean indicating if your solution will use Bing as Web Search service.|
|webMarket|When Web Search is enabled, indicates which market to use for querying.|
|mapSearchEnabled|Boolean indicating if your solution will use Azure Maps.|
|location|Azure location to deploy the services to.|
|searchSku| Search Sku. Please refer to |
|searchVersion| Search Version for query and index|
|searchManagementVersion| Search Management version|
|semanticSearchEnabled|Azure Cognitive Search (ACS) Semantic flag. Semantic Search is an option in ACS.|
|storageContainers| list of storage containers to create in the target data storage.|
|searchIndexerEnvironment| Search Indexer Environment setting. This is use for VNET support in ACS.|
|X_Tika_PDFAllPagesAsImages| Flag for Tika processing. Set to true indicates that every page of aPDF document will be converted into an Image. Set to false, means embedded images of a PDF will be extracted.|
|vnetEnable|Flag to indicate if the solution requires VNET integration.|

## 3 Initialize your environment

This script is initializing your solution environment by loading a core powershell module, sources the configuration and parameters. 

Once loaded, you can operate the solution through pre-defined powershell cmdlets. 

### Connect to Azure using the az login command

- Interactive login
```ps
az login
```
- Device login 
```ps
az login --use-device
```
### Go to the deployment folder 
```ps
cd .\deployment\
```
### Initialize the environment
```ps
.\init_env.ps1 -Name contoso
```

**NOTE** The command assumes there is a contoso.json configuration file under the deployment config path. 

To re-generate an environment after any configuration change, execute the same 

At the end of this **init_env** script, your Azure cloud and account will be set according to your configuration

```ps
# Set the Azure Cloud environment
az cloud set -n $config.cloud

# Set the Azure Account Subscription id
az account set -s $config.subscriptionId
```

The environment configuration will be available as a PowerShell variable $config

```ps
PS> $config

id                            : contoso
name                          : kmcontoso
cloud                         : AzureCloud
overlayPath                   : <YOUR OVERLAY DIRECTORY IF RELEVANT>
domain                        : <YOUR ORG DOMAIN>
tenantId                      : <YOUR TENANT ID>
clientId                      : 00000000-0000-0000-0000-000000000000
subscriptionId                : <YOUR SUBSCRIPTION ID>
organisation                  : Contoso Ltd.
organisationWebsite           : https://www.contoso.com/
organisationLogoUrl           : ~/images/logos/microsoft-logo.png
organisationLogoProviderUrl   : ~/images/logos/provider-logo.png
adminUser                     : admin@contoso.com
resourceGroupName             : kmcontoso-rg
searchDefaultFromLanguageCode : en
searchDefaultToLanguageCode   : en
spellCheckEnabled             : False
webSearchEnabled              : False
webMarket                     : en-uk
mapSearchEnabled              : False
location                      : YOUR AZURE LOCATION
searchSku                     : Standard
searchVersion                 : 2021-04-30-Preview
searchManagementVersion       : 2021-04-30-Preview
semanticSearchEnabled         : False
storageContainers             : {documents, images, metadata}
searchIndexerEnvironment      : standard
X_Tika_PDFAllPagesAsImages    : True
vnetEnable                    : False

```

A default variable $params will also be available 

```ps
PS> $params
pricing.consumption               : Y1
pricing.standard                  : S1
pricing.premium                   : P1V2
pricing.elastic                   : EP1
searchServiceName                 : kmcontososearch
cogSvcLanguage                : kmcontosocoglanguage
cogSvcVision                  : kmcontosocogvision
cogSvcForm                    : kmcontosocogform
cogSvcTranslate               : kmcontosocogtranslate
appInsightsService                : kmcontosoinsights
techStorageAccountName            : kmcontosotech
dataStorageAccountName            : kmcontosodata
vnetnsg                           : kmcontosonsg
aks                               : kmcontosoaks
acr                               : kmcontosoacr.azurecr.io
acr_prefix                        : kmcontosoacr
maps                              : kmcontosomaps
bing                              : kmcontosobing
keyvault                          : kmcontosokv
TikaContainerUrl                  : https://kmcontosotikaserver.azurewebsites.net
dataStorageContainerName          : documents
StorageContainerAddressesAsString : https://kmcontosodata.blob.core.windows.net/documents,https://kmcontosodata.blob.core.w 
                                    indows.net/images,https://kmcontosodata.blob.core.windows.net/metadata
synonymsSynonymMap                : kmcontoso-synonyms
searchSynonymMaps                 : {kmcontoso-synonyms}
documentsSkillSet                 : kmcontoso-documents
imagesSkillSet                    : kmcontoso-images
searchSkillSets                   : {kmcontoso-documents, kmcontoso-images}
indexName                         : kmcontoso-index
searchIndexes                     : kmcontoso-index
searchIndexesList                 : {kmcontoso-index}
documentsDataSource               : kmcontoso-documents
documentsStorageContainerName     : documents
imagesDataSource                  : kmcontoso-images
imagesStorageContainerName        : images
searchDataSources                 : {kmcontoso-documents, kmcontoso-images}
docimgIndexer                     : kmcontoso-docimg
documentsIndexer                  : kmcontoso-documents
imagesIndexer                     : kmcontoso-images
searchIndexers                    : kmcontoso-docimg,kmcontoso-documents,kmcontoso-images
searchIndexersList                : {kmcontoso-docimg, kmcontoso-documents, kmcontoso-images}
```

You will see a default list of services names, search related parameters mostly prefixed with your environment name. 

Some Azure services have names length restrictions. Refer to our [documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules) with that respect. 

The $params variable is persisted in the parameters.json file located in your environement prefixed by 99-. 

Service keys and connection strings are also stored in the $params in plain text to be used in deployment scripts but stored as secure string (PowerShell) in the parameters.json. 

__Note__ Any parameter you set ending with the suffix **Key** or **ConnectionString** is automatically considered a secure string and added to your KeyVault during deployment. 

Secure String in PowerShell implies the encrypted keys can't be shared across platforms. This provides another level of protection than storing the keys in plain text in json file.

If you are loading your environment cross-platform To load all services keys back to the $params variable (and parameters.json) you may want to execute the below commands. 

```ps
Get-AllServicesKeys
```
The above can be generalized to avoid service keys and connection strings to be even stored in the parameters.json.

## 4 Deploy Azure core Services

```ps
.\scripts\10_Deploy_Services.ps1
```
All services keys will be added to the keyvault at that stage. All App settings will refer to the keys via a Keyvault link.

**Note** Azure Cognitive Responsible AI 
```
Notice
I certify that use of this service is not by or for a police department in the United States.
(ResourceKindRequireAcceptTerms) This subscription cannot create CognitiveServices until you agree to Responsible AI terms for this resource. You can agree to Responsible AI terms by creating a resource through the Azure Portal then trying again. For more detail go to https://aka.ms/csrainotice
```

**Note** 

Some services like **Bing** have no support for az cli deployment so far.

As a result, if Bing is a requirement for your deployment, you will be asked to provide manually their corresponding key. 

| Services | Configuration parameter | Impact |
| ------------- | ----------- | ----------- | 
| Bing | webSearchEnabled or (config.spellCheckEnabled and config.spellCheckProvider is set to "Bing") | A value of true in your config will ask you to manually input the Bing Search Key while deploying |

## 5 Deploy Azure Functions

The default deployment method for Azure Functions is using Azure Function core Tools and az cli. 

### 5.1 - Deploy all functions

```ps
.\scripts\20_Deploy_Functions.ps1
```

### 5.2 - Test functions deployment

This step would validate the deployment of all functions by sending an empty record. Each skill-hosting function should respond with a 200 response. 

```ps
Test-Functions
```

**Note** Linux-based Azure Functions first publication process relies on a remote build. It is not uncommon to receive a timeout.  

Timed out waiting for SCM to update the Environment Settings

In that case simply re-run the script with a NoProvision switch as shown below. 
```ps
.\scripts\20_Deploy_Functions.ps1 -NoProvision
```
The command will build and publish all Azure functions again. 

## 6 Deploy Azure Web Applications 

```ps
.\scripts\30_Deploy_WebApps.ps1
```

**Note** Secure your UI Web Application 

Our solution accelerator User Interface authentication supports the built-in Azure authentication and authorization layer also known as EasyAuth

https://docs.microsoft.com/en-us/azure/app-service/configure-authentication-provider-aad

We provide additional details on authentication [here](../configuration/config/Authentication.md).

## 7 Initialize Azure Cognitive Search 

```ps
.\scripts\40_Initialize_Search.ps1
```
The script will update or create all Cognitive Search resources like datasources, indexers, index, skillsset and synonyms. 

If you are running the script against existing resources they will get updated. Only restriction is removed fields from an index will not possible. On an index update, only adding fields is supported. 

Refer to our [index update documentation](https://docs.microsoft.com/en-us/rest/api/searchservice/update-index) for more details. 


## 8 Restart the solution 

```ps
.\scripts\50_Restart_Solution.ps1
```

## 9 Validate the solution deployment 

To start validating your solution, I would first (re-)test all the deployed functions by issuing the below command

```ps
Test-Functions
```

Since Azure Functions are keys to documents and images processing, you expect each function test to return "Function is OK!". 

Example of output 

```ps
Testing Plan  kmskillsplan
Testing App  kmgeolocations
Testing Function  locations
https://kmgeolocations.azurewebsites.net/geo/locations?code=xxx
Function is OK!
Testing App  kmtext
Testing Function  TextMesh
https://kmtext.azurewebsites.net/textext/textmesh?code=xxx
Function is OK!
Testing Function  TextMerge
https://kmtext.azurewebsites.net/textext/textmerge?code=xxx
Function is OK!
Testing Function  TranslationMerge
https://kmtext.azurewebsites.net/textext/translationmerge?code=xxx
Function is OK!
Testing Function  HtmlConversion
https://kmtext.azurewebsites.net/textext/htmlconversion?code=xxx
Function is OK!
```
### Upload your data in the documents container

```ps
Push-Data -container documents
```
### Reset & Start your indexers 

```ps
Reset-SearchIndexer
Start-SearchIndexer documents
Start-SearchIndexer docimg
```
Check the status of your indexers. Investigate indexing failures if any. 

```ps
Get-SearchIndexersStatus
```

Upon __documents__ indexing completion, you can start the indexation of the extracted images. 

```ps
Start-SearchIndexer images
```

### User Interface 

Go to the UI Web application https://{{config.name}}ui.azurewebsites.net 

- Validate that you can see live news & access to the search verticals. 

- Issue few search queries to confirm the indexation of your data.
