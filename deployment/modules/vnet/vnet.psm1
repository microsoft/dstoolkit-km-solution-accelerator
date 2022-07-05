# Private Endpoints
# https://docs.microsoft.com/en-us/azure/private-link/disable-private-endpoint-network-policy 
# Network policies like network security groups (NSG) are not supported for private endpoints. 
# In order to deploy a Private Endpoint on a given subnet, an explicit disable setting is required on that subnet. 
# This setting is only applicable for the Private Endpoint. 
# For other resources in the subnet, access is controlled based on Network Security Groups (NSG) security rules definition.


#  Create a Private DNS Zone for SQL Database domain, create an association link with the Virtual Network and create a DNS Zone Group to associate the private endpoint with the Private DNS Zone.
# az network private-dns zone create --resource-group myResourceGroup `
#    --name  "privatelink.database.windows.net"

# az network private-dns link vnet create --resource-group myResourceGroup `
# --zone-name  "privatelink.database.windows.net"`
# --name MyDNSLink `
# --virtual-network myVirtualNetwork `
# --registration-enabled false

# az network private-endpoint dns-zone-group create `
#    --resource-group myResourceGroup `
#    --endpoint-name myPrivateEndpoint `
#    --name MyZoneGroup `
#    --private-dns-zone "privatelink.database.windows.net" `
#    --zone-name sql

# App Settings extra for VNET support
# {
#    "name": "WEBSITE_DNS_SERVER",
#    "value": "168.63.129.16",
#    "slotSetting": false
#  },
#  {
#    "name": "WEBSITE_VNET_ROUTE_ALL",
#    "value": "1",
#    "slotSetting": false
#  }

#region Helpers
function Get-ResourceId {
    param (
        $resourceType,
        $resourceName,
        $resourceGroupName
    )
 
    if ($resourceType -eq "FunctionApp" -or $resourceType -eq "WebApp") {
        return (az webapp show --name $resourceName --resource-group $resourceGroupName --query '[id]' --output tsv)
    }
    elseif ($resourceType -eq "Storage") {
        return (az storage account show --name $resourceName --resource-group $resourceGroupName --query '[id]' --output tsv)
    }
    elseif ($resourceType -eq "KeyVault") {
        return (az keyvault show --name $resourceName --resource-group $resourceGroupName --query '[id]' --output tsv)
    }
    elseif ($resourceType -eq "Redis") {
        return (az redis show --name $resourceName --resource-group $resourceGroupName --query '[id]' --output tsv)
    }
    elseif ($resourceType -eq "Search") {
        return (az search service show --name $resourceName --resource-group $resourceGroupName --query '[id]' --output tsv)
    }
    elseif ($resourceType -eq "ServiceBus") {
        return (az servicebus namespace show --name $resourceName --resource-group $resourceGroupName --query '[id]' --output tsv)
    }
    elseif ($resourceType -eq "CognitiveService") {
        return (az cognitiveservices account show --name $resourceName --resource-group $resourceGroupName --query '[id]' --output tsv)
    }
    elseif ($resourceType -eq "ContainerRegistry") {
        return $(az acr show --name $resourceName --resource-group $resourceGroupName --query '[id]' --output tsv)
    }
    elseif ($resourceType -eq "CosmosDB") {
        return $(az cosmosdb show --name $resourceName --resource-group $resourceGroupName --query '[id]' --output tsv)
    }
}
 
# get the private zone based on resource type
function Get-PrivateDNSZone {
    param (
        $resourceType        
    )
 
    if ($resourceType -eq "FunctionApp" -or $resourceType -eq "WebApp") {
        return "privatelink.azurewebsites.net"
    }
    elseif ($resourceType -eq "Storage") {
        return "privatelink.table.core.windows.net"
    }
    elseif ($resourceType -eq "KeyVault") {
        return "privatelink.vaultcore.azure.net"
    }
    elseif ($resourceType -eq "Redis") {
        return "privatelink.redis.cache.windows.net"
    }
    elseif ($resourceType -eq "Search") {
        return "privatelink.search.windows.net"
    }
    elseif ($resourceType -eq "ServiceBus") {
        return "privatelink.servicebus.windows.net"
    }
    elseif ($resourceType -eq "CognitiveService") {
        return "privatelink.cognitiveservices.azure.com"
    }
    elseif ($resourceType -eq "ContainerRegistry") {
        return "privatelink.azurecr.io"
    }
    elseif ($resourceType -eq "CosmosDB") {
        return "privatelink.table.cosmos.azure.com"
    }
}
 
