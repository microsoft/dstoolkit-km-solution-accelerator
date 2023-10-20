#region ENVIRONMENT 
function Resolve-Environment {
    param (
        [string] $Name,
        [string] $WorkDir
    )
    
    if ($Name) {
        $testpath = join-path $HOME ("99-" + $Name)

        if (! (Test-Path $testpath)) {
            mkdir $testpath | Out-Null
        }
        return $(Resolve-Path $testpath).Path    
    }
    else {
        $testpath = $WorkDir
        
        return $(Resolve-Path $testpath).Path
    }
}

function Import-ServicesConfig() {

    $folders = Get-ChildItem -Directory -Path (join-path $global:envpath "config")

    foreach ($folder in $folders) {
        if (Test-Path $(join-Path $folder.FullName "config.json")) 
        {
            $varValue = [string] (Get-Content -Path $(join-path $folder.FullName "config.json"))
            $varValue = ConvertFrom-Json $varValue
            $varName = $folder.Name + "cfg"

            Set-Variable -Name $varName -Value $varValue -Visibility Public -Option AllScope -Force -Scope Global
    
            Add-Param -Name ($folder.Name+"Enabled") -value $varValue.enable

            if ($varValue.enable) {
                Import-ConfigParameters $varValue
            }
        }
    }
}

function Import-ConfigParameters ($inputcfg) {
    Write-Debug "Import configuration "
    # Automatically ad the services parameters to the global parameters variable. 
    if ($inputcfg.Parameters) {
        foreach ($entry in $(Get-Member -InputObject $inputcfg.Parameters -MemberType NoteProperty)) {
            $value = $entry.Name
            Add-Param $entry.Name $inputcfg.Parameters.$value
        }
        Save-Parameters
    }
}
function Get-Config() {
    param (
        [Parameter(Mandatory = $true)] [string] $Name,
        [string] $WorkDir,
        [switch] $Reload
    )

    if (! $WorkDir) {
        $WorkDir = "."
    }

    $global:config = Import-Config -Name $Name -WorkDir $WorkDir -Reload:$Reload

    $global:params = Import-Params -Reload:$Reload
    
    # Sync-Config
    # Sync-Parameters

    # Need to reload as configuration could have some parameterized arguments. 
    if ( $Reload ) {
        $global:config = Import-Config -Name $Name -WorkDir $WorkDir
    }
}

function Import-Config() {
    param (
        [string] $Name,
        [string] $WorkDir,
        [switch] $Reload
    )

    function Import-RawConfig {
        Write-Debug -Message "Load environment from deployment config"
        # Loading Environment Template Configurations
        $global:config = [string] (Get-Content -Path (join-path $WorkDir "config" ($Name + ".json")) -Raw)
        $global:config = ConvertFrom-Json $global:config
    }
    function Import-EnvironmentConfig {
        Write-Debug -Message "Load current environment configuration"
        # Loading Environment Current Configurations
        $global:config = [string] (Get-Content -Path (join-path $global:envpath "config.json") -Raw)
        $global:config = ConvertFrom-Json $global:config
    }

    $global:envpath = Resolve-Environment -Name $Name -WorkDir $WorkDir

    if ( Test-Path $global:envpath ) {
        if ( $Reload ) {
            Import-RawConfig
            Initialize-Config $WorkDir
        }
    }
    else {
        Import-RawConfig
        Initialize-Config $WorkDir
    }

    Import-EnvironmentConfig

    return $global:config
}

function ConvertTo-String {
    param(
        [Security.SecureString] $secureString
    )

    return [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR((($secureString))))
}

function Import-Params() {

    $global:params = New-Object -TypeName PSObject

    $parametersJson = join-path $global:envpath "parameters.json"

    if (Test-Path -Path $parametersJson) {
        # Loading Environment Parameters
        $readParams = [string] (Get-Content -Path $parametersJson -ErrorAction SilentlyContinue)

        if ($readParams) {
            if ($readParams.Length -gt 0) {
                $global:params = ConvertFrom-Json $readParams
            }
        }
    }
    
    if ($PSVersionTable.Platform -eq "Win32NT") {
        Write-Debug "Decrypt secured strings..."
        $parameterslist = Get-Member -InputObject $global:params -MemberType NoteProperty

        foreach ($prop in $parameterslist) {
            if ( Test-KeyVaultCandidate $prop.Name ) {
                $propValue = Get-ParamValue $prop.Name -AsSecureString
                if ($propValue) {
                    Add-Param $prop.Name (ConvertTo-String -secureString $propValue)
                }
            }
        }
    }

    return $global:params    
}

function Add-ExtendedParameters {
    param (
        [string] $source
    )

    $services = [string] (Get-Content -Path (join-path $global:envpath $source) -ErrorAction SilentlyContinue)
    $services = ConvertFrom-Json $services

    foreach ($property in $services.PSobject.Properties) {
        if ($property.TypeNameOfValue -eq "System.Management.Automation.PSCustomObject") {
            $subproperties = $property.Value.PSobject.Members
            foreach ($member in $subproperties) {
                if ($member.MemberType -eq "NoteProperty") {
                    Add-Param ($property.Name + "." + $member.Name) $member.Value   
                }
            }    
        }
        else {
            Add-Param $property.Name $property.Value        
        }
    }
}

function Get-Parameters {
    param (
        [string] $prefix
    )
    $values = @()
    foreach ($property in $global:params.PSobject.Properties) {

        if (($property.name.indexOf($prefix) -ge 0) -and ($property.name.indexOf("Key") -lt 0)) {
            $values += $property
        }
    }
    return $values
}

function Add-Param {
    param (
        [string] $Name,
        [object] $Value
    )
    if ( $global:params.PSobject.Properties.name -eq $Name) {
        $global:params.$Name = $Value
    }
    else {
        $global:params | Add-Member -MemberType NoteProperty -Name $Name -Value $Value -ErrorAction Ignore
    }
}

function Get-ParamValue() {
    param (
        [string] $name,
        [switch] $AsSecureString
    )
    $value = $global:params | Select-Object -ExpandProperty $name

    if ($value) {
        if ($AsSecureString) {
            $value = ConvertTo-SecureString -String $value
        }
    }

    return $value
}

function Save-Parameters {
    
    # Create a blank object 
    $securedparams = New-Object -TypeName PSObject

    if ($global:params) {
        $parameterslist = Get-Member -InputObject $global:params -MemberType NoteProperty

        foreach ($prop in $parameterslist) {
    
            $propValue = Get-ParamValue $prop.Name
            
            if ($PSVersionTable.Platform -eq "Win32NT") {
                if ( Test-KeyVaultCandidate $prop.Name ) {
                    if ($propValue) {
                        $propValue = ConvertTo-SecureString -String $propValue -AsPlainText -Force | ConvertFrom-SecureString
                    }
                }
            }
    
            $securedparams | Add-Member -MemberType NoteProperty -Name $prop.Name -Value $propValue -ErrorAction Ignore
        }    
    }

    $securedparams | Add-Member -MemberType NoteProperty -Name "LastModifiedDate" -Value (Get-Date) -ErrorAction Ignore

    $securedparams | ConvertTo-Json -Depth 100 -Compress | Out-File -FilePath (Join-Path $global:envpath "parameters.json") -Force
}
    
function Save-Config {
    $global:config | ConvertTo-Json -Depth 100 -Compress | Out-File -FilePath (Join-Path $global:envpath "config.json") -Force -Encoding utf8
}

function Sync-Config {

    Write-Debug -Message "Configuration synch starting... "

    $parameters = Get-Member -InputObject $global:config -MemberType NoteProperty
    
    $folders = @("config", "monitoring", "tests")
    
    foreach ($folder in $folders) {
        $templates = Get-ChildItem -File -Path (join-path $global:envpath $folder) -Recurse
        foreach ($temp in $templates) {
            $jsontemp = Get-Content -Path $temp.FullName
            foreach ($prop in $parameters) {
                $propValue = $global:config | Select-Object -ExpandProperty $prop.Name
                $jsontemp = $jsontemp -replace ("{{config." + $prop.Name + "}}"), $propValue 
            }
            $jsontemp | Out-File -FilePath $temp.FullName -Force
        }    
    }
    
    $files = Get-ChildItem $global:envpath -Filter *.json
    
    foreach ($file in $files) {
        $jsontemp = Get-Content -Path $file.FullName
        foreach ($prop in $parameters) {
            $propValue = $global:config | Select-Object -ExpandProperty $prop.Name
            $jsontemp = $jsontemp -replace ("{{config." + $prop.Name + "}}"), $propValue 
        }
        $jsontemp | Out-File -FilePath $file.FullName -Force
    }
    
    Sync-Parameters

    Write-Debug -Message "Configuration synch completed."
}

function Sync-Parameters {

    Save-Parameters
    
    if ($global:params) {
        $parameterslist = Get-Member -InputObject $global:params -MemberType NoteProperty
    
        $folders = @("config", "monitoring", "tests")
        
        foreach ($folder in $folders) {
            $templates = Get-ChildItem -File -Path (join-path $global:envpath $folder) -Recurse
            foreach ($temp in $templates) {
                $jsontemp = Get-Content -Path $temp.FullName
                foreach ($prop in $parameterslist) {
                    $propValue = Get-ParamValue $prop.Name
                    # $propValue = $global:params | Select-Object -ExpandProperty $prop.Name
                    $jsontemp = $jsontemp -replace ("{{param." + $prop.Name + "}}"), $propValue 
                }
                $jsontemp | Out-File -FilePath $temp.FullName -Force
            }
        }
        Write-Debug -Message "Parameters synched"
    }

    Import-ServicesConfig

    Initialize-StorageConfig

    Initialize-SearchConfig

}

