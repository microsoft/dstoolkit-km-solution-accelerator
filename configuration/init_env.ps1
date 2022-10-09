$global:envpath = "."

$env:PSModulePath = $env:PSModulePath + "$([System.IO.Path]::PathSeparator)modules"
Import-Module infra -Global -DisableNameChecking -Force
Import-Module core -Global -DisableNameChecking -Force
Import-Module vnet -Global -DisableNameChecking -Force

$global:config = Import-Config -WorkDir $global:envpath
$global:params = Import-Params

# Set the Azure Cloud environment
az cloud set -n $global:config.cloud

# Set the Azure Account Subscription id
az account set -s $global:config.subscriptionId