# get the private ip address of azure resource.
function Get-PrivateIPAddress {
    param(
        $vnetResourceGroup,
        $privateEndPointName
    )
 
    return (az network private-endpoint show --name $privateEndPointName --resource-group $vnetResourceGroup --query 'customDnsConfigs[0].{IPAddress:ipAddresses[0]}' --output tsv)
}
 
# get the public outbound ip address of webapp/function app.
function Get-PublicOutboundIPAddress {
    param(
        $appResourceGroupName,
        $appName
    )
 
    $outboundIpAddresses = az webapp show -n $appName -g $appResourceGroupName  --query "possibleOutboundIpAddresses"
    return $outboundIpAddresses.Trim("\""").split(",")
}
 
#endregion
 
#region VNET Private Endpoints
 
# add private end point
function Add-PrivateEndPoint {
    param (
        $appType, 
        $appName,
        $privateEndPointName,
        $privateEndPointConnectionName,
        $groupId
    )
 
    Write-Host "========================================"
    Write-Host "Creating private end point: $privateEndPointName for App: $appName." -ForegroundColor "Yellow"
 
    $resourceId = Get-ResourceId $appType $appName $config.resourceGroupName
    az network private-endpoint create `
        -g $config.vnetResourceGroup `
        -n $privateEndPointName `
        --vnet-name $config.vnetName `
        --subnet $config.vnetPESubnet `
        -l $config.location `
        --private-connection-resource-id $resourceId `
        --connection-name $privateEndPointConnectionName `
        --group-id $groupId
 
    Write-Host "Created private end point: $privateEndPointName for App: $appName." -ForegroundColor "Green"
    Write-Host "========================================"
}
 
# remove the private end point
function Remove-PrivateEndPoint {
    param(
        $privateEndPointName
    )
 
    Write-Host "========================================"
    Write-Host "Deleting Private EndPoint: $privateEndPointName." -ForegroundColor "Yellow"
    az network private-endpoint delete --name $privateEndPointName --resource-group $config.resourceGroupName
    Write-Host "Deleted Private EndPoint: $privateEndPointName." -ForegroundColor "Green"
    Write-Host "========================================"
}
 
#endregion
 
#region VNET Integration for App Services
function Add-VNetIntegration {
    param (
        $vnetName,
        $subnetName,
        $appResourceGroupName, 
        $appName
    )
 
    Write-Host "========================================"
    Write-Host "Adding App Service: $appName to Virtual Network: $vnetName." -ForegroundColor "Yellow"
    az functionapp vnet-integration add -g $appResourceGroupName -n $appName --vnet $vnetName --subnet $subnetName
    Write-Host "Added App Service: $appName to Virtual Network: $vnetName." -ForegroundColor "Green"
    Write-Host "========================================"
}
 
# function Add-VNetAppSettings {
#    param (
#        $appResourceGroupName, 
#        $appName,
#        $appType
#    )
 
#    Write-Host "========================================"
#    Write-Host "Adding Virtual Network related app settings to App: $appName." -ForegroundColor "Yellow"
 
#    # Add DNS and VNET route all app settings
#    if ($appType -eq "FunctionApp" ) {
#        az functionapp config appsettings set --name $appName --resource-group $appResourceGroupName --settings "WEBSITE_DNS_SERVER=168.63.129.16" "WEBSITE_VNET_ROUTE_ALL=1" "WEBSITE_CONTENTOVERVNET=1"
#    }
#    elseif ($appType -eq "WebApp") {
#        az webapp config appsettings set --name $appName --resource-group $appResourceGroupName --settings "WEBSITE_DNS_SERVER=168.63.129.16" "WEBSITE_VNET_ROUTE_ALL=1" "WEBSITE_CONTENTOVERVNET=1"
#    }
 
#    Write-Host "Added Virtual Network related app settings to App: $appName." -ForegroundColor "Green"
#    Write-Host "========================================"
# }
#endregion
 
function Add-KeyVaultNetworkRule {
    param(
        $keyvaultName, 
        $outboundIpAddressArray
    ) 
 
    Write-Host "========================================"
    Write-Host "Updating outbound ip address of app services in the keyvault: $keyvaultName." -ForegroundColor "Yellow"
 
    foreach ($outboundIpAddress in $outboundIpAddressArray) {
        az keyvault network-rule add --name $keyvaultName --ip-address $outboundIpAddress
    }
 
    Write-Host "========================================"
    Write-Host "Updated outbound ip address of app services in the keyvault: $keyvaultName." -ForegroundColor "Green"
}
 
# update keyvault to remove ip address of all web app/function apps
function Remove-KeyVaultNetworkRule {
    param(
        $keyvaultName, 
        $appName, 
        $outboundIpAddressArray
    ) 
 
    Write-Host "========================================"
    Write-Host "Removing outbound ip address of : $appName in the keyvault: $keyvaultName." -ForegroundColor "Yellow"
    foreach ($outboundIpAddress in $outboundIpAddressArray) {
        az keyvault network-rule remove --name $keyvaultName --ip-address $outboundIpAddress
    }
 
    Write-Host "========================================"
    Write-Host "Removed outbound ip address of : $appName in the keyvault: $keyvaultName." -ForegroundColor "Green"
}
# configure storage account with below changes
# allow/deny access to all networks
function Update-KeyVaultNetwork {
    param (
        $vnetResourceGroupName,
        $vnetName,
        $subnets,
        $keyvaultResourceGroupName,
        $keyvaultName,
        $AllNetwork
    )
 
    Write-Host "========================================"
    Write-Host "Updating keyvault: $keyvaultName network settings." -ForegroundColor "Yellow"
    if ($AllNetwork -eq $true) {
        az keyvault update --name $keyvaultName --default-action Allow
    }
    else {
        az keyvault update --name $keyvaultName --default-action Deny
    }
 
    $subnets = $subnets | Select-Object @{Label = "SubnetName"; Expression = { "$($_.'SubnetName')" } } -Unique
    foreach ($subnet in $subnets) {
        $subnetId = $(az network vnet subnet show --resource-group $vnetResourceGroupName --vnet-name $vnetName --name $subnet.SubnetName --query id --output tsv)
        az keyvault network-rule add --resource-group $keyvaultResourceGroupName --name $keyvaultName --subnet $subnetId
    }
 
    Write-Host "Updated keyvault: $keyvaultName network settings." -ForegroundColor "Green"
    Write-Host "========================================"
}
 
 
#region VNET Storage Account 
 
# configure storage account with below changes
# 1. Deny access to all networks
# 2. Add a network rule for a virtual network and subnet.
function Add-StorageAccountVNetNetworkRules {
    param (
        $storageAccountName
    )
 
    Write-Host "========================================"
    Write-Host "Updating storage account: $storageAccountName with subnet information." -ForegroundColor "Yellow"
 
    #Deny access to all networks
    az storage account update --resource-group $config.resourceGroupName  --name $storageAccountName --default-action Deny
 
    # Add subnet to storage account
    foreach ($subnet in $subnets.vnetSubnets) {
        $subnetId = $(az network vnet subnet show --resource-group $config.vnetResourceGroup --vnet-name $config.vnetName  --name $subnet.SubnetName --query id --output tsv)
        az storage account network-rule add --subnet $subnetId -g $config.resourceGroupName --account-name $storageAccountName
    }
 
    Write-Host "Updated storage account: $storageAccountName with subnet information." -ForegroundColor "Green"
    Write-Host "========================================"
}
 
# configure storage account with below changes
# 1. Allow access to all networks
# 2. Remove subnet from storage account.
function Remove-StorageAccountVNetNetworkRules {
    param (
        $storageAccountName
    )
 
    Write-Host "========================================"
    Write-Host "Updating storage account: $storageAccountName with subnet information." -ForegroundColor "Yellow"
 
    # Remove subnet from storage account
    foreach ($subnet in $config.vnetSubnets) {
        $subnetId = $(az network vnet subnet show --resource-group $config.vnetResourceGroup --vnet-name $config.vnetName  --name $subnet.SubnetName --query id --output tsv)
        az storage account network-rule remove --subnet $subnetId -g $config.resourceGroupName --account-name $storageAccountName
    }
 
    # Allow access to all networks
    az storage account update --resource-group $config.resourceGroupName --name $storageAccountName --default-action Allow
 
    Write-Host "Updated storage account: $storageAccountName with subnet information." -ForegroundColor "Green"
    Write-Host "========================================"
}
 
#endregion 
 
 
#region VMET Cognitive Services
 
# Configure cognitive service to vnet
function Add-CognitiveServiceVnetNetworkRules {
    param(
        $cognitiveServiceName
    )
     
    Write-Host "========================================"
    Write-Host "Updating cognitive service: $cognitiveServiceName with subnet information." -ForegroundColor "Yellow"
 
    az cognitiveservices account update --resource-group $config.resourceGroupName --name $cognitiveServiceName --custom-domain $cognitiveServiceName
 
    # Add subnet to storage account
    foreach ($subnet in $config.vnetSubnets) {
        $subnetId = $(az network vnet subnet show --resource-group $config.vnetResourceGroup --vnet-name $config.vnetName --name $subnet.SubnetName --query id --output tsv)
        az cognitiveservices account network-rule add -g $config.resourceGroupName --name $cognitiveServiceName --subnet $subnetId
    }
 
    Write-Host "Updated cognitive service: $cognitiveServiceName with subnet information." -ForegroundColor "Green"
}
 
# Remove cognitive service from vnet
function Remove-CognitiveServiceVnetNetworkRules {
    param(
        $cognitiveServiceName
    )
     
    Write-Host "========================================"
    Write-Host "Updating cognitive service: $cognitiveServiceName with subnet information." -ForegroundColor "Yellow"
 
    az cognitiveservices account update --resource-group $config.resourceGroupName --name $cognitiveServiceName --default-action Allow
 
    # Add subnet to storage account
    foreach ($subnet in $config.vnetSubnets) {
        $subnetId = $(az network vnet subnet show --resource-group $config.vnetResourceGroup --vnet-name $config.vnetName --name $subnet.SubnetName --query id --output tsv)
        az cognitiveservices account network-rule remove -g $config.resourceGroupName --name $cognitiveServiceName --subnet $subnetId
    }
 
    Write-Host "Updated cognitive service: $cognitiveServiceName with subnet information." -ForegroundColor "Green"
}
 
#endregion 
 
 
#region WebApps & Functions
function Add-VNETAppSettings {
 
    Push-Location $global:envpath
 
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($appService in $plan.FunctionApps) {   
            if ( $appService.vnetIntegration) {
                $settings = "config/vnet/appsettings.json" 
                az functionapp config appsettings set --name $appService.Name --resource-group $config.resourceGroupName --settings @$settings
            }    
        }
    }
     
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($appService in $plan.WebApps) {
            if ( $appService.vnetIntegration) {
                $settings = "config/vnet/appsettings.json" 
                az webapp config appsettings set --name $appService.Name `
                    --resource-group $config.resourceGroupName `
                    --settings @$settings
 
                if ($appService.slots) {
                    foreach ($slot in $appService.slots) {
                        az webapp config appsettings set --name $appService.Name `
                            --resource-group $config.resourceGroupName `
                            --slot $slot `
                            --settings @$settings                            
                    }
                }
            }
     
        }
    }
     
    Pop-Location    
}
 
#endregion
 
# Main Script starts here  
# Add Private Endpoints + VNET Integration for all functions and webapps
 
function Initialize-PE {

    $finalIpAddressArray = New-Object System.Collections.ArrayList
 
    Push-Location $global:envpath
 
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($appService in $plan.FunctionApps) {
            if ( $appService.vnetPrivateEndpoint) {
                Add-PrivateEndPoint  "FunctionApp" $appService.Name ($appService.Name + "-pe01") ($appService.Name + "-connection")
            }
 
            if ( $appService.vnetIntegration) {
                $subnetName = $config.vnetIntegrationMapping[$appService.Id]
 
                az functionapp vnet-integration add -g $config.resourceGroupName -n $appService.Name --vnet $config.vnetName --subnet $subnetName
                $settings = "config/vnet/appsettings.json" 
                az functionapp config appsettings set --name $appService.Name --resource-group $config.resourceGroupName --settings @$settings
            }
 
            $outboundIpAddressArray = Get-PublicOutboundIPAddress $config.resourceGroupName $appService.AppName
            foreach ($outboundIPAddress in $outboundIpAddressArray) {
                if ($finalIpAddressArray.Contains($outboundIPAddress) -eq $false) {
                    $finalIpAddressArray.Add($outboundIPAddress)
                }
            }
        }
    }
 
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($appService in $plan.WebApps) {
            if ( $appService.vnetPrivateEndpoint) {
                Add-PrivateEndPoint  "WebApp" $appService.Name ($appService.Name + "-pe01") ($appService.Name + "-connection")
            }
 
            if ( $appService.vnetIntegration) {
                $subnetName = $config.vnetIntegrationMapping[$appService.Id]
                az functionapp vnet-integration add -g $config.resourceGroupName -n $appService.Name --vnet $config.vnetName --subnet $subnetName
                $settings = "config/vnet/appsettings.json" 
                az functionapp config appsettings set --name $appService.Name --resource-group $config.resourceGroupName --settings @$settings
            }
 
            $outboundIpAddressArray = Get-PublicOutboundIPAddress $config.resourceGroupName $appService.AppName
            foreach ($outboundIPAddress in $outboundIpAddressArray) {
                if ($finalIpAddressArray.Contains($outboundIPAddress) -eq $false) {
                    $finalIpAddressArray.Add($outboundIPAddress)
                }
            }
        }
    }
 
    Pop-Location
 
    # Add the other private endpoints
    Add-PrivateEndPoint "Storage" $params.dataStorageAccountName ($params.dataStorageAccountName + "-pe01") ($params.dataStorageAccountName + "-connection") "blob"
    Add-PrivateEndPoint "KeyVault" $param.keyvault ($param.keyvault + "-pe01") ($param.keyvault + "-connection") "vault"
    Add-PrivateEndPoint "CognitiveService" $params.cogServicesBundle ($params.cogServicesBundle + "-pe01") ($params.cogServicesBundle + "-connection") "account"
 
    Add-PrivateEndPoint "ContainerRegistry" $params.acr_prefix ($params.acr_prefix + "-pe01") ($params.acr_prefix + "-connection") "registry"
 
    Add-PrivateEndPoint "Search" $params.searchServiceName ($params.searchServiceName + "-pe01") ($params.searchServiceName + "-connection") "searchService"
 
    # Network Rules 
    Add-StorageAccountVNetNetworkRules $params.dataStorageAccountName
    Add-CognitiveServiceVnetNetworkRules $params.cogServicesBundle
 
    # Keyvault changes. 
    # 1. Deny public access
    # 2. white list the Public IP address of WebApp/FunctionApp so that the Keyvault references works fine. 
    #    refer to the bug here for details - https://github.com/Azure/Azure-Functions/issues/1291
    #    todo: check if ip address is already present or not before adding to keyvault.
 
    Update-KeyVaultNetwork $config.vnetResourceGroup $config.vnetName $config.vnetSubnets $config.resourceGroupName $param.keyvault $false
 
    if ($finalIpAddressArray.Count -gt 0) {
        Add-KeyVaultNetworkRule $param.keyvault $finalIpAddressArray
    }
 
    #region VNET Search 
    # https://docs.microsoft.com/en-us/azure/search/search-indexer-howto-access-private
    Push-Location $global:envpath
 
    $url = "https://management.azure.com/subscriptions/" + $config.subscriptionId + "/resourceGroups/" + $config.resourceGroupName + "/providers/Microsoft.Search/searchServices/" + $params.searchServiceName + "/sharedPrivateLinkResources/blob-pe?api-version=2020-08-01"
    $body = "config/vnet/create-blob-pe.json"
    az rest --method put --uri $url --body @$body
 

    #TODO
    # for ($num = 6 ; $num -le 8 ; $num++)
    # {
    #     $url="https://management.azure.com/subscriptions/"+$config.subscriptionId+"/resourceGroups/"+$config.resourceGroupName+"/providers/Microsoft.Search/searchServices/"+$config.searchServiceName+"/sharedPrivateLinkResources/skill"+$num+"-pe?api-version="+$config.searchManagementVersion
    #     $body="config/vnet/skills/create-skill"+$num+"-pe.json"
    #     az rest --method put --uri $url --body @$body
    # }
    
    Pop-Location
    #endregion
     
    # Azure Search IPs - West Europe
    $cogsearchwe = @("40.74.18.154/32",
        "40.74.30.0/26",
        "51.145.176.249/32",
        "51.145.177.212/32",
        "51.145.178.138/32",
        "51.145.178.140/32",
        "52.137.24.236/32",
        "52.137.26.114/32",
        "52.137.26.155/32",
        "52.137.26.198/32",
        "52.137.27.49/32",
        "52.137.56.115/32",
        "52.137.60.208/32",
        "52.157.231.64/32",
        "104.45.64.0/32",
        "104.45.64.147/32",
        "104.45.64.224/32",
        "104.45.65.30/32",
        "104.45.65.89/32",
        "2603:1020:206:1::180/121")
 
    $cogsearchwe = @("10.240.130.35/32")
 
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($appService in $plan.FunctionApps) {
            Write-Host $appService.Name
 
            $pri = 1
            foreach ($range in $cogsearchwe) {
                az functionapp config access-restriction add `
                    -g $config.resourceGroupName `
                    -n $appService.Name `
                    --description "Azure Cognitive Search rule for External access" `
                    --rule-name $("Search" + $pri) `
                    --action Allow `
                    --priority $pri `
                    --ip-address $range
 
                $pri += 10
            }
        }
    }
 
    $cogsearchwe = @("10.240.130.35/32")
 
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($appService in $plan.FunctionApps) {
            Write-Host $appService.Name
 
            $pri = 1
            foreach ($range in $cogsearchwe) {
                az functionapp config access-restriction remove `
                    -g $config.resourceGroupName `
                    -n $appService.Name `
                    --rule-name $("Search" + $pri) `
 
                $pri += 10
            }
        }
    }
 
    # az functionapp config access-restriction add -g ResourceGroup -n AppName --rule-name
    # app_gateway 
    # --action Allow 
    # --vnet-name core_weu 
    # --subnet app_gateway 
    # --priority 300
 
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($appService in $plan.FunctionApps) {
            Write-Host $appService.Name
 
            $pri = 1
            foreach ($range in $cogsearchwe) {
                az functionapp config access-restriction add `
                    -g $config.resourceGroupName `
                    -n $appService.Name `
                    --description "Azure Cognitive Search rule for External access" `
                    --rule-name vnettest `
                    --vnet-resource-group $config.vnetResourceGroup `
                    --vnet-name $config.vnetName `
                    --subnet $config.vnetPESubnet `
                    --action Allow `
                    --priority 50 `
         
            }
        }
    }
}

