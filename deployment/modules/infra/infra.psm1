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

    if ($config.vnetEnable) {
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
                --sku $azureResource.Sku

            Write-Host "Adding Az Cli required permissions to KeyVault" -ForegroundColor Yellow

            # 04b07795-8ddb-461a-bbee-02f9e1bf7b46 is AZ CLI client id
            az keyvault set-policy --name $azureResource.Name --object-id 04b07795-8ddb-461a-bbee-02f9e1bf7b46 `
                --certificate-permissions get list create delete `
                --key-permissions get list create delete `
                --secret-permissions get set list delete
            
            $currentUserId = ((az ad signed-in-user show) | ConvertFrom-Json).objectId
            az keyvault set-policy --name $azureResource.Name --object-id $currentUserId `
                --certificate-permissions get list create delete `
                --key-permissions get list create delete `
                --secret-permissions get set list delete
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
                --enable-hierarchical-namespace true `
                --kind $azureResource.Accountkind

            if ($azureResource.IsDataStorage) {

                Get-DataStorageAccountParameters; 

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
            }
            else { 
                Get-TechStorageAccountParameters;            
            }
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
            --location $config.location `
            --custom-domain $azureResource.Name `
            --yes

            az cognitiveservices account identity assign -n $azureResource.Name -g $azureResource.ResourceGroup
        }
    }

    Get-CognitiveServiceKey;
}

function New-SearchServices {

    foreach ($azureResource in $searchservicecfg.Items) {

        Write-Host "Provisionning Search Service "$azureResource.Name;

        $exists = az search service show --name $azureResource.Name --resource-group $azureResource.ResourceGroup --query id --out tsv

        if ( $exists ) {
            Write-Host "Search Service already exists...Skipping.";
        }
        else {
            az search service create `
                --name  $azureResource.Name `
                --resource-group $azureResource.ResourceGroup `
                --sku $azureResource.Sku `
                --location $config.location `
                --partition-count 1 `
                --replica-count 1
        }

        az search service update --name $azureResource.Name --resource-group $azureResource.ResourceGroup --identity-type "SystemAssigned"

        if ($searchservicecfg.Parameters.semanticSearchEnabled)
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

function New-AzureMapsService() {
    if ($config.mapSearchEnabled) {

        $exists = az maps account show -g $config.resourceGroupName -n $params.maps --query id --out tsv

        if ($exists) {
            Write-Host "Azure Maps service already exists...Skipping.";
        }
        else {
            az maps account create --name $params.maps `
            --resource-group $config.resourceGroupName `
            --sku S0 `
            --subscription $config.subscriptionId `
            --accept-tos

            $mapsKey = az maps account keys list --name $params.maps --resource-group $config.resourceGroupName --query primaryKey --out tsv

            Add-Param "mapsSubscriptionKey" $mapsKey
        }
    }
}

function New-BingSearchService() {
    if ( $config.webSearchEnabled -or ($config.spellCheckEnabled -and $config.spellCheckProvider.Equals("Bing")) ) {
        Write-Host "Provision Bing Search service manually. When provisionned..." -ForegroundColor Red    
        $bingKey = Read-Host "Provide Bing Search Key " -MaskInput
        Add-Param "bingServicesKey" $bingKey
    }
}
