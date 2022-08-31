[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)] [string] $Name,
    [Parameter()] [switch] $Create,
    [Parameter()] [switch] $NoLogin
)

if ( $Create ) {
    Write-Host "Creating a new environment for"$Name" !" -ForegroundColor DarkMagenta
}
else {
    Write-Host "Loading environment"$Name" ..." -ForegroundColor DarkGreen
}

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

$modulePath = Join-Path $ScriptDir "modules"

# Load the core module
Import-Module (join-path $modulePath "infra") -Global -DisableNameChecking -Force
Import-Module (join-path $modulePath "core") -Global -DisableNameChecking -Force
Import-Module (join-path $modulePath "vnet") -Global -DisableNameChecking -Force

Get-Config -Name $Name -WorkDir $ScriptDir -Reload:$true

if ( $config.overlayPath ) {
    Write-Host "Environment overlay "$config.overlayPath -ForegroundColor DarkGreen
}

# Set the Azure Cloud environment
az cloud set -n $config.cloud
Write-Host "Azure Cloud "$config.cloud -ForegroundColor DarkBlue

# Set the Azure Account Subscription id
az account set -s $config.subscriptionId
Write-Host "Azure Subscription "$config.subscriptionId -ForegroundColor DarkBlue