function Add-CognitiveSearchIps {

    # Azure Search IPs - West Europe
    $cogsearchwe = @(
        "40.74.18.154/32",
        "40.74.30.0/26",
        "51.145.176.249/32",
        "51.145.177.212/32",
        "51.145.178.138/32",
        "51.145.178.140/32",
        "52.137.24.236/32",
        "52.137.26.114/32",
        "52.137.26.155/32",
        "52.137.26.198/32",
        "52.137.27.49/32",
        "52.137.56.115/32",
        "52.137.60.208/32",
        "52.157.231.64/32",
        "104.45.64.0/32",
        "104.45.64.147/32",
        "104.45.64.224/32",
        "104.45.65.30/32",
        "104.45.65.89/32",
        "2603:1020:206:1::180/121")
  
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($appService in $plan.FunctionApps) {
            Write-Host $appService.Name
 
            $pri = 200
            foreach ($range in $cogsearchwe) {
                az functionapp config access-restriction add `
                    -g $config.resourceGroupName `
                    -n $appService.Name `
                    --description "Azure Cognitive Search rule for External access" `
                    --rule-name $("Search" + $pri) `
                    --action Allow `
                    --priority $pri `
                    --ip-address $range
 
                $pri += 10
            }
        }
    }    
}


function Add-CognitiveSearchIpsToGateway {

    104.40.136.101/32
    $extolloeu = @(
        "104.40.136.101/32",
        "65.52.135.30/32"
    )

    # Azure Search IPs - West Europe
    $cogsearchwe = @(
        "40.74.18.154/32",
        "40.74.30.0/26",
        "51.145.176.249/32",
        "51.145.177.212/32",
        "51.145.178.138/32",
        "51.145.178.140/32",
        "52.137.24.236/32",
        "52.137.26.114/32",
        "52.137.26.155/32",
        "52.137.26.198/32",
        "52.137.27.49/32",
        "52.137.56.115/32",
        "52.137.60.208/32",
        "52.157.231.64/32",
        "104.45.64.0/32",
        "104.45.64.147/32",
        "104.45.64.224/32",
        "104.45.65.30/32",
        "104.45.65.89/32",
        "2603:1020:206:1::180/121")
  
    foreach ($plan in $webappscfg.AppPlans) 
    {
        foreach ($appService in $plan.WebApps) 
        {
            if ($plan.IsLinux) 
            {
                $pri = 200
                foreach ($range in $cogsearchwe) {
                    az webapp config access-restriction add `
                        -g $config.resourceGroupName `
                        -n $appService.Name `
                        --description "Azure Cognitive Search rule for External access" `
                        --rule-name $("Search" + $pri) `
                        --action Allow `
                        --priority $pri `
                        --ip-address $range
     
                    $pri += 10
                }
            }
        }
    }
}


Export-ModuleMember -Function *