function Initialize-StorageConfig {
    # first Container is designated as data container
    $dataStorageContainerName = $params.storageContainers[0];
    Add-Param "dataStorageContainerName" $dataStorageContainerName

    Add-Param "StorageContainersAsString" $([String]::Join(',', $params.storageContainers))

    # Create the containers entries for UI SAS access
    $StorageContainerAddresses = @()
    foreach ($container in $params.storageContainers) {
        $url = "https://" + $global:params.dataStorageAccountName + ".blob.core.windows.net/" + $container
        $StorageContainerAddresses += $url
    }
    Add-Param "StorageContainerAddresses" $StorageContainerAddresses
    Add-Param "StorageContainerAddressesAsString" $([String]::Join(',', $StorageContainerAddresses))
}

function Sync-Modules {
    $folders = @("modules", "scripts")

    foreach ($folder in $folders) {
        $modulepath = Join-Path $global:envpath $folder
        if (! (Test-Path $modulepath)) {
            mkdir $modulepath | Out-Null
        }
        Copy-Item -Path ("..\deployment\" + $folder + "\*") -Destination $modulepath"\" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Debug -Message $folder
    }
}

function Get-DeploymentOverlayPath {
    param (
        [string] $relpath
    )
    
    # Default override path under the Deployment folder
    $overridepath = join-path $global:workpath "config" $config.id  $relpath "*"
    
    if ($config.overlayPath) {
        Write-Debug -Message ("Using configured overlay path " + $config.overlayPath)
        $overridepath = join-path $global:workpath $config.overlayPath $relpath "*"
    }
    
    return $overridepath
}
    
function Initialize-Config() {
    param (
        [string] $WorkDir
    )
        
    Save-Config 
        
    # Config
    $configpath = $global:envpath
    
    if (! (Test-Path $configpath)) {
        mkdir $configpath | Out-Null
    }
    
    # Save the working directory 
    $global:workpath = $WorkDir
    
    $originalConfigPath = (join-path $WorkDir ".." "configuration" "*")
    Copy-Item -Path $originalConfigPath -Destination $configpath"\" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Debug -Message ("Config created and copied on " + $configpath)
        
    # Override - Only relevant when initializing a deployment
    $overridepath = Get-DeploymentOverlayPath "configuration"
    
    if ( test-path $overridepath) {
        Copy-Item -Path $overridepath -Destination $configpath"\" -Recurse -Force
    }
    
    # Sync-Config
        
    $releasePath = join-path $WorkDir "releases"
    if ( Test-Path $releasePath ) {
        Copy-Item -Path $releasePath -Destination $global:envpath -Filter "*.publish.latest.zip" -Recurse -Force
        Write-Host "Functions Releases copied." -ForegroundColor DarkYellow    
    }
        
    $datapath = join-Path $global:envpath "data"
    if (-Not (Test-Path $datapath)) {
        mkdir $datapath | Out-Null
    }
    $overridepath = Get-DeploymentOverlayPath "data"
    
    if ( test-path $overridepath) {
        Copy-Item -Path $overridepath -Destination (join-path $datapath "\") -Recurse -Force
    }
    else {
        Copy-Item -Path (join-path $WorkDir ".." "data" "*") -Destination (join-path $datapath "\") -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    Sync-Modules
}
    
#endregion 
    
#region DATA

function Push-Data() {
    param (
        [Parameter(Mandatory = $true)]
        [string] $container,
        [string] $sourcepath
    )
    
    if (! $sourcepath) {
        $sourcepath = join-Path $global:envpath "data" $container
    }
    Write-Host ("Upload documents from path " + $sourcepath) -ForegroundColor DarkYellow
    
    # Upload documents
    az storage blob upload-batch `
        --account-name $params.dataStorageAccountName `
        --account-key $params.storageAccountKey  `
        --overwrite `
        -d $container `
        -s $sourcepath
}
    
function Get-ContainerFilesList () {
    param (
        [Parameter(Mandatory = $true)]
        [string] $container,
        [Parameter(Mandatory = $false)]
        [string] $path
    )
    
    $files = @()
    
    if ($path) {
        $files = az storage fs file list --path $path `
            --file-system $container `
            --recursive `
            --account-name $params.dataStorageAccountName `
            --account-key $params.storageAccountKey `
            --exclude-dir `
            --query "[].{name:name}" `
            --output tsv
    }
    else {
        $files = az storage fs file list `
            --file-system $container `
            --recursive `
            --account-name $params.dataStorageAccountName `
            --account-key $params.storageAccountKey `
            --exclude-dir `
            --query "[].{name:name}" `
            --output tsv    
    }
    
    return $files
}

function Add-BlobRetryTag () {
    param (
        [Parameter(Mandatory = $true)]
        [string] $container,
        [Parameter(Mandatory = $true)]
        [string] $path
    )
    
    $now = Get-Date -Format "yyyyMMddHHmmss"
    
    az storage fs file metadata update `
        --file-system $container `
        --path $path `
        --account-name $params.dataStorageAccountName `
        --account-key $params.storageAccountKey  `
        --metadata AzureSearch_RetryTag=$now
    
}
#endregion

#region Service Keys 
function Get-AllServicesKeys() {
    param (
        [switch] $AddToKeyVault
    )
    Get-AppInsightsInstrumentationKey
    Get-TechStorageAccountAccessKeys
    Get-DataStorageAccountAccessKeys
    Get-CognitiveServiceKey
    Get-AzureMapsSubscriptionKey
    Get-FunctionsKeys
    Get-SearchServiceKeys
    Get-OpenAIKey

    Sync-Config

    if ($AddToKeyVault) {
        Add-KeyVaultSecrets
    }
}

function Get-AppInsightsInstrumentationKey {
    $tuples = Get-Parameters "appInsightsService"
    
    foreach ($tuple in $tuples) {
        $key = az monitor app-insights component show --app $tuple.Value -g $config.resourceGroupName --query instrumentationKey  --out tsv
    
        if ( $key -and $key.length -gt 0 ) {
            Add-Param "APPINSIGHTS_INSTRUMENTATIONKEY" $key
        }            
    }
    Save-Parameters  
}
    
function Get-TechStorageAccountAccessKeys {    
    $techStorageAccountKey = az storage account keys list --account-name $params.techStorageAccountName -g $config.resourceGroupName --query [0].value  --out tsv
        
    $techStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=' + $params.techStorageAccountName + ';AccountKey=' + $techStorageAccountKey + ';EndpointSuffix=core.windows.net'
    Add-Param "techStorageConnString" $techStorageConnectionString
    
    Save-Parameters
}

function Get-DataStorageAccountAccessKeys {
    
    $storageAccountKey = az storage account keys list --account-name $params.dataStorageAccountName -g $config.resourceGroupName --query [0].value --out tsv
    Add-Param "storageAccountKey" $storageAccountKey
    
    $storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=' + $params.dataStorageAccountName + ';AccountKey=' + $storageAccountKey + ';EndpointSuffix=core.windows.net'
    Add-Param "storageConnectionString" $storageConnectionString
    
    Save-Parameters
}

function Add-DataStorageRBAC {    
    $scope = "/subscriptions/" + $config.subscriptionId + "/resourcegroups/" + $config.resourceGroupName + "/providers/Microsoft.Storage/storageAccounts/" + $params.dataStorageAccountName
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            $principalId = az webapp identity show -n $webApp.Name -g $plan.ResourceGroup --query principalId --out tsv  
            az role assignment create --role "Storage Blob Data Contributor" --assignee $principalId --scope $scope
            az role assignment create --role "Storage Blob Delegator" --assignee $principalId --scope $scope
        }
    }

    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.Services) {          
            $principalId = az functionapp identity show -n $functionApp.Name -g $plan.ResourceGroup --query principalId --out tsv    
            az role assignment create --role "Storage Blob Data Contributor" --assignee $principalId --scope $scope
        }
    }

    $currentUserId = ((az ad signed-in-user show) | ConvertFrom-Json).id
    az role assignment create --role "Storage Blob Data Contributor" --assignee $currentUserId --scope $scope
    az role assignment create --role "Storage Blob Delegator" --assignee $currentUserId --scope $scope
}
    
function Get-CognitiveServiceKey {
    
    foreach ($azureResource in $cogservicescfg.Items) {
        Write-Host "Checking Cognitive Service existence "$azureResource.Name

        $exists = az cognitiveservices account show --name $azureResource.Name --resource-group $azureResource.ResourceGroup --query id --out tsv

        if ( $exists ) {
            Write-Host "Fetching Cognitive Service key "$azureResource.Name

            $cogServicesKey = az cognitiveservices account keys list --name $azureResource.Name -g $azureResource.ResourceGroup --query key1 --out tsv

            if ( $cogServicesKey -and $cogServicesKey.Length -gt 0 ) {
                Add-Param ($azureResource.Parameter+"Key") $cogServicesKey
            }
        }
    }

    Save-Parameters
}
    
function Get-AzureMapsSubscriptionKey {
    
    if ($mapscfg.enable) {
        $mapsKey = az maps account keys list --name $params.maps --resource-group $config.resourceGroupName --query primaryKey --out tsv
        Add-Param "MapConfig--AzureMapsSubscriptionKey" $mapsKey
    
        Save-Parameters
    }
}
#endregion

#region Azure Cognitive Search 
    
