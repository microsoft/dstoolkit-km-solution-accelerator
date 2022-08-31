# Module containing functions for the infrastructure deployment

function Set-Subscription
{
    # Select subscription
    Write-Host "Selecting subscription '$config.subscriptionId'";
    az account set --subscription $config.subscriptionId
}

function Assert-Subscription
{
    # Register RPs
    $resourceProviders = @("microsoft.cognitiveservices", "microsoft.insights", "microsoft.search", "microsoft.storage","microsoft.maps","microsoft.bing","Microsoft.KeyVault");
    if ($resourceProviders.length) {
        Write-Host "Registering resource providers"
        foreach ($resourceProvider in $resourceProviders) {
            az provider register --namespace $resourceProvider
        }
    }
}

function New-ResourceGroups
{
    function FindOrCreateResourceGroup($resourceGroupName)
    {    
        $test = az group exists -n $resourceGroupName
        if ($test -eq $true) 
        {
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
}

function New-AzureKeyVault() 
{
    Write-Host "=============================================================="

    az keyvault create --location $config.location --name $params.keyvault --resource-group $config.resourceGroupName

    Write-Host "Adding Az Cli required permissions to KeyVault" -ForegroundColor Yellow

    # 04b07795-8ddb-461a-bbee-02f9e1bf7b46 is AZ CLI client id
    az keyvault set-policy --name $params.keyvault --object-id 04b07795-8ddb-461a-bbee-02f9e1bf7b46 `
    --certificate-permissions get list create delete `
    --key-permissions get list create delete `
    --secret-permissions get set list delete
    
    $currentUserId = ((az ad signed-in-user show) | ConvertFrom-Json).objectId
    az keyvault set-policy --name $params.keyvault --object-id $currentUserId `
    --certificate-permissions get list create delete `
    --key-permissions get list create delete `
    --secret-permissions get set list delete
    
    Write-Host "=============================================================="
}

function New-AppInsights
{
    Write-Host "Creating App Insights";
    az monitor app-insights component create --app $params.appInsightsService --location $config.location  --resource-group $config.resourceGroupName

    Get-AppInsightsInstrumentationKey
}

# Create a technical storage account 
function New-TechnicalStorageAccount {

    Write-Host "Creating Technical Storage Account";

    az storage account create --name $params.techStorageAccountName `
    --location $config.location `
    --resource-group $config.resourceGroupName `
    --assign-identity `
    --allow-blob-public-access false `
    --sku Standard_LRS

    Get-TechStorageAccountParameters;
}

function New-DataStorageAccountAndContainer
{
    Write-Host "Creating Data Storage Account";

    az storage account create --name $params.dataStorageAccountName `
    --location $config.location `
    --resource-group $config.resourceGroupName `
    --sku Standard_LRS `
    --assign-identity `
    --allow-blob-public-access false `
    --enable-hierarchical-namespace true `
    --kind StorageV2
    
    Get-DataStorageAccountParameters; 

    # Iterate through the list of containers to create. 
    foreach ($container in $config.storageContainers) {
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

function New-CognitiveServices
{
    if ($config.cogServicesBundleDisabled) {

        # az cognitiveservices account list-kinds

        # Language 
        Write-Host "Creating TextAnalytics Cognitive Service";
        az cognitiveservices account create `
        -n $params.cogSvcLanguage `
        -g $config.resourceGroupName `
        --kind TextAnalytics `
        --sku S `
        --location $config.location `
        --yes

        az cognitiveservices account identity assign -n $params.cogSvcLanguage -g $config.resourceGroupName

        # Vision
        Write-Host "Creating ComputerVision Cognitive Service";
        az cognitiveservices account create `
        -n $params.cogSvcVision `
        -g $config.resourceGroupName `
        --kind ComputerVision `
        --sku S1 `
        --location $config.location `
        --yes

        az cognitiveservices account identity assign -n $params.cogSvcVision -g $config.resourceGroupName

        # Form
        Write-Host "Creating FormRecognizer Cognitive Service";
        az cognitiveservices account create `
        -n $params.cogSvcForm `
        -g $config.resourceGroupName `
        --kind FormRecognizer `
        --sku S0 `
        --location $config.location `
        --yes

        az cognitiveservices account identity assign -n $params.cogSvcForm -g $config.resourceGroupName

        # Translation
        Write-Host "Creating Translation Cognitive Service";
        az cognitiveservices account create `
        -n $params.cogSvcTranslate `
        -g $config.resourceGroupName `
        --kind TextTranslation `
        --sku S1 `
        --location $config.location `
        --yes

        az cognitiveservices account identity assign -n $params.cogSvcTranslate -g $config.resourceGroupName
        
    }
    else {
        Write-Host "Creating Bundle Cognitive Services";
        az cognitiveservices account create `
        -n $params.cogServicesBundle `
        -g $config.resourceGroupName `
        --kind CognitiveServices `
        --sku S0 `
        --location $config.location `
        --yes    

        az cognitiveservices account identity assign -n $params.cogServicesBundle -g $config.resourceGroupName
    }

    Get-CognitiveServiceKey;
}
function New-SearchServices
{
    Write-Host "Provisionning Search Service";

    $exists = az search service show --name $params.searchServiceName --resource-group $config.resourceGroupName --query id --out tsv

    if ( $exists ) {
        Write-Host "Search Service already exists...";
    }
    else {
        az search service create `
        --name  $params.searchServiceName `
        --resource-group $config.resourceGroupName `
        --sku $config.searchSku `
        --location $config.location `
        --partition-count 1 `
        --replica-count 1
    }

    az search service update --name  $params.searchServiceName --resource-group $config.resourceGroupName --identity-type "SystemAssigned"

    Get-SearchServiceKeys; 
}

function New-ACRService
{
    Write-Host "Provisionning ACR service";

    $exists = az acr show -g $config.resourceGroupName -n $params.acr_prefix --query id --out tsv

    if ( $exists ) {
        Write-Host "ACR service already exists...";
    }
    else {
        az acr create -g $config.resourceGroupName -n $params.acr_prefix --sku Premium --admin-enabled true --location $config.location

        az acr identity assign --identities '[system]' -g $config.resourceGroupName -n $params.acr_prefix
    }
}

function New-AzureMapsService()
{
    if ($config.mapSearchEnabled) {
        az maps account create --name $params.maps `
            --resource-group $config.resourceGroupName `
            --sku S0 `
            --subscription $config.subscriptionId `
            --accept-tos

        $mapsKey = az maps account keys list --name $params.maps --resource-group $config.resourceGroupName --query primaryKey --out tsv

        Add-Param "mapsSubscriptionKey" $mapsKey
    }
}

function New-BingSearchService()
{
    if ( $config.webSearchEnabled -or ($config.spellCheckEnabled -and $config.spellCheckProvider.Equals("Bing")) ) {
        Write-Host "Provision Bing Search service manually. When provisionned..." -ForegroundColor Red    
        $bingKey = Read-Host "Provide Bing Search Key " -MaskInput
        Add-Param "bingServicesKey" $bingKey
    }
}