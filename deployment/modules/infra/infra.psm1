# Module containing functions for the infrastructure deployment

function Set-Subscription {
    # Select subscription
    Write-Host "Selecting subscription '$config.subscriptionId'";
    az account set --subscription $config.subscriptionId
}

function Assert-Subscription {
    # Required RPs
    $resourceProviders = @("microsoft.cognitiveservices", "microsoft.insights", "microsoft.search", "microsoft.storage", "Microsoft.KeyVault");
    if ($resourceProviders.length) {
        Write-Host "Registering Required Resource Providers"
        foreach ($resourceProvider in $resourceProviders) {
            az provider register --namespace $resourceProvider
        }
    }

    # Optional RPs
    $resourceProviders = @("microsoft.maps", "microsoft.bing");
    if ($resourceProviders.length) {
        Write-Host "Registering Optional Resource Providers"
        foreach ($resourceProvider in $resourceProviders) {
            az provider register --namespace $resourceProvider
        }
    }
}

function New-ResourceGroups {
    function FindOrCreateResourceGroup($resourceGroupName) {    
        $test = az group exists -n $resourceGroupName
        if ($test -eq $true) {
            Write-Host "Using existing resource group '$resourceGroupName'";
        }
        else {
            Write-Host "Resource group '$resourceGroupName' does not exist.";
            if (!$config.location) {
                $config.location = Read-Host "please enter a location:";
            }
            Write-Host "Creating resource group '$resourceGroupName' in location '$config.location'";
            az group create -l $config.location -n $resourceGroupName
        }
    }
    
    FindOrCreateResourceGroup $config.resourceGroupName 

    if ($vnetcfg.enable) {
        FindOrCreateResourceGroup $vnetcfg.vnetResourceGroup
    }
}

function New-AzureKeyVault() {

    foreach ($azureResource in $keyvaultcfg.Items) {
        Write-Host ("Key Vault Service Name  " + $azureResource.Name) -ForegroundColor Yellow

        $exists = az keyvault show --name $azureResource.Name --resource-group $azureResource.ResourceGroup --query id --out tsv

        if ( $exists ) {
            Write-Host "Key Vault service already exists...Skipping.";
        }
        else {    
            az keyvault create --location $config.location `
                --name $azureResource.Name `
                --resource-group $azureResource.ResourceGroup `
                --sku $azureResource.Sku `
                --enable-rbac-authorization true

            Write-Host "Adding Az Cli required permissions to KeyVault" -ForegroundColor Yellow
            
            $currentUserId = ((az ad signed-in-user show) | ConvertFrom-Json).id
            $keyVaultScope = "/subscriptions/" + $config.subscriptionId + "/resourcegroups/" + $config.resourceGroupName + "/providers/Microsoft.KeyVault/vaults/" + $params.keyvault

            az role assignment create --role "Key Vault Secrets Officer" --assignee $currentUserId --scope $keyVaultScope            
            
        }
    }
}