function Initialize-SearchConfig {

    if ($searchcfg.searchBlobPartitions) {
        Write-Host "Blob partitionning enabled ..."
        for ($i = 0; $i -lt $searchcfg.searchBlobPartitions.Count; $i++) {
            $partitionName = $searchcfg.searchBlobPartitions[$i]
    
            $indexerPath = join-path $global:envpath "config" "search" "indexers" "documents.json"
            if ( test-path $indexerPath) {
                # Create a partition datasource for documents
                $datasource = Get-Content -Path (join-path $global:envpath "config" "search" "datasources" "documents.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ("documents-" + $i)
                $jsonobj.container | Add-Member -MemberType NoteProperty -Name "query" -Value $partitionName -ErrorAction Ignore
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "config" "search" "datasources" ("documents-" + $i + ".json")) -Force
    
                # Create a partition indexer for documents
                $datasource = Get-Content -Path (join-path $global:envpath "config" "search" "indexers" "documents.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ("documents-" + $i)
                $jsonobj.dataSourceName = ("documents-" + $i)
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "config" "search" "indexers" ("documents-" + $i + ".json")) -Force    
            }
    
            $indexerPath = join-path $global:envpath "config" "search" "indexers" "images.json"
            if ( test-path $indexerPath) {
                # Create a partition datasource for images
                $datasource = Get-Content -Path (join-path $global:envpath "config" "search" "datasources" "images.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ("images-" + $i)
                $jsonobj.container | Add-Member -MemberType NoteProperty -Name "query" -Value $partitionName -ErrorAction Ignore
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "config" "search" "datasources" ("images-" + $i + ".json")) -Force
    
                # Create a partition indexer for images
                $datasource = Get-Content -Path (join-path $global:envpath "config" "search" "indexers" "images.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ("images-" + $i)
                $jsonobj.dataSourceName = ("images-" + $i)
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "config" "search" "indexers" ("images-" + $i + ".json")) -Force
    
            }
        }
        Remove-item (join-path $global:envpath "config" "search" "indexers" "documents.json") -Force -ErrorAction SilentlyContinue
        Remove-Item (join-path $global:envpath "config" "search" "indexers" "images.json") -Force -ErrorAction SilentlyContinue
    }
    
    Initialize-SearchParameters    
}
    
function Initialize-SearchParameters {
    
    Write-Debug -Message "Create/Update Search Configuration"
    
    # Get the list of Synonyms Maps
    $synonymmaps = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "synonyms") -Recurse
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($item)
        Add-Param $item"SynonymMap" $value
        $synonymmaps += $value
    }
    Add-Param "searchSynonymMaps" $synonymmaps
    Write-Debug -Message "Parameters Synonyms created"
    
    # Get the list of SkillsSets
    $skillslist = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "skillsets")
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($item)
        Add-Param $item"SkillSet" $value
        $skillslist += $value
    }
    Add-Param "searchSkillSets" $skillslist
    Write-Debug -Message "Parameters SkillSet created"
    
    # Get the list of Indexes
    $indexeslist = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexes")
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($item)
        Add-Param "indexName" $value
        $indexeslist += $value
    }
    Add-Param "searchIndexes" ($indexeslist | Join-String -Property $_ -Separator ",")
    Add-Param "searchIndexesList" $indexeslist
    Write-Debug -Message "Parameters Indexes created"
    
    # Get the list of DataSources
    $datasourceslist = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "datasources")
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($item)
        Add-Param $item"DataSource" $value
        Add-Param $item"StorageContainerName" $item            
        $datasourceslist += $value
    }
    Add-Param "searchDataSources" $datasourceslist
    Write-Debug -Message "Parameters DataSources created"
    
    # Get the list of Indexers
    $indexersList = @()
    $indexersStemList=@()

    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexers")
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($item)
        Add-Param $item"Indexer" $value
        $indexersList += $value
        $indexersStemList += $item
    }
    Add-Param "searchIndexers" ($indexersList | Join-String -Property $_ -Separator ",")
    Add-Param "searchIndexersList" $indexersList
    Add-Param "searchIndexersStemList" $indexersStemList
    Write-Debug -Message "Parameters Indexers created"
}
    
function Get-SearchServiceKeys {    
    Write-Host "Fetching Search service keys..." -ForegroundColor DarkBlue

    # Still used during deployment
    $searchServiceKey = az search admin-key show --resource-group $config.resourceGroupName --service-name $params.searchServiceName  --query primaryKey --out tsv
    Add-Param "searchServiceK" $searchServiceKey
        
    # Semantic search is currently using REST API, to be removed once migrate to SDK    
    $searchServiceQueryKey = az search query-key list --resource-group $config.resourceGroupName --service-name $params.searchServiceName  --query [0].key --out tsv
    Add-Param "SearchServiceConfig--QueryKey" $searchServiceQueryKey
   
    Save-Parameters
}

function Add-SearchRBAC {
    # Improve this limiting only to API
    $searchScope = "/subscriptions/" + $config.subscriptionId + "/resourcegroups/" + $config.resourceGroupName + "/providers/Microsoft.Search/searchServices/" + $params.searchServiceName
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            $principalId = az webapp identity show -n $webApp.Name -g $plan.ResourceGroup --query principalId --out tsv  
            az role assignment create --role "Search Service Contributor" --assignee $principalId --scope $searchScope
            az role assignment create --role "Search Index Data Contributor" --assignee $principalId --scope $searchScope
        }
    }

    $currentUserId = ((az ad signed-in-user show) | ConvertFrom-Json).id
    az role assignment create --role "Search Service Contributor" --assignee $currentUserId --scope $searchScope
    az role assignment create --role "Search Index Data Contributor" --assignee $currentUserId --scope $searchScope
}

function Invoke-SearchAPI {
    param (
        [string]$url,
        [string]$body,
        [string]$method = "PUT"
    )
    
    if (! $params.searchServiceK) {
        Get-SearchServiceKeys
    }

    $headers = @{
        'api-key'      = $params.searchServiceK
        'Content-Type' = 'application/json'
        'Accept'       = 'application/json'
    }
    $baseSearchUrl = "https://" + $params.searchServiceName + ".search.windows.net"
    $fullUrl = $baseSearchUrl + $url
    
    Write-Host -Message ("Calling Search API " + $method + ": '" + $fullUrl + "'")
    
    Invoke-RestMethod -Uri $fullUrl -Headers $headers -Method $method -Body $body | ConvertTo-Json -Depth 100
}

function Get-SearchMgtUrl () {
    $mgturl = "https://management.azure.com/subscriptions/" + $config.subscriptionId + "/resourceGroups/" + $config.resourceGroupName + "/providers/Microsoft.Search/searchServices/" + $params.searchServiceName
    $mgturl += "?api-version="+$searchcfg.Parameters.searchManagementVersion
    return $mgturl
}

function Initialize-Search {
    param (
        [switch]$AllowIndexDowntime
    )
    
    Write-Debug -Message "Create/Update Search Components"
    
    Update-SearchSynonyms
    Update-SearchIndex -AllowIndexDowntime:$AllowIndexDowntime
    Update-SearchDataSource
    Update-SearchSkillSet
    Update-SearchIndexer
    
    Update-SearchAliases
}

function Update-SearchAliases {
    param (
        [ValidateSet("PUT", "DELETE")]
        [string]$method = "PUT"
    )
    Write-Debug -Message "Creating/Updating existing Search Aliases"
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "aliases") -Recurse
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
        Invoke-SearchAPI -url ("/aliases/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion) -body $configBody -method $method
    }
}

function Update-SearchSynonyms {
    param (
        [ValidateSet("PUT", "DELETE")]
        [string]$method = "PUT"
    )
    Write-Debug -Message "Creating/Updating existing Search Synonym Map(s)"
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "synonyms") -Recurse
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
        Invoke-SearchAPI -url ("/synonymmaps/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion) -body $configBody -method $method
    }
}

function Update-SearchIndex {
    param (
        [string]$name,
        [switch]$AllowIndexDowntime
    )
    Write-Debug -Message "Creating/Updating existing Search Index(es)"
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexes") -Recurse
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody

        if ( $name ) {
            if ($jsonobj.name.indexOf($name) -ge 0) {
                Invoke-SearchAPI -url ("/indexes/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion + "&allowIndexDowntime=" + $AllowIndexDowntime) -body $configBody
            }    
        }
        else {
            Invoke-SearchAPI -url ("/indexes/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion + "&allowIndexDowntime=" + $AllowIndexDowntime) -body $configBody
        }
    }
}

    
function Update-SearchDataSource {
    param (
        [string]$name
    )
    Write-Debug -Message "Creating/Updating existing Search DataSource(s)"
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "datasources")
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody

        if ( $name ) {
            if ($jsonobj.name.indexOf($name) -ge 0) {
                Invoke-SearchAPI -url ("/datasources/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion) -body $configBody
            }    
        }
        else {
            Invoke-SearchAPI -url ("/datasources/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion) -body $configBody
        }
    }
}
    
function Update-SearchSkillSet {
    param (
        [string]$name
    )
    Write-Debug -Message "Creating/Updating existing Search SkillSet(s)"
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "skillsets")
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody

        if ( $name ) {
            if ($jsonobj.name.indexOf($name) -ge 0) {
                Invoke-SearchAPI -url ("/skillsets/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion) -body $configBody
            }    
        }
        else {
            Invoke-SearchAPI -url ("/skillsets/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion) -body $configBody
        }
    }
}
    
# https://docs.microsoft.com/en-us/rest/api/searchservice/preview-api/create-indexer
# https://docs.microsoft.com/en-us/rest/api/searchservice/update-indexer
function Update-SearchIndexer {
    param (
        [string]$name
    )
    Write-Debug -Message "Creating/Updating existing Search Indexer(s)"
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexers")
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
    
        if ( $name ) {
            if ($jsonobj.name.indexOf($name) -ge 0) {
                Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion) -body $configBody
            }    
        }
        else {
            if ($jsonobj.name.indexOf("spo") -ge 0) {
                Write-Host "Skipping SharePoint Indexer re-configuration." -ForegroundColor DarkRed
            }
            else {
                Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion) -body $configBody
            }    
        }
    }
}

    
# https://docs.microsoft.com/en-us/rest/api/searchservice/reset-indexer
    
