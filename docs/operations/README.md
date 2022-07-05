![banner](../media/banner.png)

# Operations

Our solution accelerator ships very useful PowerShell cmdlets & scripts to deploy or operate your   

## Deployment Scripts 

![](media/deploy.png)

All scripts are using a set of cmdlets 

## PS Cmdlets 

The init_env.ps1 is loading two modules 
-core : cmdlets to deploy, operate & monitor your solution
-vnet : ad-hoc cmdlets to support a VNET deployment

The list of all cmdlets can be done via the below command 
```ps
get-command -Module core
```

| Command | Description |
|--|--|
| Initialize-Search | | 
| Start-SearchIndexer | |
| Reset-SearchIndexer | |


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

### Azure Functions 

```ps

```

