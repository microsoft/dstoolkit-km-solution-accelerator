#region Helpers

# get resource id
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
 
# get resource id
function Get-AvailableSubnets {
   
    $availableSubnetsItems = az network vnet subnet list -g $vnetcfg.vnetResourceGroup --vnet-name $vnetcfg.vnetName --query [].name 
    $subnetNames = @()
    foreach ($subnet in $availableSubnetsItems) {
    $cleanedResult = ((($subnet.Replace("[", "")).Replace("]", "")).Replace('"', '')).Replace(',', '')
        if ($cleanedResult) {
            $subnetNames += $cleanedResult
        }
    }

    return $subnetNames

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

function Add-VNET {

    az network vnet create --name $vnetcfg.vnetName --resource-group $vnetcfg.vnetResourceGroup `
        --location $config.location `
        --address-prefixes $vnetcfg.vnetAddressPrefix
}

function Add-Subnet {
    param(
        $resourceGroupName,
        $vnetName,
        $subnetName,
        $networkSecurityGroup,
        $ipaddress
    )

    Write-Host("creating subnet with name: " + $subnetName) -ForegroundColor "Yellow"
    az network vnet subnet create -g $resourceGroupName --vnet-name $vnetName -n $subnetName `
    --address-prefixes $ipaddress --network-security-group $networkSecurityGroup `
    --disable-private-endpoint-network-policies false `
    --service-endpoints Microsoft.Storage Microsoft.CognitiveServices Microsoft.ContainerRegistry Microsoft.KeyVault Microsoft.Web

    Add-NewSubnettoAllResources $subnetName
    Write-Host("created subnet with name: " + $subnetName) -ForegroundColor "Green"
}

function Add-NewSubnettoAllResources {
    param(
        $subnetName
    )

    $azureResourcesArray = ($cogservicescfg, $storagecfg, $keyvaultcfg, $conregistrycfg)
    foreach ($azureResource in $azureResourcesArray) {
        $serviceEndpoint = $azureResource.ServiceEndPoint
        foreach ($item in $azureResource.items) {
            Add-SubnetToNetworkRule $item.Name $subnetName $serviceEndpoint 
        }
    }

    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($appService in $plan.FunctionApps) {
            Add-WebAppAccessRestrictionRule $config.resourceGroupName $appService.Name  $null ("Allow Subnet " + $vnetparams.$vnetparams.subnetRuleNameCounter) $vnetcfg.vnetName $subnetName $vnetparams.inboundRulePriority
            $vnetparams.inboundRulePriority++
            $vnetparams.subnetRuleNameCounter++
        }
    }

    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($appService in $plan.WebApps) {
            Add-WebAppAccessRestrictionRule $config.resourceGroupName $appService.Name  $null ("Allow Subnet " + $vnetparams.subnetRuleNameCounter) $vnetcfg.vnetName $subnetName $vnetparams.inboundRulePriority
            $vnetparams.inboundRulePriority++
            $vnetparams.subnetRuleNameCounter++
        }
    }
}
  
function Get-AvailableIPAddress {
    param(
        $startAvailableAddress
    )

    $startingIPAvailableAddress = $startAvailableAddress
    $array = $startingIPAvailableAddress.split('.')
    $nextmaskAddress = [int]$array[2] + 1
    $nextAvailableAddress = $array[0] + '.' + $array[1] + '.' + $nextmaskAddress + '.' + $array[3]
    
    return $nextAvailableAddress

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
        $groupId,
        $subnet
    )
 
    Write-Host "========================================"
    Write-Host "Creating private end point: $privateEndPointName for Resource: $appName." -ForegroundColor "Yellow"
    $resourceId = Get-ResourceId $appType $appName $config.resourceGroupName

    az network private-endpoint create `
        -g $vnetcfg.vnetResourceGroup `
        -n $privateEndPointName `
        --vnet-name $vnetcfg.vnetName `
        --subnet $subnet `
        -l $config.location `
        --private-connection-resource-id $resourceId `
        --connection-name $privateEndPointConnectionName `
        --group-id $groupId
 
    Write-Host "Created private end point: $privateEndPointName for Resource: $appName." -ForegroundColor "Green"
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
    $vnetId = az network vnet show -n $vnetName -g $vnetcfg.vnetResourceGroup --query id  -o tsv  
    Write-Host "========================================"
    Write-Host "Adding subnet: $subnetName as outbound rule to App Service: $appName" -ForegroundColor "Yellow"
    az functionapp vnet-integration add -g $appResourceGroupName -n $appName --vnet $vnetId --subnet $subnetName
    Write-Host "Added subnet: $subnetName as outbound rule to App Service: $appName" -ForegroundColor "Green"
    Write-Host "========================================"
}
 
function Add-WebAppAccessRestrictionRule {
    param (
        [Parameter(Mandatory = $true, Position = 0)]
        $appResourceGroupName, 
        [Parameter(Mandatory = $true, Position = 1)]
        $appName,
        [Parameter(Mandatory = $false, Position = 2)]
        $ipaddress,
        [Parameter(Mandatory = $false, Position = 3)]
        $ruleName,
        [Parameter(Mandatory = $false, Position = 4)]
        $vnetName,
        [Parameter(Mandatory = $false, Position = 5)]
        $subnetName,
        [Parameter(Mandatory = $true, Position = 6)]
        $priority
    )
 
    Write-Host "========================================"
    if ($ipaddress) {
        Write-Host "Adding Inbound rule with IPAddress $ipaddress $appResourceGroupName $ruleName $ipaddress)  $priority to Resource: $appName" -ForegroundColor "Yellow"
        az webapp config access-restriction add --resource-group $appResourceGroupName --name $appName `
        --rule-name $ruleName --action Allow --ip-address $ipaddress --priority $priority
        Write-Host "Added Inbound rule with IPAddress $ipaddress to Resource: $appName" -ForegroundColor "Green"
    }
    elseif (($subnetName)) {
        Write-Host "Adding Inbound rule with subnet  $subnetName to Resource: $appName" -ForegroundColor "Yellow"
        az webapp config access-restriction add -g $appResourceGroupName -n $appName `
        --rule-name $ruleName --action Allow --vnet-name $vnetName --subnet $subnetName --priority $priority --vnet-resource-group $vnetcfg.vnetResourceGroup
        Write-Host "Added Inbound rule with subnet  $subnetName to Resource: $appName" -ForegroundColor "Green"
    }
    Write-Host "========================================"

}
#endregion

#region keyvault network rules

# add network rule to keyvault
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
#endregion
 
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
 
#region VNET Cognitive Services
 
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
    Write-Host "========================================"
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
    Write-Host "========================================"

}
 
#endregion 

#region Networkaccess rules
function Add-SubnetToNetworkRule {
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $resourcename,
        [Parameter(Mandatory = $true, Position = 1)]
        $subnetName,
        [Parameter(Mandatory = $true, Position = 2)]
        $serviceEndpoints,
        [Parameter(Mandatory = $false, Position = 3)]
        $ipAddresses
    )
    Write-Host "========================================"
    Write-Host "Adding subnet:$subnetName to resource: $resourcename" -ForegroundColor "Yellow"
    
    $subnetid = $(az network vnet subnet show --resource-group $vnetcfg.vnetResourceGroup --vnet-name $vnetcfg.vnetName --name $subnetName --query id --output tsv)

    if ($serviceEndpoints -eq "Microsoft.Storage") {
        az storage account network-rule add --resource-group $config.resourceGroupName --account-name $resourcename  --subnet $subnetid
    }
    elseif ($serviceEndpoints -eq "Microsoft.CognitiveServices") {
        az cognitiveservices account network-rule add `
        -g $config.resourceGroupName -n $resourcename `
        --subnet $subnetid
    }
    elseif ($serviceEndpoints -eq "Microsoft.ContainerRegistry") {
        foreach ($ipAddress in $ipAddresses) {
            az acr network-rule add `
            --name $resourcename `
            --ip-address $ipAddress
        }

    }
    elseif ($serviceEndpoints -eq "Microsoft.KeyVault") {
        az keyvault network-rule add --resource-group $config.resourceGroupName --name $resourcename --subnet $subnetId
    }
    Write-Host "Added subnet:$subnetName to resource: $resourcename" -ForegroundColor "Green"
    Write-Host "========================================"

}
function Enable-PrivaceAccess {
    param(
        $resourcename,
        $appType
    )
    Write-Host "========================================"
    Write-Host ("Enabling private access to $appType : $resourcename  ") -ForegroundColor "Yellow"

    $resourceId = Get-ResourceId $appType $resourcename $config.resourceGroupName

    if ($appType -eq "ContainerRegistry") {
        az acr update --name $resourcename --default-action Deny
    }
    elseif ($appType -eq "KeyVault") {
        az keyvault update --name $resourcename --default-action Deny
    }
    elseif ($appType -eq "Search") {
        az search service update --name $resourcename --resource-group $config.resourceGroupName --public-access "Disabled"
    }
    else {
    az resource update --ids $resourceId  --set properties.networkAcls="{'defaultAction':'Deny'}"
    }

    Write-Host ("Enabled private access to $appType : $resourcename  ") -ForegroundColor "Yellow"
    Write-Host "========================================"
}
#endregion

#region WebApps & Functions
function Add-VNETAppSettings {
 
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
}
 
#endregion

#region initialise vnet integration

function Set-VNETFunctions {
    Write-Host "========================================"
    Write-Host ("VNet Integration starting for function apps") -ForegroundColor Yellow
    $funcConfigGroupId = $functionscfg.GroupId
    $funcGroupId = $groupidcfg.$funcConfigGroupId
    foreach ($plan in $functionscfg.AppPlans) {
            $subnetName = $plan.name + "-subnet"
            if ($availableSubnets -match $subnetName) {
                Write-Host ("subnet exists for function app " + $plan.Name) -ForegroundColor Green
            }
            else {
                Write-Host ("subnet $subnetName  does not exists in " + $vnetcfg.vnetName) -ForegroundColor Green
                Add-Subnet $vnetcfg.vnetResourceGroup $vnetcfg.vnetName $subnetName $vnetcfg.networkSecurityGroup ($vnetparams.nextAvailableFuncAddress + '/28')
                $global:availableSubnets = $availableSubnets + $subnetName
                $vnetparams.nextAvailableFuncAddress = Get-AvailableIPAddress $vnetparams.nextAvailableFuncAddress
            }
            foreach ($appService in $plan.FunctionApps) {
                Add-PrivateEndPoint  "FunctionApp" $appService.Name ($appService.Name + "-pe") ($appService.Name + "-connection") $funcGroupId $vnetcfg.privateEndPointSubnet 
    
                Write-Host("Adding subnet: $subnetName to outbound rule for Resource " + $appService.Name) -ForegroundColor Yellow
                $vnetId = az network vnet show -n $vnetcfg.vnetName -g $vnetcfg.vnetResourceGroup --query id  -o tsv  
                
                az functionapp vnet-integration add -g $config.resourceGroupName -n $appService.Name --vnet $vnetId --subnet $subnetName
                Write-Host("Added subnet: $subnetName to outbound rule for resource" + $appService.Name) -ForegroundColor Green
                
                if ( $appService.AccessIPRestriction) {
                    Add-WebAppAccessRestrictionRule $config.resourceGroupName $appService.Name $vnetcfg.ipAddressToAllow ("Allow Ip" + $vnetparams.subnetRuleNameCounter) $null $null $vnetparams.inboundRulePriority
                    $vnetparams.inboundRulePriority++
                    $vnetparams.subnetRuleNameCounter++
                }

                if ( $appService.AccessSubnetRestriction) {
                    foreach ($subnet in $existingSubnets) {
                        Add-WebAppAccessRestrictionRule $config.resourceGroupName $appService.Name  $null ("Allow Subnet" + $vnetparams.subnetRuleNameCounter) $vnetcfg.vnetName $subnet.Trim() $vnetparams.inboundRulePriority
                        $vnetparams.inboundRulePriority++ 
                        $vnetparams.subnetRuleNameCounter++
                    }
                }
            
            }
    }
    Add-VNETParam "lastCreatedFuncAppIpAddress" $vnetparams.nextAvailableFuncAddress

    Write-Host ("VNet Integration completed for function apps") -ForegroundColor Green
    Write-Host "========================================"

}

function Set-VNETWebApps { 

    Write-Host "========================================"
    Write-Host ("VNet Integration starting for web apps ") -ForegroundColor Yellow

    $appServiceGroupId = $webappscfg.GroupId
    $webAppGroupId = $groupidcfg.$appServiceGroupId
    foreach ($plan in $webappscfg.AppPlans) {
            $subnetName = $plan.name + "-subnet"
            if ($availableSubnets -match $subnetName) {
                Write-Host ("subnet exists for function app " + $plan.Name) -ForegroundColor Green
            }
            else {
                Write-Host ("subnet does not exists ") -ForegroundColor Green
                Add-Subnet $vnetcfg.vnetResourceGroup $vnetcfg.vnetName $subnetName $vnetcfg.networkSecurityGroup ($vnetparams.nextAvailableWebAppAddress + '/28')
                $global:availableSubnets = $availableSubnets + $subnetName
                $vnetparams.nextAvailableWebAppAddress = Get-AvailableIPAddress $vnetparams.nextAvailableWebAppAddress
            }
            foreach ($appService in $plan.WebApps) {
                Add-PrivateEndPoint  "WebApp" $appService.Name ($appService.Name + "-pea") ($appService.Name + "-connection") $webAppGroupId $vnetcfg.privateEndPointSubnet 

                Write-Host("Adding subnet: $subnetName to outbound rule")
                $vnetId = az network vnet show -n $vnetcfg.vnetName -g $vnetcfg.vnetResourceGroup --query id  -o tsv  

                az functionapp vnet-integration add -g $config.resourceGroupName -n $appService.Name --vnet $vnetId --subnet $subnetName

                if ( $appService.AccessIPRestriction) {
                    Add-WebAppAccessRestrictionRule $config.resourceGroupName $appService.Name $vnetcfg.ipAddressToAllow ("Allow IP" + $vnetparams.subnetRuleNameCounter) $null $null $vnetparams.inboundRulePriority
                    $vnetparams.inboundRulePriority++    
                    $vnetparams.subnetRuleNameCounter++
                }

                if ( $appService.AccessSubnetRestriction) {
                    foreach ($subnet in $existingSubnets) {
                        Add-WebAppAccessRestrictionRule $config.resourceGroupName $appService.Name  $null ("Allow Subnet" + $vnetparams.subnetRuleNameCounter) $vnetcfg.vnetName $subnet.Trim() $vnetparams.inboundRulePriority
                        $vnetparams.inboundRulePriority++    
                        $vnetparams.subnetRuleNameCounter++
                    }
                }
            }
    }
    Add-VNETParam "lastCreatedWebAppIpAddress" $vnetparams.nextAvailableWebAppAddress

    Add-VNETParam "inboundRulePriority" $vnetparams.inboundRulePriority
    Add-VNETParam "inboundRulePriority" $vnetparams.inboundRulePriority
    Add-VNETParam "subnetRuleNameCounter" $vnetparams.subnetRuleNameCounter
    Add-VNETParam "subnetRuleNameCounter" $vnetparams.subnetRuleNameCounter

    Write-Host ("VNet Integration completed for web apps ") -ForegroundColor Green
    Write-Host "========================================"

}

function Set-VNETSearch { 

    Write-Host "========================================"
    Write-Host ("VNet Integration starting for search service") -ForegroundColor Yellow
  
    $searchserviceType = $searchservicecfg.Apptype
    $searchServiceGroupId = $searchservicecfg.GroupId
    $searchsrvcpGroupId = $groupidcfg.$searchServiceGroupId
    foreach ($srchservice in $searchservicecfg.Items) {
        if ( $srchservice.EnablePrivateAccess) {
          Enable-PrivaceAccess  $srchservice.Name  $searchserviceType
       }
       
        Add-PrivateEndPoint  $searchserviceType $srchservice.Name ($srchservice.Name + "-pe") ($srchservice.Name + "-connection") $searchsrvcpGroupId $vnetcfg.privateEndPointSubnet 
        az search service update --name $srchservice.Name --resource-group $config.resourceGroupName --public-access --public-network-access "disabled"

       if ( $srchservice.AddPrivateSharedLink) {
            foreach ($spaReq in $createpecfg.items) {
                if ($spaReq.properties.privateLinkResourceId -match "Microsoft.Web/sites") {
                    $url = "https://management.azure.com/subscriptions/" + $config.subscriptionId + "/resourceGroups/CognitiveSearch-Dev/providers/Microsoft.Search/searchServices/" + $srchservice.Name + "/sharedPrivateLinkResources/" + $spaReq.name + "?api-version=2020-08-01-preview"
                }
                else {
                    $url = "https://management.azure.com/subscriptions/" + $config.subscriptionId + "/resourceGroups/CognitiveSearch-Dev/providers/Microsoft.Search/searchServices/" + $srchservice.Name + "/sharedPrivateLinkResources/" + $spaReq.name + "?api-version=2020-08-01"
                }
                $body = ($spaReq | ConvertTo-Json -Compress).Replace('"', '\"')
                Write-Host("=========================") -ForegroundColor Yellow
                Write-Host("creating shared private access with name: " + $spaReq.name) -ForegroundColor Yellow

                az rest --method put --uri $url --body $body
                
                Write-Host("created shared private access with name: " + $spaReq.name) -ForegroundColor Green
                Write-Host("=========================") -ForegroundColor Green
                Start-Sleep -Seconds 120
            }
       }
    }

    Write-Host ("VNet Integration completed for search service") -ForegroundColor Green
    Write-Host "========================================"

}

function Set-VNETResource ($azureResourcesArray) {
    Write-Host "========================================"
    Write-Host ("VNet Integration starting for $azureResourcesArraycognitive") -ForegroundColor Yellow

    foreach ($azureResource in $azureResourcesArray) {
        $appType = $azureResource.Apptype
        Write-Host ("App type is  " + $appType) -ForegroundColor Yellow

        $serviceEndPoint = $azureResource.ServiceEndPoint
        $azServiceGroupId = $azureResource.GroupId
        $azureResourceGroupId = $groupidcfg.$azServiceGroupId
        Write-Host ("VNet Integration started for resource with types " + $appType) -ForegroundColor Yellow

        foreach ($item in $azureResource.items) {
            Write-Host ("VNet Integration started for resource with name " + $item.Name) -ForegroundColor Yellow

            if ( $item.EnablePrivateAccess) {
                Enable-PrivaceAccess  $item.Name  $appType
            }
            
            Add-PrivateEndPoint $appType $item.Name ($item.Name + "-pea") ($item.Name + "-connection") $azureResourceGroupId $vnetcfg.privateEndPointSubnet 
    
            if ( $item.AddExistingSubnets) {
                if ($appType -eq "ContainerRegistry") {
                    Add-SubnetToNetworkRule $item.Name $subnet.Trim() $serviceEndPoint $item.ipAddressToAdd
                }
                elseif ($item.Itemtype -eq "Language") {
                    foreach ($subnet in $existingSubnets) {
                        Write-Host("$subnet adding subnet") -ForegroundColor Yellow
                        Add-SubnetToNetworkRule $item.Name $subnet.Trim() $serviceEndPoint 
                    }
                }
                else {
                    foreach ($subnet in $availableSubnets) {
                        Write-Host("$subnet adding subnet") -ForegroundColor Yellow
                        Add-SubnetToNetworkRule $item.Name $subnet.Trim() $serviceEndPoint 
                    }
                }
            }
            
            Write-Host ("VNet Integration completed for resource with name " + $item.Name) -ForegroundColor Green
        }
        Write-Host ("VNet Integration completed for resource with types " + $appType) -ForegroundColor Green

    }
    Write-Host ("VNet Integration starting for $azureResourcesArraycognitive") -ForegroundColor Yellow
    Write-Host "========================================"
}

# Main Script starts here  
# Add Private Endpoints + VNET Integration for all functions and webapps
function Initialize-VNET {

    Import-VNETConfig

    Set-VNETFunctions
    Save-VNETParameters

    Set-VNETWebApps 
    Save-VNETParameters

    Set-VNETResource $cogservicescfg 
    Set-VNETResource $storagecfg
    Set-VNETResource $keyvaultcfg
    Set-VNETResource $conregistrycfg

    Set-VNETSearch
    Save-VNETParameters
}
#endregion

#region VNET Configuration & Parameters

function Import-VNETConfig {

    $global:vnetcfg = [string] (Get-Content -Path (join-path $global:envpath "config" "vnet" "config.json"))
    $global:vnetcfg = ConvertFrom-Json $global:vnetcfg
    $global:availableSubnets = Get-AvailableSubnets
    $global:existingSubnets = Get-AvailableSubnets

    Import-createpeConfig
    Import-groupidConfig

    Import-VNETParams

    if (! $vnetparams.inboundRulePriority) {
        Add-VNETParam "inboundRulePriority" 400
    }

    if (! $vnetparams.subnetRuleNameCounter) {
        Add-VNETParam "subnetRuleNameCounter" 1
    }

    if (! $vnetparams.lastCreatedWebAppIpAddress) {
        Add-VNETParam "nextAvailableWebAppAddress" $vnetcfg.VnetWebAppStartIpAddress
    }

    if (! $vnetparams.lastCreatedFuncAppIpAddress) {
        Add-VNETParam "nextAvailableFuncAddress" $vnetcfg.VnetFuncAppStartIpAddress
    }
}
function Import-createpeConfig() {
    # Import Other configurations like functions
    $global:createpecfg = [string] (Get-Content -Path (join-path $global:envpath "config" "vnet" "createpe.json"))
    $global:createpecfg = ConvertFrom-Json $global:createpecfg
}
function Import-groupidConfig() {
    # Import Other configurations like functions
    $global:groupidcfg = [string] (Get-Content -Path (join-path $global:envpath "config" "vnet" "groupid.json"))
    $global:groupidcfg = ConvertFrom-Json $global:groupidcfg
}

function Import-VNETParams {

    $global:vnetparams = New-Object -TypeName PSObject

    $parametersJson = join-path $global:envpath "vnetparameters.json"

    if (Test-Path -Path $parametersJson) {
        # Loading Environment Parameters
        $readParams = [string] (Get-Content -Path $parametersJson -ErrorAction SilentlyContinue)

        if ($readParams) {
            if ($readParams.Length -gt 0) {
                $global:vnetparams = ConvertFrom-Json $readParams
            }
        }
    }

    return $global:vnetparams
}

function Add-VNETParam($name, $value) {

    if ( $global:vnetparams.PSobject.Properties.name -eq $name) {
        $global:vnetparams.$name = $value
    }
    else {
        $global:vnetparams | Add-Member -MemberType NoteProperty -Name $name -Value $value -ErrorAction Ignore
    }
}

function Save-VNETParameters() {
    $global:vnetparams | ConvertTo-Json -Depth 100 -Compress | Out-File -FilePath (join-path $global:envpath "vnetparameters.json") -Force
}
#endregion

#region Approve private endpoints
Function approve-privateEndpoints {
    foreach ($spaReq in $createpecfg.items) {
        if ($spaReq.properties.groupId -eq "vault") {
            approve-KeyvaultEndPoint $spaReq.properties.privateLinkResourceId
        }
        elseif ($spaReq.properties.groupId -eq "sites") {
            approve-FuncAppEndPoint $spaReq.properties.privateLinkResourceId
        }
        elseif ($spaReq.properties.groupId -eq "blob") {
            approve-storageAccountPrivatEndPoint $spaReq.properties.privateLinkResourceId
        }
    }
}
Function approve-storageAccountPrivatEndPoint {
    param(
        $storageAccountNameResourceId
    )
    $id = (az storage account show --id $storageAccountNameResourceId --query "privateEndpointConnections[1].id")
    az storage account private-endpoint-connection approve --id $id
}

Function approve-KeyvaultEndPoint {
    param(
        $keyvaultResourceId
    )
    $array = $keyvaultResourceId.split('/')

    $length = $array.$length

    $name = $array[$length - 1]
   
    $privateEndPointConnections = (az keyvault show -n $name --query "properties.privateEndpointConnections")
    foreach ($privateEndPointConnection in $privateEndPointConnections) {

        if ($privateEndPointConnection.privateLinkServiceConnectionState.status -eq "Pending") {
            az keyvault private-endpoint-connection approve --id $privateEndPointConnection.id
        }
    }
}

Function approve-FuncAppEndPoint {
    param(
        $funcAppResourceId
    )
    $id = (az network private-endpoint-connection list --id $funcAppResourceId --query "[0].id")
    az network private-endpoint-connection approve --id  $id --description "Approved"
}

#endregion


Export-ModuleMember -Function *