# POST https://[service name].search.windows.net/indexers/[indexer name]/reset?api-version=[api-version]  
#   Content-Type: application/json  
#   api-key: [admin key]
    
function Reset-SearchIndexer {
    param (
        [string]$name
    )
    Write-Host "Reset Search Indexer(s)"
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexers")
    
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
    
        if ( $name ) {
            if ( $jsonobj.name.indexOf($name) -ge 0) {
                Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "/reset?api-version=" + $searchcfg.Parameters.searchVersion) -method "POST"
            }    
        }
        else {
            Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "/reset?api-version=" + $searchcfg.Parameters.searchVersion) -method "POST"
        }
    }
};
    
# https://docs.microsoft.com/en-us/rest/api/searchservice/run-indexer
    
# POST https://[service name].search.windows.net/indexers/[indexer name]/run?api-version=[api-version]  
#   Content-Type: application/json  
#   api-key: [admin key]
    
function Start-SearchIndexer {
    param (
        [string]$name
    )
    Write-Host "Run Search Indexer(s) "$name
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexers")
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
    
        if ( $name ) {
            if ( $jsonobj.name.indexOf($name) -ge 0) {
                Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "/run?api-version=" + $searchcfg.Parameters.searchManagementVersion) -method "POST" 
            }
        }
        else {
            Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "/run?api-version=" + $searchcfg.Parameters.searchManagementVersion) -method "POST" 
        }
    }
};
    
function Get-SearchIndexersStatus {
    $indexersStatus = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexers")
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
        $status = Invoke-SearchAPI -Method GET -url ("/indexers/" + $jsonobj.name + "/status?api-version=" + $searchcfg.Parameters.searchVersion)
        $status = ConvertFrom-Json $status
        $properties = @{ name = $status.name
            status            = $status.status
            lastStatus        = $status.lastResult.status
            itemsProcessed    = $status.lastResult.itemsProcessed
            itemsFailed       = $status.lastResult.itemsFailed
            startTime         = $status.lastResult.startTime
            endTime           = $status.lastResult.endTime
        }
        $indexersStatus += New-Object psobject -Property $properties
    }
    
    $indexersStatus | format-table -AutoSize
}
    
