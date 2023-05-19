param(
    [string]$EnvironmentName
    )

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

Write-Host $ScriptDir

if ($EnvironmentName)
{
    Write-Host "Selected Environment "$EnvironmentName
    $initPath=Join-Path $ScriptDir ".." "init_env.ps1"
    .  $initPath -Name $EnvironmentName -NoLogin
}

Start-Transcript 

# Set the extension to use dynamic installation
az config set extension.use_dynamic_install=yes_without_prompt

# Add the extension for Azure Web App AuthV2
az extension add --name authV2

# Add Application Insights Extension
az extension add --name application-insights

# Deploy steps
Write-Host "=============================================================="
Set-Subscription

Write-Host "=============================================================="
Assert-Subscription

Write-Host "=============================================================="
New-ResourceGroups

Write-Host "=============================================================="
New-AzureKeyVault

Write-Host "=============================================================="
New-AppInsights

Write-Host "=============================================================="
New-StorageAccount

Write-Host "=============================================================="
New-CognitiveServices

Write-Host "=============================================================="
New-SearchServices

Write-Host "=============================================================="
New-ACRService

Write-Host "=============================================================="
New-AzureMapsService

Write-Host "=============================================================="
New-BingSearchService

Write-Host "=============================================================="
New-Cosmos

Write-Host "=============================================================="
New-ServiceBus

Write-Host "=============================================================="
New-RedisCache

Write-Host "=============================================================="
New-AKSCluster

# Save and Apply the Parameters we got
Sync-Config

# Add all keys and connection strings to the KV 
Initialize-KeyVault

Stop-Transcript