function New-AppInsights {

    $exists = az monitor app-insights component show --app $params.appInsightsService --resource-group $config.resourceGroupName --query id --out tsv

    if ( $exists ) {
        Write-Host "App Insights service already exists...Skipping.";
    }
    else {
        Write-Host "Creating App Insights & Log Analytics workspace";
        # az monitor app-insights component create --app $params.appInsightsService `
        #     --location $config.location `
        #     --resource-group $config.resourceGroupName

        # $workspace="/subscriptions/"+$config.subscriptionId+"/resourcegroups/"+$config.resourceGroupName `
        #     + "/providers/microsoft.operationalinsights/workspaces/" + $params.appInsightsService

        az monitor log-analytics workspace create --resource-group $config.resourceGroupName `
            --workspace-name $params.logAnalyticsService

        az monitor app-insights component create --app $params.appInsightsService `
            --location $config.location `
            --kind web `
            --resource-group $config.resourceGroupName `
            --workspace $params.logAnalyticsService
    }

    Get-AppInsightsInstrumentationKey
}

#
# Create Storage Account Services 
#
function New-StorageAccount {

    foreach ($azureResource in $storagecfg.Items) {
        Write-Host ("Service Name  " + $azureResource.Name) -ForegroundColor Yellow
        $exists = az storage account show --name $azureResource.Name --resource-group $azureResource.ResourceGroup --query id --out tsv

        if ( $exists ) {
            Write-Host "Storage service already exists...Skipping.";
        }
        else {
            az storage account create --name $azureResource.Name `
                --location $config.location `
                --resource-group $azureResource.ResourceGroup `
                --sku Standard_LRS `
                --assign-identity `
                --allow-blob-public-access false `
                --enable-hierarchical-namespace $azureResource.EnableHierarchicalNamespace `
                --kind $azureResource.Accountkind

            if ($azureResource.IsDataStorage) {

                Get-DataStorageAccountAccessKeys
                
                # Iterate through the list of containers to create. 
                foreach ($container in $params.storageContainers) {
                    az storage container create -n $container `
                        --account-name $params.dataStorageAccountName `
                        --account-key $params.storageAccountKey `
                        --resource-group $config.resourceGroupName
                }
                    
                # Soft blob deletion policy (7 days)
                az storage account blob-service-properties update --account-name $params.dataStorageAccountName `
                    --resource-group $config.resourceGroupName `
                    --enable-delete-retention true `
                    --delete-retention-days 7

                # storageFileShares
                foreach ($container in $params.storageFileShares) {
                    az storage share-rm create `
                    --resource-group $config.resourceGroupName `
                    --storage-account $params.dataStorageAccountName `
                    --name $container `
                    --quota 1024 `
                    --enabled-protocols SMB
                }
            }
        }

        if ($azureResource.IsDataStorage) {
            Get-DataStorageAccountAccessKeys            
        }
        else {
            Get-TechStorageAccountAccessKeys
        }
    }
}

function New-CognitiveServices {

    foreach ($azureResource in $cogservicescfg.Items) {
        Write-Host "Provisionning Cognitive Service "$azureResource.Name;

        $exists = az cognitiveservices account show --name $azureResource.Name --resource-group $azureResource.ResourceGroup --query id --out tsv

        if ( $exists ) {
            Write-Host "Service already exists...Skipping.";
        }
        else {
            az cognitiveservices account create `
            -n $azureResource.Name `
            -g $azureResource.ResourceGroup `
            --kind $azureResource.Kind `
            --sku $azureResource.Sku `
            --location $azureResource.Region `
            --custom-domain $azureResource.Name `
            --yes

            az cognitiveservices account identity assign -n $azureResource.Name -g $azureResource.ResourceGroup
        }
    }

    Get-CognitiveServiceKey;
}

function New-SearchServices {

    foreach ($azureResource in $searchcfg.Items) {

        Write-Host "Provisionning Search Service "$azureResource.Name;

        $exists = az search service show --name $azureResource.Name --resource-group $azureResource.ResourceGroup --query id --out tsv

        if ( $exists ) {
            Write-Host "Search Service already exists...Skipping.";
        }
        else {
            az search service create `
                --name  $azureResource.Name `
                --resource-group $azureResource.ResourceGroup `
                --auth-options aadOrApiKey `
                --aad-auth-failure-mode http401WithBearerChallenge `
                --sku $params.searchSku `
                --location $config.location `
                --partition-count 1 `
                --replica-count 1
        }

        az search service update --name $azureResource.Name --resource-group $azureResource.ResourceGroup --identity-type "SystemAssigned"

        if ($searchcfg.Parameters.semanticSearchEnabled)
        {
            Enable-SemanticSearch $azureResource.Name
        }

    }

    Get-SearchServiceKeys;    

}

function New-ACRService {

    foreach ($azureResource in $conregistrycfg.Items) {

        Write-Host "Provisionning ACR service "$azureResource.Name;

        $exists = az acr show -g $azureResource.ResourceGroup -n $azureResource.Name --query id --out tsv

        if ( $exists ) {
            Write-Host "ACR service already exists...Skipping.";
        }
        else {
            az acr create -g $azureResource.ResourceGroup -n $azureResource.Name --sku Premium --admin-enabled true --location $config.location

            az acr identity assign --identities '[system]' -g $azureResource.ResourceGroup -n $azureResource.Name
        }
    }
}

function New-ContainerInstances {

    foreach ($azureResource in $acicfg.Items) {

        Write-Host "Provisionning Container Instances service(s) "$azureResource.Name;

        $exists = az container show -g $azureResource.ResourceGroup -n $azureResource.Name --query id --out tsv

        if ( $exists ) {
            Write-Host "ACI service already exists...Skipping.";
        }
        else {
            if ( $azureResource.YAMLPath ) {
                az container create --resource-group $azureResource.ResourceGroup `
                --file (join-path $global:envpath $azureResource.YAMLPath) `
                --cpu 4 `
                --memory 8 `
                --assign-identity
            }
        }
    }
}

function New-AzureMapsService() {

    if ($params.mapsEnabled) {

        $exists = az maps account show -g $config.resourceGroupName -n $params.maps --query id --out tsv

        if ($exists) {
            Write-Host "Azure Maps service already exists...Skipping.";
        }
        else {
            az maps account create --name $params.maps `
            --resource-group $config.resourceGroupName `
            --sku G2 `
            --kind Gen2 `
            --subscription $config.subscriptionId `
            --accept-tos

            $mapsKey = az maps account keys list --name $params.maps --resource-group $config.resourceGroupName --query primaryKey --out tsv
            Add-Param "MapConfig--AzureMapsSubscriptionKey" $mapsKey
            Save-Parameters
        }
    }
}

function New-BingSearchService() {
    if ( $config.bingEnabled -or ($config.spellCheckEnabled -and $config.spellCheckProvider.Equals("Bing")) ) {
        Write-Host "Provision Bing Search service manually. When provisionned..." -ForegroundColor Red    
        $bingKey = Read-Host "Provide Bing Search Key " -MaskInput
        Add-Param "bingServicesKey" $bingKey
        Save-Parameters
    }
}

#region Access Restriction for App Services

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
        az webapp config access-restriction add `
            --resource-group $appResourceGroupName `
            --name $appName `
            --rule-name $ruleName `
            --action Allow `
            --ip-address $ipaddress `
            --priority $priority
        Write-Host "Added Inbound rule with IPAddress $ipaddress to Resource: $appName" -ForegroundColor "Green"
    }
    elseif (($subnetName)) {
        Write-Host "Adding Inbound rule with subnet  $subnetName to Resource: $appName" -ForegroundColor "Yellow"
        az webapp config access-restriction add `
            -g $appResourceGroupName `
            -n $appName `
            --rule-name $ruleName `
            --action Allow `
            --vnet-name $vnetName `
            --subnet $subnetName `
            --priority $priority `
            --vnet-resource-group $vnetcfg.vnetResourceGroup
        Write-Host "Added Inbound rule with subnet  $subnetName to Resource: $appName" -ForegroundColor "Green"
    }
    Write-Host "========================================"

}

function Get-WebAppPublicOutboundIPAddress {
    param(
        $appResourceGroupName,
        $appName
    )
    $outboundIpAddresses = az webapp show -n $appName -g $appResourceGroupName  --query "possibleOutboundIpAddresses"
    return $outboundIpAddresses.Trim("\""").split(",")
}

function Get-WebAppOutboundIPs() {
    param (
        $appServicesCfg
    )
    $ipAddressToAllow=@()

    foreach ($plan in $appServicesCfg.AppPlans) {
        foreach ($appService in $plan.Services) {
            $ipAddressToAllow+=$(Get-WebAppPublicOutboundIPAddress -appResourceGroupName $config.resourceGroupName -appName $appService.Name)
        }
    }
    return $ipAddressToAllow | Sort-Object -Unique
}

function Set-WebAppServicesAccessRestriction {

    Write-Host "========================================"
    Write-Host ("Set Access Restriction for web apps ") -ForegroundColor Yellow

    $ipAddressToAllow=Get-WebAppOutboundIPs $functionscfg

    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($appService in $plan.Services) {
            if ( $appService.AccessIPRestriction) {

                # https://learn.microsoft.com/en-us/azure/app-service/app-service-ip-restrictions?tabs=azurecli
                
                az resource update `
                --resource-group $config.resourceGroupName `
                --name $appService.Name `
                --resource-type "Microsoft.Web/sites" `
                --set properties.siteConfig.ipSecurityRestrictionsDefaultAction=Deny

                $inboundRulePriority = 500
                $subnetRuleNameCounter = 1
                foreach ($AllowedIP in $ipAddressToAllow) {
                    Add-WebAppAccessRestrictionRule $config.resourceGroupName $appService.Name $AllowedIP ("Allow Function Ip " + $AllowedIP) $null $null $inboundRulePriority
                    $inboundRulePriority++
                    $subnetRuleNameCounter++
                }
            }
        }
    }
    Write-Host ("Access Restriction completed for web apps ") -ForegroundColor Green
    Write-Host "========================================"
}

#endregion