function Get-SearchIndexer {
    param (
        [string]$item
    )
    
    if ($item) {
        $indexercfg = join-path $global:envpath $("\config\search\indexers\" + $item + ".json")
    
        if (Test-Path -Path $indexercfg) {
            $indexerBody = [string] (Get-Content -Path $indexercfg)
            $jsonobj = ConvertFrom-Json $indexerBody
            $status = Invoke-SearchAPI -Method GET -url ("/indexers/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion)    
            return $(ConvertFrom-Json $status)    
        }
        else {
            Write-Host "Indexer name is incorrect. Please recheck.";
        }
    }
    else {
        Write-Host "Please provide an indexer name.";
    }
}
    
function Get-SearchIndexerStatus {
    param (
        [string]$item
    )
    
    if ($item) {
        $indexerBody = [string] (Get-Content -Path (join-path $global:envpath $("\config\search\indexers\" + $item + ".json")))
        $jsonobj = ConvertFrom-Json $indexerBody
        $status = Invoke-SearchAPI -Method GET -url ("/indexers/" + $jsonobj.name + "/status?api-version=" + $searchcfg.Parameters.searchVersion)    
        return $(ConvertFrom-Json $status)
    }
    else {
        Write-Host "Please provide an indexer name.";
    }
}
    
function Get-SearchServiceDetails() {
    az rest --method GET --url $(Get-SearchMgtUrl)
}
    
# https://docs.microsoft.com/en-us/rest/api/searchservice/preview-api/reset-documents
    
# POST https://[service name].search.windows.net/indexers/[indexer name]/resetdocs?api-version=[api-version]
#     Content-Type: application/json
#     api-key: [admin key]
    
function Reset-SearchDocument {
    param (
        [string]$key
    )
    Write-Host "Reset Search Document(s) "$key
    
    $body = @{
        'documentKeys' = @($key)
    }
    Invoke-SearchAPI -url ("/indexers/documents/resetdocs?api-version=" + $searchcfg.Parameters.searchVersion) -method "POST" -body $body
}

# https://learn.microsoft.com/en-us/azure/search/semantic-search-overview

function Enable-SemanticSearch ($searchServiceName) {

    $mgturl = ("https://management.azure.com/subscriptions/" + $config.subscriptionId + "/resourceGroups/" + $config.resourceGroupName + "/providers/Microsoft.Search/searchServices/")
    if ($searchServiceName) {
        $mgturl += $searchServiceName
    }
    else {
        $mgturl += $params.searchServiceName
    }
    $mgturl += "?api-version=2021-04-01-Preview"

    Push-Location (Join-Path $global:envpath "config" "search" "semantic")
    az rest --method PUT --url $mgturl --body '@enable.json'
    Pop-Location
}

function Disable-SemanticSearch ($searchServiceName) {
    $mgturl = ("https://management.azure.com/subscriptions/" + $config.subscriptionId + "/resourceGroups/" + $config.resourceGroupName + "/providers/Microsoft.Search/searchServices/")
    if ($searchServiceName) {
        $mgturl += $searchServiceName
    }
    else {
        $mgturl += $params.searchServiceName
    }
    $mgturl += "?api-version=2021-04-01-Preview"

    Push-Location (Join-Path $global:envpath "config" "search" "semantic")
    az rest --method PUT --url $mgturl --body '@disable.json'
    Pop-Location
}

function Suspend-Search {
    # Suspend the indexers default scheduling.
    # https://learn.microsoft.com/en-us/azure/search/search-howto-schedule-indexers?tabs=rest#configure-a-schedule
    Write-Host "Suspend Indexers' Scheduling..."

    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexers")
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
        $jsonobj.schedule=$null
        $updatedCfg=(Convertto-json $jsonobj -Depth 100)
        Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "?api-version=" + $searchcfg.Parameters.searchVersion) -method "PUT" -body $updatedCfg
    }
}

function Resume-Search {
    Sync-Config
    Sync-Parameters
    Initialize-Search
}

function Get-SearchFailedItems {
    param(
        [string] $Indexer,
        [switch] $SkipHistory,
        [switch] $Tagging
    )

    function Format-ErrorKey ($error) {
        $id = $error.key.Split("&")[0].Replace("localId=", "");
        $id = [System.Web.HttpUtility]::UrlDecode($id);
        # Find the base url of the document url to remove it.
        foreach($storageUrl in $params.StorageContainerAddresses)
        {
            if ($id.indexOf($storageUrl) -ge 0)
            {
                $baseurl=$storageUrl
            }
        }
        return [System.Web.HttpUtility]::UrlDecode($id.Replace($baseurl+"/",""));
    }

    $now = Get-Date -Format "yyyyMMddHHmmss"

    if ($Indexer) {
        $indexersTargetList = @($Indexer)
    }
    else {
        $indexersTargetList = $params.searchIndexersStemList
    }

    foreach ($container in $indexersTargetList) {

        Write-Host "-------------------"
        Write-Host "Indexer "$container -ForegroundColor DarkCyan

        $status = Get-SearchIndexerStatus $container
        
        $filestotag = @() 

        $errors = $status.lastResult.errors

        # Latest Errors
        foreach ($error in $errors) {
            $filestotag += Format-ErrorKey $error
        }

        # Executions History
        if (-not $SkipHistory)
        {
            $executions = $status.executionHistory

            # $executions | format-table -AutoSize

            foreach ($exec in $executions) {
            
                if ( $exec.itemsFailed -gt 0) {
                    Write-Host ("failed "+$exec.startTime+" "+$exec.endTime+" "+$exec.itemsProcessed+" "+$exec.itemsFailed) -ForegroundColor DarkMagenta
                }
                else {
                    Write-Host ($exec.status+" "+$exec.startTime+" "+$exec.endTime+" "+$exec.itemsProcessed+" "+$exec.itemsFailed) -ForegroundColor DarkGreen
                }
        
                if ( $exec.status -eq "reset" ) {
                    break
                }
                else {
                    foreach ($error in $exec.errors) {
                        $filestotag += Format-ErrorKey $error
                    }
                }
            }
        }

        $filestotag = $filestotag | Sort-Object -Unique 

        Write-Host "Found "$filestotag.Length" documents in errors." -ForegroundColor DarkCyan

        $filestotag | Format-Table -AutoSize

        if ($Tagging) {
            foreach ($file in $filestotag) {
                Write-Host "Tagging "$file -ForegroundColor DarkYellow
                Add-BlobRetryTag -container $container -path $file
            }
        }
    }
}

#endregion
 
#region App Service Plan
function Test-AppPlanExistence {
    param (
        [string]$aspName
    )
    $planCheck = az appservice plan list --query "[?name=='$aspName']" | ConvertFrom-Json
    
    return ($planCheck.Length -gt 0)
}
#endregion
    
#region Helpers 
function Test-FileExistence { 
    param (
        [string]$path
    )
    return Test-Path $path
}
    
function Test-DirectoryExistence { 
    param (
        [string]$path
    )
    if (!(Test-Path $path)) {
        mkdir $path | Out-Null
    }
    
    return $path
}
function Compress-Release() {
    param (
        [string]$deploymentdir,
        [string]$now
    )
    
    $releasePath = Join-Path $global:envpath "releases"
    Test-DirectoryExistence $releasePath
    
    Test-DirectoryExistence (join-path $releasePath "windows")
    Test-DirectoryExistence (join-path $releasePath "linux")
    Test-DirectoryExistence (join-path $releasePath "webapps")
     
    $releases = Get-ChildItem -Directory $deploymentdir -Recurse | Where-Object { $_.Name -match $now }
          
    foreach ($release in $releases) {
        Write-host "Zipping "$release.Name
        $reldestpath = join-path $deploymentdir ($release.Name + ".zip")
        Push-Location $release.FullName
        Compress-Archive -Path ".\*" -DestinationPath $reldestpath -Force
        Pop-Location
    
        $zipname = ((Join-Path $releasePath $release.Parent.Name $release.Name.Replace($now, "")) + "latest.zip")
        Write-Host $zipname
        Copy-Item $reldestpath $zipname -Force    
    }
}
#endregion
    
#region Azure Functions 
function Test-FunctionExistence {
    param (
        [string]$aspName
    )
    $exists = az functionapp list --query "[?name=='$aspName']" | ConvertFrom-Json
    
    return ($exists.Length -gt 0)
}
    
function Get-AzureFunctionFiles() {
    param (
        [string] $srcPath
    )
    
    $excludeFiles = @('local.settings.json', '.gitignore', '*.code-workspace')
        
    return Get-ChildItem -Path $srcPath -Exclude $excludeFiles | Where-Object { ! $_.PSIsContainer }
}
function Get-AzureFunctionFolders() {
    param (
        [string] $srcPath
    )
    
    $excludeFolders = @('.venv', '.vscode', '__pycache__', 'tests', 'entities', 'docs', 'notebooks')
    $excludeFoldersRegex = $excludeFolders -join '|'
        
    return Get-ChildItem -Path $srcPath -Directory -Exclude $excludeFolders | Where-Object { $_.FullName.Replace($srcPath, "") -notmatch $excludeFoldersRegex }
}
    
function New-Functions {
    foreach ($plan in $functionscfg.AppPlans) {
        Write-Host "Plan "$plan.Name
    
        # Consumption Plan Support
        $consumption = ($plan.Sku -eq "Y1")
    
        if (! $consumption) {
            if ($plan.IsLinux) {
                if (!(Test-AppPlanExistence $plan.Name)) {
                    # Create a Linux plan
                    az functionapp plan create `
                        --name $plan.Name `
                        --resource-group $plan.ResourceGroup `
                        --location $config.location `
                        --sku $plan.Sku `
                        --is-linux true `
                        --number-of-workers 1 
                }
            }
            else {
                if (!(Test-AppPlanExistence $plan.Name)) {
                    # Create a Non-Linux plan
                    az functionapp plan create `
                        --name $plan.Name `
                        --resource-group $plan.ResourceGroup `
                        --location $config.location `
                        --sku $plan.Sku
                }
            }
        }
        else {
            Write-Host "Consumption Plan. Skipping App Service Plan creation."
        }
    
        foreach ($functionApp in $plan.Services) {
            # Linux-based function
            if ($plan.IsLinux) {
                if (!(Test-FunctionExistence $functionApp.Name)) {
                    if ($consumption) {
                        # Create a Function App
                        az functionapp create --name $functionApp.Name `
                            --storage-account $params.techStorageAccountName `
                            --consumption-plan-location $config.location `
                            --resource-group $plan.ResourceGroup `
                            --os-type Linux `
                            --app-insights $params.appInsightsService `
                            --https-only true `
                            --runtime python `
                            --runtime-version $functionApp.PythonVersion `
                            --functions-version $functionApp.Version
                    }
                    else {
                        if ($functionApp.Image) {
                            # $imageName = $params.acr + "/" + $functionApp.Image
                            # Image names must be prefixed with server 
                            # local repository is $params.acr or {{config.name}}acr.azurecr.io

                            # Create a Function App
                            az functionapp create --name $functionApp.Name `
                                --storage-account $params.techStorageAccountName `
                                --plan $plan.Name `
                                --resource-group $plan.ResourceGroup `
                                --functions-version $functionApp.Version `
                                --os-type Linux `
                                --https-only true `
                                --app-insights $params.appInsightsService `
                                --deployment-container-image-name $functionApp.Image     

                            az functionapp config set --always-on true --name $functionApp.Name --resource-group $plan.ResourceGroup              
                        }
                        else {
                            az functionapp create --name $functionApp.Name `
                                --storage-account $params.techStorageAccountName `
                                --plan $plan.Name `
                                --resource-group $plan.ResourceGroup `
                                --functions-version $functionApp.Version `
                                --os-type Linux `
                                --https-only true `
                                --app-insights $params.appInsightsService `
                                --runtime python `
                                --runtime-version $functionApp.PythonVersion `
                                --functions-version $functionApp.Version                            
                        }
                    }
                }
    
                $storekey = $params.techStorageConnString
    
                az functionapp config appsettings set `
                    --name $functionApp.Name `
                    --resource-group $plan.ResourceGroup `
                    --settings AzureWebJobsStorage=$storekey
            }
            else {
                # Windows-based function
                if (!(Test-FunctionExistence $functionApp.Name)) {
                    if ($consumption) {
                        # Create a Function App service
                        az functionapp create --name $functionApp.Name `
                            --storage-account $params.techStorageAccountName `
                            --consumption-plan-location $config.location `
                            --resource-group $plan.ResourceGroup `
                            --functions-version $functionApp.Version `
                            --https-only true `
                            --app-insights $params.appInsightsService
                    }
                    else {
                        # Create a Function App service
                        az functionapp create --name $functionApp.Name `
                            --storage-account $params.techStorageAccountName `
                            --plan $plan.Name `
                            --resource-group $plan.ResourceGroup `
                            --functions-version $functionApp.Version `
                            --https-only true `
                            --app-insights $params.appInsightsService                        
                    }
                }
    
                # For Windows function apps only, also enable .NET 6.0 that is needed by the runtime
                az functionapp config set --net-framework-version $functionApp.DotnetVersion --resource-group $plan.ResourceGroup --name $functionApp.Name

                # Use 64 bits worker process
                az functionapp config set -g $plan.ResourceGroup -n $functionApp.Name --use-32bit-worker-process false
            }
    
            # Assign a system managed identity
            az functionapp identity assign -g $plan.ResourceGroup -n $functionApp.Name
    
            # FTP State to FTPS Only
            az functionapp config set -g $plan.ResourceGroup -n $functionApp.Name --ftps-state FtpsOnly
        }
    }
}

function Restore-Functions {
    New-Functions
    Build-Functions -Publish
    Get-FunctionsKeys
    Sync-Config
    Publish-FunctionsSettings
}

function Remove-Functions {
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.Services) {
            az functionapp delete `
                --name $functionApp.Name `
                --resource-group $plan.ResourceGroup `
                --subscription $config.subscriptionId
        }
        
        az functionapp plan delete `
            --name $plan.Name `
            --resource-group $plan.ResourceGroup `
            --subscription $config.subscriptionId `
            --yes
    }
}

function Build-Functions () {
    param (
        [switch] $LinuxOnly,
        [switch] $WindowsOnly,
        [switch] $KeyVaultPolicies,
        [switch] $Publish,
        [switch] $Settings
    )
    
    $deploymentdir = Test-DirectoryExistence (join-Path $global:envpath "build")
    
    $now = Get-Date -Format "yyyyMMddHHmmss"
    function build($function) {
        dotnet publish -c RELEASE -o (join-path $deploymentdir "windows" ($function + ".publish." + $now))
    }   
    
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.Services) {
            # Windows
            if (-not $plan.IsLinux) {
                if ( -not $LinuxOnly ) {
                    Write-Host ("Building Windows Function App " + $functionApp.Name) -ForegroundColor DarkCyan
    
                    # Build the configured functions
                    Push-Location (join-path $global:workpath ".." $functionApp.Path)
                    build $functionApp.Name
                    Pop-Location
                }
            }
            else {
                if ( -not $WindowsOnly) {
                    if ($functionApp.Path) {
                        Write-Host ("Building Linux-Python Function App " + $functionApp.Name) -ForegroundColor DarkCyan
                        # Linux - Python 
                        # $respath = $deploymentdir+"\linux\"+$functionApp.Name+".publish."+$now
                        $respath = join-path $deploymentdir "linux" ($functionApp.Name + ".publish." + $now)
                
                        Remove-Item $respath -Recurse -ErrorAction SilentlyContinue
    
                        Test-DirectoryExistence $respath
    
                        $srcPath = join-path $global:workpath ".." $functionApp.Path
    
                        Get-AzureFunctionFolders $srcPath | Copy-Item -Destination $respath -Recurse -Force
    
                        Get-AzureFunctionFiles $srcPath | Copy-Item -Destination $respath -Force
    
                        # Add the Function override
                        $overridepath = Get-DeploymentOverlayPath $functionApp.Path
                
                        if ( test-path $overridepath) {
                            Copy-Item -Path $overridepath -Destination $respath -Recurse -Force
                        }
                    }
                }
            }
        }
    }
    
    Compress-Release $deploymentdir $now
    
    # add build version evertytime we build the webapp
    Add-Param "FunctionsBuildVersion" $now
        
    Sync-Config

    if ( $Publish ) {
        Publish-Functions -LinuxOnly:$LinuxOnly -WindowsOnly:$WindowsOnly
    }
    if ( $Settings ) {
        Publish-FunctionsSettings -LinuxOnly:$LinuxOnly -WindowsOnly:$WindowsOnly
    }
}
    
function Publish-Functions() {   
    param (
        [switch] $LinuxOnly,
        [switch] $WindowsOnly
    )
        
    Push-Location $global:envpath
    
    foreach ($plan in $functionscfg.AppPlans) {      
        foreach ($functionApp in $plan.Services) {
            if (! $plan.IsLinux) {
                if (-not $LinuxOnly) {
                    Write-host "Publishing Windows function "$functionApp.Name
    
                    $releasepath = join-path "releases" "windows" ($functionApp.Name + ".publish.latest.zip")
                    az webapp deployment source config-zip --resource-group $plan.ResourceGroup --name $functionApp.Name --src $releasepath
                }
            }
            else {
                if (-not $WindowsOnly) {
                    if ($functionApp.Path) {
                        Write-host "Publishing Python function "$functionApp.Name
    
                        $releasepath = join-path "releases" "linux" ($functionApp.Name + ".publish.latest.zip")
    
                        $unzipPath = join-path "releases" "linux" $functionApp.Name
                        Expand-Archive -Path $releasepath -DestinationPath $unzipPath -Force
                        Push-Location $unzipPath
                        func azure functionapp publish $functionApp.Name --build remote --python
                        Pop-Location
                    }
                }
            }
        }
    }
    Pop-Location
}

# function Upgrade-Functions() {   
#     Push-Location $global:envpath
    
#     foreach ($plan in $functionscfg.AppPlans) {
#         foreach ($functionApp in $plan.Services) {
#             az functionapp config appsettings set --settings FUNCTIONS_EXTENSION_VERSION=~4 --resource-group $plan.ResourceGroup --name $functionApp.Name
    
#             if (! $plan.IsLinux) {
#                 # For Windows function apps only, also enable .NET 6.0 that is needed by the runtime
#                 az functionapp config set --net-framework-version v6.0 --resource-group $plan.ResourceGroup --name $functionApp.Name
#             }
#         }
#     }
#     Pop-Location
# }
    
function Restart-Functions() {   
    Push-Location $global:envpath
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.Services) {
            az functionapp stop --resource-group $plan.ResourceGroup --name $functionApp.Name
            az functionapp start --resource-group $plan.ResourceGroup --name $functionApp.Name 
        }
    }
    Pop-Location
}

function Publish-FunctionSettings() {
    param (
        $plan,
        $functionApp
    )

    $settingspath = "config/functions/" + $functionApp.Id + ".json" 
    
    if (Test-Path $settingspath) {
        az webapp config appsettings set -g $plan.ResourceGroup -n $functionApp.Name --settings @$settingspath
    }

    $settingspath = "config/functions/" + $functionApp.Id + "." + $config.id + ".json"

    if (Test-Path $settingspath) {
        az webapp config appsettings set -g $plan.ResourceGroup -n $functionApp.Name --settings @$settingspath
    }
}

function Publish-FunctionsSettings() {
    param (
        [switch] $LinuxOnly,
        [switch] $WindowsOnly
    )

    # Make sure we have the latest configuration & parameters in
    Sync-Config
    
    Push-Location $global:envpath

    foreach ($plan in $functionscfg.AppPlans) {                    
        foreach ($functionApp in $plan.Services) {
            if (-not $plan.IsLinux) {
                if ( -not $LinuxOnly ) {
                    Publish-FunctionSettings $plan $functionApp
                }
            }
            else {
                if (-not $WindowsOnly) {
                    Publish-FunctionSettings $plan $functionApp
                }
            }
        }
    }
    Pop-Location
}

function New-FunctionsKeys() {
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.Services) {
            foreach ($function in $functionApp.Functions) {
                az functionapp function keys set -g $plan.ResourceGroup `
                    -n $functionApp.Name `
                    --function-name $function.Name `
                    --key-name default `
                    --debug
            }
        }
    }
}

function Get-FunctionsKeys() {
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.Services) {
            foreach ($function in $functionApp.Functions) {
                $url = az functionapp function show -g $plan.ResourceGroup -n $functionApp.Name --function-name $function.Name --query invokeUrlTemplate --out tsv
                if ($url) {
                    $url = $url.Replace("http://", "https://")
                    Add-Param ($functionApp.Id + "." + $function.Name + ".url") $url
        
                    try {
                        $fkey = az functionapp function keys list -g $plan.ResourceGroup -n $functionApp.Name --function-name $function.Name --query default --out tsv
                        if ( $fkey ) {
                            $furl = $url + "?code=" + $fkey
                            Write-host $furl
                            Add-Param ($functionApp.Id + "." + $function.Name) $furl
                            Add-Param ($functionApp.Id + "." + $function.Name + ".key") $fkey    
                        }
                    }
                    catch {
                        Add-Param ($functionApp.Id + "." + $function.Name) $url
                        Add-Param ($functionApp.Id + "." + $function.Name + ".key") ""                    
                    }    
                }
            }
        }
    }
    
    Sync-Config
}
    
function Test-Functions() {
    param (
        [switch] $Local
    )

    # $results=@()

    foreach ($plan in $functionscfg.AppPlans) {
        Write-Host "--------------------"
        Write-Host "Testing Plan "$plan.name -ForegroundColor DarkCyan
        foreach ($functionApp in $plan.Services) {
            Write-Host "Testing App "$functionApp.name -ForegroundColor DarkYellow
            foreach ($function in $functionApp.Functions) {
                Write-Host "Testing Function "$function.name -ForegroundColor DarkBlue
                $url = az functionapp function show -g $plan.ResourceGroup -n $functionApp.Name --function-name $function.Name --query invokeUrlTemplate --out tsv
                if ($url) {
                    $url = $url.Replace("http://", "https://")   
                    try {
                        $fkey = az functionapp function keys list -g $plan.ResourceGroup -n $functionApp.Name --function-name $function.Name --query default --out tsv
                        if ( $fkey ) {
                            $furl = $url + "?code=" + $fkey
                        }
                    }
                    catch {
                    }
    
                    # Write-Host $furl
    
                    try {
                        $response = Invoke-WebRequest -Uri $furl -Method Post -Body '{"values":[{"recordId":"0","data":{}}]}' -Headers @{'Content-Type' = 'application/json' }
        
                        if ($response.StatusCode -eq 200) {
                            Write-Host "Function is OK!" -ForegroundColor Green
                        }
                        else {
                            Write-Host "The function may be down, please check!" -ForegroundColor Red
                        }                        
                    }
                    catch {                    
                        Write-Host "Exception: "$_.Exception.Message -ForegroundColor DarkRed
    
                        # $Results+={ "Name":"test", "Status":"Failed", "Message":$_.Exception.Message}
                    }
                }
            }
        }
    }
}

#endregion
    
#region Azure Web App

function Test-WebAppExistence {
    param (
        [string]$aspName
    )
    $exists = az webapp list --query "[?name=='$aspName']" | ConvertFrom-Json
    
    return ($exists.Length -gt 0)
}
    
function New-WebApps {    
    param (
        [switch] $LinuxOnly,
        [switch] $WindowsOnly
    )
    
    foreach ($plan in $webappscfg.AppPlans) {
        if ($plan.IsLinux) {
            if (-not $WindowsOnly) {
                if (!(Test-AppPlanExistence $plan.Name)) {
                    # Create a Linux plan
                    az appservice plan create -g $plan.ResourceGroup `
                        --name $plan.Name `
                        --is-linux `
                        --location $config.location `
                        --sku $plan.Sku `
                        --number-of-workers 1 
                }
            }
        }
        else {
            if (-not $LinuxOnly) {
                # Create a Non-Linux plan
                if (!(Test-AppPlanExistence $plan.Name)) {
                    az appservice plan create -g $plan.ResourceGroup `
                        --name $plan.Name `
                        --location $config.location `
                        --sku $plan.Sku 
                }
            }
        }
    
        foreach ($webApp in $plan.Services) {
            Write-Host "Evaluating Web App "$webApp.Name -ForegroundColor DarkYellow
            if ($plan.IsLinux) {
                if (-not $WindowsOnly) {
                    # $imageName = $params.acr + "/" + $webApp.Image
                    # Image names must be prefixed with server 
                    # local repository is $params.acr or {{config.name}}acr.azurecr.io
                    if ( !(Test-WebAppExistence $webApp.Name)) {
                        Write-Host "Creating Web App "$webApp.Name -ForegroundColor DarkYellow
                        # Create a Web App
                        az webapp create --name $webApp.Name `
                            --plan $plan.Name `
                            --resource-group $plan.ResourceGroup `
                            --https-only true `
                            --deployment-container-image-name $webApp.Image
                        
                        $storekey = $params.techStorageConnString
    
                        az webapp config appsettings set `
                            --name $webApp.Name `
                            --resource-group $plan.ResourceGroup `
                            --settings AzureWebJobsStorage=$storekey
                    }
                }
            }
            else {
                if (-not $LinuxOnly) {
                    # Create the webui app service 
                    if ( !(Test-WebAppExistence $webApp.Name)) {
                        Write-Host "Creating Web App "$webApp.Name -ForegroundColor DarkYellow
                        az webapp create `
                            --name $webApp.Name `
                            --plan $plan.Name `
                            --resource-group $plan.ResourceGroup `
                            --https-only true `
                            --runtime 'dotnet:6'
    
                        if ($config.stagingUIEnabled) {
                            az webapp deployment slot create --name $webApp.Name `
                                --resource-group $plan.ResourceGroup `
                                --https-only true `
                                --slot staging `
                                --configuration-source $webApp.Name    
                        }
                    }
    
                    # Use 64 bits worker process
                    az webapp config set -g $plan.ResourceGroup -n $webApp.Name --use-32bit-worker-process false
    
                    # Staging support
                    if ($config.stagingUIEnabled) {
                        az webapp config set -g $plan.ResourceGroup -n $webApp.Name --use-32bit-worker-process false --slot staging
                        az webapp identity assign -g $plan.ResourceGroup -n $webApp.Name --slot staging
                        az webapp config set -g $plan.ResourceGroup -n $webApp.Name --ftps-state FtpsOnly --slot staging
                        # az webapp update  -g $plan.ResourceGroup -n $webApp.Name --set httpsOnly=true --slot staging    
                    }

                    # App Service Logging
                    az webapp log config --application-logging azureblobstorage `
                        --resource-group $plan.ResourceGroup `
                        --name $webApp.Name `
                        --detailed-error-messages true `
                        --failed-request-tracing true
                    # TODO Still need the UI to specifiy the storage and container to put the logs in...
                }
            }
    
            Write-Host "Configuring Web App "$webApp.Name -ForegroundColor DarkYellow

            # Assign a system managed identity
            az webapp identity assign -g $plan.ResourceGroup -n $webApp.Name
            Write-Host "Assigned System identity " -ForegroundColor DarkYellow
                
            # FTP State to FTPS Only
            az webapp config set -g $plan.ResourceGroup -n $webApp.Name --ftps-state FtpsOnly
            Write-Host "FTPS Only " -ForegroundColor DarkYellow

            # Disable FTP and SCM Basic Authentication 
            # $parent ="sites/"+$webApp.Name 
            # az resource update --resource-group $plan.ResourceGroup `
            # --name ftp `
            # --namespace Microsoft.Web `
            # --resource-type basicPublishingCredentialsPolicies `
            # --parent $parent `
            # --set properties.allow=false

            # az resource update --resource-group $plan.ResourceGroup `
            # --name scm `
            # --namespace Microsoft.Web `
            # --resource-type basicPublishingCredentialsPolicies `
            # --parent $parent `
            # --set properties.allow=false

            Write-Host "FTP and SCM Basic Authentication" -ForegroundColor DarkYellow
        }
    }
}

function Build-WebApps {
    param (
        [switch] $LinuxOnly,
        [switch] $WindowsOnly,
        [switch] $Publish,
        [switch] $KeyVaultPolicies,
        [switch] $Settings
    )
    
    $deploymentdir = Test-DirectoryExistence (join-Path $global:envpath "build")
    
    $now = Get-Date -Format "yyyyMMddHHmmss"
    function publish_windows($function) {
        Write-Host $pwd
        $buildpath = join-path $deploymentdir "webapps" ($function + ".publish." + $now)
        Write-Host $buildpath
        dotnet publish -c RELEASE -o $buildpath 
        #| Out-Null
        return $buildpath
    }
    function publish_linux($function) {
        Write-Host $pwd
        $buildpath = join-path $deploymentdir "webapps" ($function + ".publish." + $now)
        Write-Host $buildpath
        dotnet publish -r linux-x64 --self-contained false -c RELEASE -o $buildpath | Out-Null
        return $buildpath
    }
    
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            if (-not $webApp.Image) {

                $appLocation = (join-path $global:workpath ".." $webApp.Path)
                Write-Host $appLocation -ForegroundColor DarkGreen
    
                Push-Location $appLocation
                if ($plan.IsLinux) {
                    if (-not $WindowsOnly) {
                        Write-Host "Building Linux WebApp "$webApp.Name -ForegroundColor DarkGreen
                        $respath = publish_linux $webApp.Name
                    }
                }
                else {
                    if (-not $LinuxOnly) {
                        Write-Host "Building Windows WebApp "$webApp.Name -ForegroundColor DarkGreen
                        $respath = publish_windows $webApp.Name
                    }
                }

                Pop-Location
                
                # Add the UI override (i.e. config.json + wwwroot branding support)
                # $overridepath = Get-DeploymentOverlayPath (join-path "02-CognitiveSearch.UI" "CognitiveSearch.UI")
                $overridepath = Get-DeploymentOverlayPath $webApp.Path
                Write-Host ("Building WebApp - Add a potential overlay " + $overridepath)
    
                if ( test-path $overridepath) {
                    Copy-Item -Path $overridepath -Destination $respath -Recurse -Force
                }
            }
        }
    }
    
    Compress-Release $deploymentdir $now
    
    # add build version evertytime we build the webapp
    Add-Param "WebAppBuildVersion" $now
    
    Sync-Config
    
    if ( $Publish ) {
        Publish-WebApps -LinuxOnly:$LinuxOnly -WindowsOnly:$WindowsOnly 
    }
    # if ( $KeyVaultPolicies ) {
    #     Add-KeyVaultWebAppsRBAC 
    # }
    if ( $Settings ) {
        Publish-WebAppsSettings -LinuxOnly:$LinuxOnly -WindowsOnly:$WindowsOnly
    }
}
    
function Restart-WebApps {
    param (
        [switch] $Production,
        [string] $Slot = "staging"
    )
    
    if (-not $config.stagingUIEnabled) {
        $Production = $true
    }
    
    Push-Location $global:envpath
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            if (-not $webApp.Image) {
                if ($production) {
                    az webapp stop --resource-group $plan.ResourceGroup `
                        --name $webApp.Name
                        
                    az webapp start --resource-group $plan.ResourceGroup `
                        --name $webApp.Name   
                }
                else {
                    az webapp stop --resource-group $plan.ResourceGroup `
                        --name $webApp.Name `
                        --slot $Slot
    
                    az webapp start --resource-group $plan.ResourceGroup `
                        --name $webApp.Name `
                        --slot $Slot
                }
            }
            else {
                az webapp stop --resource-group $plan.ResourceGroup --name $webApp.Name
                    
                az webapp start --resource-group $plan.ResourceGroup --name $webApp.Name
            }
        }
    }
    Pop-Location
}
    
