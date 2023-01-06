# Overlay

Use an overlay folder to override default configuration or settings. 

You may want to have one overlay directory per environment. 

The entry to specify an overlay path in your environment configuration file. 

```json
    "overlayPath":"<YOUR OVERLAY DIRECTORY IF RELEVANT>",
```

Note: The overlay structure should match the structure of the project **not** the structure of the generated 99-<env> folder. 

# Common overlay scenarii

## WebApp UI 

### Icons & Logos

Identify your environment with your own favicon or logo is crucial.  

- Changing the favicon 
```
myoverlay\src\CognitiveSearch.UI\CognitiveSearch.UI\wwwroot\favicon.ico
```
- Adding your own logo(s) to the overlay as follows

```
myoverlay\src\CognitiveSearch.UI\CognitiveSearch.UI\wwwroot\images\logos\mylogo.png
myoverlay\src\CognitiveSearch.UI\CognitiveSearch.UI\wwwroot\images\logos\myproviderlogo.png
```

- Change it in your environment definition **organisationLogoUrl** and **organisationLogoProviderUrl**

```json
{
    "id": "contoso",
    "name": "kmcontoso",
    "cloud": "AzureCloud",
    "overlayPath": "<YOUR OVERLAY DIRECTORY IF RELEVANT>",
    "domain": "<YOUR ORG DOMAIN>",
    "tenantId": "<YOUR TENANT ID>",
    "clientId": "00000000-0000-0000-0000-000000000000",
    "subscriptionId": "<YOUR SUBSCRIPTION ID>",
    "organisation": "Contoso Ltd.",
    "organisationWebsite": "https://www.contoso.com/",
    "organisationLogoUrl": "~/images/logos/mylogo.png",
    "organisationLogoProviderUrl": "~/images/logos/myproviderlogo.png",
    "adminUser": "admin@contoso.com",
    "resourceGroupName": "kmcontoso-rg",
    "spellCheckEnabled": false,
    "spellCheckProvider": "Bing",
    "location": "YOUR AZURE LOCATION",
    "clarityProjectId": "",
    "vnetEnable": false
}
```
- Re-Initialize your environment
- Build-WebApps -WindowsOnly -Publish -Settings

Note: logs urls are convey in the web app ui settings. 

### Configuration

If you want to override the UI configuration, add the modified config.json following the same folder structure

```
myoverlay\src\CognitiveSearch.UI\CognitiveSearch.UI\config.json
```
Useful to enable/disable the verticals you need or change some filterings default settings. 

### Settings

If you want to override the entire webappui.json setting
```
myoverlay\configuration\config\webapps\webappui.json
```

If you want to override some webappui.json settings
```
myoverlay\configuration\config\webapps\webappui.[env name].json
```
The above is usefull to add extra settings or override default settings per environment.

# Search 

## Add another index to the default configuration.
```
myoverlay\configuration\config\search\indexes\myindex.json
```
By simply adding a new index, indexer, datasources etc. definition file under the appropriate folder, the solution will pick them up automatically.

## If your company doesn't allow ADLS Gen2 storage account

- Create the overlay datasources folder as follows
```
myoverlay\configuration\config\search\datasources
```
- Copy all datasources files under that new folder
- Adjust the datasource type from **adlsgen2** to **azureblob** [See documentation](https://learn.microsoft.com/en-us/azure/search/search-howto-indexing-azure-blob-storage#define-the-data-source)
```json
{
    "name": "{{param.documentsDataSource}}",
    "description": "Documents Storage datasource",
    "type": "azureblob",
    "credentials": {
        "connectionString": "{{param.storageConnectionString}}"
    },
    "container": {
        "name": "{{param.documentsStorageContainerName}}"
    },
    "dataDeletionDetectionPolicy": {
        "@odata.type": "#Microsoft.Azure.Search.SoftDeleteColumnDeletionDetectionPolicy",
        "softDeleteColumnName": "IsDeleted",
        "softDeleteMarkerValue": "true"
    }
}
```
- Re-Initialize your environment
- Initialize search
