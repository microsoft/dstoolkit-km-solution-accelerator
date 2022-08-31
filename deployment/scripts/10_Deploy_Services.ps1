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
Set-Subscription

Assert-Subscription
New-ResourceGroups
New-AzureKeyVault
New-AppInsights
New-TechnicalStorageAccount
New-DataStorageAccountAndContainer
New-CognitiveServices
New-SearchServices
New-ACRService
New-AzureMapsService
New-BingSearchService

# Save and Apply the Parameters we got
Sync-Parameters

# Add all keys and connection strings to the KV 
Initialize-KeyVault

Stop-Transcript