function Stop-WebApps {
    param (
        [switch] $Production,
        [switch] $LinuxOnly,
        [switch] $WindowsOnly,
        [string] $Slot = "staging"
    )
    
    if (-not $config.stagingUIEnabled) {
        $Production = $true
    }
    
    Push-Location $global:envpath
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            if ($plan.IsLinux) {
                if (-not $WindowsOnly) {
                    az webapp stop --resource-group $plan.ResourceGroup --name $webApp.Name
                }
            }
            else {
                if (-not $LinuxOnly) {
                    if ($production) {
                        az webapp stop --resource-group $plan.ResourceGroup `
                            --name $webApp.Name
                    }
                    else {
                        az webapp stop --resource-group $plan.ResourceGroup `
                            --name $webApp.Name `
                            --slot $Slot
                    }    
                }
            }            
        }
    }
    Pop-Location
}
    
function Start-WebApps {
    param (
        [switch] $Production,
        [switch] $LinuxOnly,
        [switch] $WindowsOnly,
        [string] $Slot = "staging"
    )
    
    if (-not $config.stagingUIEnabled) {
        $Production = $true
    }
    
    Push-Location $global:envpath
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            if ($plan.IsLinux) {
                if (-not $WindowsOnly) {
                    az webapp start --resource-group $plan.ResourceGroup --name $webApp.Name
                }
            }
            else {
                if (-not $LinuxOnly) {
                    if ($production) {
                        az webapp start --resource-group $plan.ResourceGroup `
                            --name $webApp.Name
                    }
                    else {
                        az webapp start --resource-group $plan.ResourceGroup `
                            --name $webApp.Name `
                            --slot $Slot
                    }    
                }
            }            
        }
    }
    Pop-Location
}

