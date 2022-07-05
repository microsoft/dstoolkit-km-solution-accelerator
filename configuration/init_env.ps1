$global:envpath="."

$env:PSModulePath = $env:PSModulePath + "$([System.IO.Path]::PathSeparator)modules"
Import-Module core -Global -DisableNameChecking -Force
Import-Module vnet -Global -DisableNameChecking -Force

$global:config = Import-Config -WorkDir $global:envpath
$global:params = Import-Params -WorkDir $global:envpath

# Set the Azure Cloud environment
az cloud set -n $config.cloud

# Set the Azure Account Subscription id
az account set -s $config.subscriptionId
