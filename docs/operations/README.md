![banner](../media/banner.png)

# Operations

Our solution accelerator ships very useful PowerShell cmdlets & scripts to deploy or operate your solution.   

## Deployment Scripts 

![](media/deploy.png)

All scripts are using a set of cmdlets defined in two modules.  
-core : cmdlets to deploy, operate & monitor your solution
-vnet : ad-hoc cmdlets to support a VNET deployment

The __init_env.ps1__ is loading two modules at startup. 

The list of all cmdlets can be done via the below command 
```ps
get-command -Module core
```

| Command | Description |
|--|--|
| Initialize-Search | Push the entire search configuration to Azure Cognitive Search| 
| Start-SearchIndexer | Start all indexers |
| Reset-SearchIndexer | Reset all indexers |

## Core PowerShell module

Validate the core and vnet modules are loaded in your PS session. 

```ps
get-module
```
Example of output
```ps
ModuleType Version    PreRelease Name                                ExportedCommands
---------- -------    ---------- ----                                ----------------
Script     0.0                   core                                {Add-BlobRetryTag, Add-ExtendedParameters, Add-KeyVaultFun… 
Manifest   1.2.5                 Microsoft.PowerShell.Archive        {Compress-Archive, Expand-Archive}
Manifest   7.0.0.0               Microsoft.PowerShell.Management     {Add-Content, Clear-Content, Clear-Item, Clear-ItemPropert… 
Manifest   7.0.0.0               Microsoft.PowerShell.Utility        {Add-Member, Add-Type, Clear-Variable, Compare-Object…}     
Script     2.1.0                 PSReadLine                          {Get-PSReadLineKeyHandler, Get-PSReadLineOption, Remove-PS… 
Script     0.0                   vnet                                {Add-CognitiveSearchIps, Add-CognitiveSearchIpsToGateway, … 

```

To list of all available commands from the **core** module 

```ps
Get-Command -Module core
```

## VNET PowerShell module 

**vnet** module is provided as is, containing some useful commands to setup network rules or private endpoints.  

I would encourage you to review and adapt the vnet commands to your needs and Azure security policies.


## Operating your Azure Cognitive Search instance

**Remove Search index**

```ps
Update-SearchAliases -method DELETE
Remove-SearchIndex -name {{config.name}}-index
Update-SearchIndex
```

**Start a specific indexer** 

```ps
Start-SearchIndexer documents
```

**Get all indexers status**

```ps
Get-SearchIndexersStatus
```

**Get status of a specific indexer**

```ps
Get-SearchIndexerStatus documents
```

## Azure Functions 

```ps
Test-Functions
```