function Remove-WebApps {
    param (
        [switch] $LinuxOnly,
        [switch] $WindowsOnly
    )
  
    Push-Location $global:envpath
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            if ($plan.IsLinux) {
                if (-not $WindowsOnly) {
                    az webapp delete --resource-group $plan.ResourceGroup --name $webApp.Name
                }
            }
            else {
                if (-not $LinuxOnly) {
                    az webapp delete --resource-group $plan.ResourceGroup `
                        --name $webApp.Name
                }
            }            
        }
    }
    Pop-Location
}

function Restore-WebApps {
    param (
        [switch] $LinuxOnly,
        [switch] $WindowsOnly
    )

    New-WebApps -LinuxOnly:$LinuxOnly -WindowsOnly:$WindowsOnly

    Build-WebApps -LinuxOnly:$LinuxOnly -WindowsOnly:$WindowsOnly -Publish -KeyVaultPolicies -Settings

    Set-WebAppServicesAccessRestriction
}

function Publish-WebApps {
    param (
        [switch] $Production,
        [string] $Slot = "staging",
        [switch] $LinuxOnly,
        [switch] $WindowsOnly
    )
    
    if (-not $config.stagingUIEnabled) {
        $Production = $true
    }
    
    Push-Location $global:envpath 
    foreach ($plan in $webappscfg.AppPlans) {
    
        foreach ($webApp in $plan.Services) {
    
            if (-not $webApp.Image) {
                if (-not $plan.IsLinux) {
    
                    $releasepath = "releases/webapps/" + $webApp.Name + ".publish.latest.zip"
    
                    if ($production) {
                        az webapp deployment source config-zip --resource-group $plan.ResourceGroup `
                            --name $webApp.Name `
                            --src $releasepath    
                    }
                    else {
                        az webapp deployment source config-zip --resource-group $plan.ResourceGroup `
                            --name $webApp.Name `
                            --slot $Slot `
                            --src $releasepath                        
                    }
                }
            }
        }
    }
    Pop-Location
}
    
function Publish-WebAppsSettings {
    param (
        [switch] $LinuxOnly,
        [switch] $WindowsOnly,
        [switch] $Production,
        [string] $Slot = "staging"
    )
    
    if (-not $config.stagingUIEnabled) {
        $Production = $true
    }
    
    # Make sure we have the latest configuration & parameters in
    Sync-Config
    
    Push-Location $global:envpath
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            Write-Host $webApp
    
            $settingspath = "config/webapps/" + $webApp.Id + ".json" 
    
            if (Test-Path $settingspath) {
                if (-not $plan.IsLinux) {
                    if (-not $LinuxOnly) {
                        if ($production) {
                            az webapp config appsettings set -g $plan.ResourceGroup `
                                -n $webApp.Name `
                                --settings @$settingspath
                        }
                        else {
                            az webapp config appsettings set -g $plan.ResourceGroup `
                                -n $webApp.Name `
                                --slot $Slot `
                                --settings @$settingspath
                        }
                    }
                }
                else {
                    if (-not $WindowsOnly) {
                        az webapp config appsettings set -g $plan.ResourceGroup `
                            -n $webApp.Name `
                            --settings @$settingspath
                    }
                }
            }
    
            $settingspath = "config/webapps/" + $webApp.Id + "." + $config.id + ".json"
    
            if (Test-Path $settingspath) {
                if (-not $plan.IsLinux) {
                    if (-not $LinuxOnly) {
                        if ($production) {
                            az webapp config appsettings set -g $plan.ResourceGroup `
                                -n $webApp.Name `
                                --settings @$settingspath
                        }
                        else {
                            az webapp config appsettings set -g $plan.ResourceGroup `
                                -n $webApp.Name `
                                --slot $Slot `
                                --settings @$settingspath
                        }
                    }
                }
                else {
                    if (-not $WindowsOnly) {
                        az webapp config appsettings set -g $plan.ResourceGroup `
                            -n $webApp.Name `
                            --settings @$settingspath
                    }
                }
            }
        }
    }
    Pop-Location
}
    
function Set-WebAppAuthentication {
    
    # az webapp auth microsoft update --name
    # --resource-group
    # [--allowed-audiences]
    # [--client-id]
    # [--client-secret]
    # [--client-secret-setting-name]
    # [--issuer]
    # [--slot]
    # [--tenant-id]
    # [--yes]
    
}

#endregion
    
#region Docker 

function Build-DockerImages {
    param (
        [Int64] $ImageId = 0,
        [switch] $WebApp
    )
    
    Write-Host "Parameter ImageId "$ImageId
    
    foreach ($image in $dockercfg.Images) {
        Write-Host ("Docker Image " + $image.Id + " " + $image.Name)
    
        $build = $false
    
        if ($WebApp) {
            if ($image.webapp) {
                $build = $true
            }   
        }
        elseif (($ImageId -eq 0) -or ($ImageId -eq $image.Id)) {
            $build = $true
        }
    
        if ($build) {
            Write-Host "Building image..." -ForegroundColor DarkYellow
    
            $imgdockerfile = join-path $global:workpath $image.Path Dockerfile
    
            if ($image.BuildContext) {
                Push-Location (join-path $global:workpath $image.BuildContext)
            }
            else {
                Push-Location (join-path $global:workpath $image.Path)
            }
    
            az acr build --platform linux --image $image.Name --registry $params.acr --file $imgdockerfile . 
            Pop-Location 
        }
    }
        
    # Save and Apply the Parameters we got
    Sync-Config
}
    
#endregion
    
#region Key Vault
function Initialize-KeyVault {
    Add-KeyVaultSecrets
}

function Test-KeyVaultCandidate {
    param (
        $name
    )
    if ( $name.endswith("Key") -or $name.endswith("ConnectionString") -or $name.endswith("Password") -or $name.endswith("Username")) {
        if ($name -ne "APPINSIGHTS_INSTRUMENTATIONKEY") {
            return $true 
        } else {
            return $false
        }
    }
    else {
        return $false
    } 


}
function Add-KeyVaultSecrets {
    param (
        [switch] $AddNew
    )
    
    $secretExpiryDate = ((get-date).ToUniversalTime().AddYears(2)).ToString("yyyy-MM-ddTHH:mm:ssZ")
    
    $params.PSObject.Properties | ForEach-Object {
        if ( Test-KeyVaultCandidate -name $_.Name ) {
            if ($_.Value) {
                $exists = az keyvault secret show --name $_.Name --vault-name $params.keyvault --query name --out tsv
                if ($exists -and $AddNew) {
                    Write-Host "Skipping existing secret "$_.Name -ForegroundColor DarkYellow
                }
                else {
                    az keyvault secret set --name $_.Name --value $_.Value --vault-name $params.keyvault
                    az keyvault secret set-attributes --vault-name $params.keyvault --name $_.Name --expires $secretExpiryDate
                    Write-Host ("Added Secret to the Keyvault " + $_.Name) -ForegroundColor Green
                }
            }
        }
    }
}

function Add-KeyVaultFunctionsRBAC {
    $keyVaultScope = "/subscriptions/" + $config.subscriptionId + "/resourcegroups/" + $config.resourceGroupName + "/providers/Microsoft.KeyVault/vaults/" + $params.keyvault
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.Services) {          
            $principalId = az functionapp identity show -n $functionApp.Name -g $plan.ResourceGroup --query principalId --out tsv    
            az role assignment create --role "Key Vault Secrets User" --assignee $principalId --scope $keyVaultScope
        }
    }
}

function Add-KeyVaultWebAppsRBAC {
    $keyVaultScope = "/subscriptions/" + $config.subscriptionId + "/resourcegroups/" + $config.resourceGroupName + "/providers/Microsoft.KeyVault/vaults/" + $params.keyvault
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            $principalId = az webapp identity show -n $webApp.Name -g $plan.ResourceGroup --query principalId --out tsv  
            az role assignment create --role "Key Vault Secrets User" --assignee $principalId --scope $keyVaultScope
        }
    }
}
    
#endregion
    
#region Solution 

function Build-Solution {
    param (
        [switch] $Publish
    )
    Build-DockerImages
    Build-Functions
    Build-WebApps
    
    if ($Publish) {
        Publish-Solution
    }
}

function Publish-Solution {
    param (
        [switch] $Production,
        [string] $Slot = "staging"
    )
    
    if (-not $config.stagingUIEnabled) {
        $Production = $true
    }
    
    Sync-Config
    
    Publish-Functions
    Publish-WebApps
    
    # Publishing settings will restart all app services.
    Publish-FunctionsSettings
    Publish-WebAppsSettings
}

function Update-Solution {
    param (
        [switch] $NewFunction,
        [switch] $NewSkill,
        [switch] $Search,
        [switch] $UI
    )

    Sync-Config

    if ( $NewFunction ) {
        New-Functions
        Build-Functions -Publish -KeyVaultPolicies -Settings        
        Get-FunctionsKeys        
        Sync-Config
    }
    
    if ( $NewSkill ) {
        Build-Functions -Publish -KeyVaultPolicies -Settings        
        Get-FunctionsKeys
        Sync-Config
        Initialize-Search
    }
    
    # if ( $Function ) {
    #     Build-Functions -Publish -KeyVaultPolicies -Settings
    # }  

    if ($Search) {
        Initialize-Search
    }
    
    If ($UI) {
        Build-WebApps -WindowsOnly -Publish -KeyVaultPolicies -Settings
    }
}

function Optimize-Solution () {
    
    # Scale down the entire solution
    
    Push-Location $global:envpath
    
    $targetPlan = "P1V2" 
    
    # WebApps
    foreach ($plan in $webappscfg.AppPlans) {
        Write-Host "Plan "$plan.Name
    
        # Consumption Plan Support
        $consumption = ($plan.Sku -eq "Y1")
    
        if (-not $consumption) {
            az appservice plan update `
                --name $plan.Name `
                --resource-group $plan.ResourceGroup `
                --sku $targetPlan `
                --number-of-workers 1
        }
    }
    
    # Functions
    foreach ($plan in $functionscfg.AppPlans) {
        Write-Host "Plan "$plan.Name
    
        # Consumption Plan Support
        $consumption = ($plan.Sku -eq "Y1")
    
        if (-not $consumption) {
            az functionapp plan update `
                --name $plan.Name `
                --resource-group $plan.ResourceGroup `
                --sku $targetPlan `
                --number-of-workers 1
        }
    }
    
    Pop-Location
}

function Test-Solution {
    Test-Functions
}

function Suspend-Solution {

    # De-Allocate Functions which are costly to leave running.
    Remove-Functions

    # Remove Linux WebApp (e.g. Tika)
    Remove-WebApps -LinuxOnly

}

function Resume-Solution {

    # Re-deploy the functions
    Restore-Functions

    # Start Linux WebApp
    Restore-WebApps -LinuxOnly

}
#endregion

#region Open AI 
function Get-OpenAIKey {

    if ($openaicfg.enable) {

        foreach ($azureResource in $openaicfg.Items) {
            Write-Host "Checking Open AI Service existence "$azureResource.Name
    
            $exists = az cognitiveservices account show --name $azureResource.Name --resource-group $azureResource.ResourceGroup --query id --out tsv
    
            if ( $exists ) {
                Write-Host "Fetching OpenAI Service key "$azureResource.Name
    
                $cogServicesKey = az cognitiveservices account keys list --name $azureResource.Name -g $azureResource.ResourceGroup --query key1 --out tsv
    
                if ( $cogServicesKey -and $cogServicesKey.Length -gt 0 ) {
                    Add-Param ($azureResource.Parameter+"Key") $cogServicesKey
                }
    
                # Endpoint
                $cogEndpoint = az cognitiveservices account show -n $azureResource.Name -g $azureResource.ResourceGroup --query properties.endpoint --out tsv 
                if ( $cogEndpoint -and $cogEndpoint.Length -gt 0 ) {
                    Add-Param ($azureResource.Parameter+"Endpoint") $cogEndpoint
                }
            }
        }
        Save-Parameters    
    }
}

function Deploy-OpenAIModule () {
    param (
        [string] $DeploymentName,
        [string] $ModelName,
        [string] $ModelVersion
    )
    Write-Host "Deploying Open AI Model Cognitive Service";

    az cognitiveservices account deployment create `
        --name $params.OpenAI `
        --resource-group $config.resourceGroupName `
        --deployment-name $params.OpenAIEngine `
        --model-name $params.OpenAIModel `
        --model-version $params.OpenAIModelVersion  `
        --model-format "OpenAI" `
        --sku-capacity $params.OpenAICapacity `
        --sku-name "Standard"    
}

# function Remove-OpenAIModel() { 
#     param (
#         [string] $DeploymentName
#     )

#     az cognitiveservices account deployment delete `
#     -g $openaicfg.Items[0].ResourceGroup `
#     -n $openaicfg.Items[0].Name `
#     --deployment-name $DeploymentName    
# }
#endregion

Export-ModuleMember -Function *
