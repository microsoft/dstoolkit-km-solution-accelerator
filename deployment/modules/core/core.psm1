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

function Import-ConfigItem {
    param (
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    $folders = Get-ChildItem -Directory -Path (join-path $global:envpath $Name)

    foreach ($folder in $folders) {
        if (Test-Path $(join-Path $folder.FullName "config.json")) {
            $varValue = [string] (Get-Content -Path $(join-path $folder.FullName "config.json"))
            $varValue = ConvertFrom-Json $varValue
            $varName = $folder.Name + "cfg"

            Set-Variable -Name $varName -Value $varValue -Visibility Public -Option AllScope -Force -Scope Global
    
            Add-Param -Name ($folder.Name + "Enabled") -Value $varValue.enable

            if ($varValue.enable) {
                Import-ConfigParameters $varValue
            }
        }
    }

    Save-Parameters
}

function Import-ServicesConfig() {
    Import-ConfigItem -Name "services"
    Import-ConfigItem -Name "features"
}

function Import-ConfigParameters ($inputcfg) {
    Write-Debug "Import configuration "
    # Automatically ad the services parameters to the global parameters variable. 
    if ($inputcfg.Parameters) {
        foreach ($entry in $(Get-Member -InputObject $inputcfg.Parameters -MemberType NoteProperty)) {
            $value = $entry.Name
            Add-Param -Name $entry.Name -Value $inputcfg.Parameters.$value
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

function Add-Config {
    param (
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [Parameter(Mandatory = $true)]
        [object] $Value
    )
    if ( $global:config.PSobject.Properties.name -eq $Name) {
        $global:config.$Name = $Value
    }
    else {
        $global:config | Add-Member -MemberType NoteProperty -Name $Name -Value $Value -ErrorAction Ignore
    }
}

function Save-Config {
    $global:config | ConvertTo-Json -Depth 100 -Compress | Out-File -FilePath (join-path $global:envpath "config.json")
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
                    Add-Param -Name $prop.Name -Value (ConvertTo-String -secureString $propValue)
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
                    Add-Param -Name ($property.Name + "." + $member.Name) -Value $member.Value   
                }
            }    
        }
        else {
            Add-Param -Name $property.Name -Value $property.Value        
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
    
    $folders = @("services", "monitoring", "tests")
    
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
    
        $folders = @("services", "monitoring", "tests")
        
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
    # Container
    $dataStorageContainerName = $params.storageContainers[0];
    Add-Param -Name "dataStorageContainerName" -Value $dataStorageContainerName

    Add-Param -Name "StorageContainersAsString" -Value $([String]::Join(',', $params.storageContainers))

    # Create the containers entries for UI SAS access
    $StorageContainerAddresses = @()
    foreach ($container in $params.storageContainers) {
        $url = "https://" + $global:params.dataStorageAccountName + ".blob.core.windows.net/" + $container
        $StorageContainerAddresses += $url
    }
    Add-Param -Name "StorageContainerAddresses" -Value $StorageContainerAddresses
    Add-Param -Name "StorageContainerAddressesAsString" -Value $([String]::Join(',', $StorageContainerAddresses))
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
    $overridepath = join-path $global:workpath "services" $config.id  $relpath "*"
    
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
        --account-key $params.dataStorageAccountKey  `
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
            --account-key $params.dataStorageAccountKey `
            --exclude-dir `
            --query "[].{name:name}" `
            --output tsv
    }
    else {
        $files = az storage fs file list `
            --file-system $container `
            --recursive `
            --account-name $params.dataStorageAccountName `
            --account-key $params.dataStorageAccountKey `
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
        --account-key $params.dataStorageAccountKey  `
        --metadata AzureSearch_RetryTag=$now
    
}
#endregion

#region Service Keys 
function Get-AllServicesKeys() {
    param (
        [switch] $AddToKeyVault
    )
    Get-AppInsightsInstrumentationKey
    Get-StorageAccountsKeys

    Get-CognitiveServiceKey
    Get-AzureMapsSubscriptionKey
    Get-FunctionsKeys
    Get-SearchServiceKeys
    Get-OpenAIKey
    Get-ServiceBusConnectionString
    Get-RedisConnectionString
    Get-CosmosTableConnectionString
    Get-AKSCredentials

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
            Add-Param -Name ($tuple.Name + "Key") -Value $key
        }            
    }
    Save-Parameters
    
    $tuples = Get-Parameters "appInsightsWindows"
    
    foreach ($tuple in $tuples) {
        $key = az monitor app-insights component show --app $tuple.Value -g $config.resourceGroupNameWindows --query instrumentationKey  --out tsv
    
        if ( $key -and $key.length -gt 0 ) {
            Add-Param -Name ($tuple.Name + "Key") -Value $key
        }            
    }
    Save-Parameters
    
    $tuples = Get-Parameters "appInsightsLinux"
    
    foreach ($tuple in $tuples) {
        $key = az monitor app-insights component show --app $tuple.Value -g $config.resourceGroupNameLinux --query instrumentationKey  --out tsv
    
        if ( $key -and $key.length -gt 0 ) {
            Add-Param -Name ($tuple.Name + "Key") -Value $key
        }            
    }
    Save-Parameters
}

function Get-StorageAccountsKeys {
    param (
        [string] $Id
    )

    foreach ($azureResource in $storagecfg.Items) {

        if ($Id -and $azureResource.Id -ne $Id) {
            continue
        }

        Write-Host ("Service Name  " + $azureResource.Name) -ForegroundColor Yellow
        $exists = az storage account show --name $azureResource.Name --resource-group $azureResource.ResourceGroup --query id --out tsv

        if ( $exists ) {
            $storageAccountKey = az storage account keys list --account-name $azureResource.Name -g $azureResource.ResourceGroup --query [0].value  --out tsv
            Add-Param -Name ($azureResource.Id + "StorageAccountKey") -Value $storageAccountKey
            
            $storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=' + $azureResource.Name + ';AccountKey=' + $storageAccountKey + ';EndpointSuffix=core.windows.net'
            Add-Param -Name ($azureResource.Id + "StorageConnectionString") -Value $storageConnectionString   
        }
    }

    Save-Parameters
}
    
function Get-CognitiveServiceKey {
    
    foreach ($azureResource in $cogservicescfg.Items) {
        Write-Host "Checking Cognitive Service existence "$azureResource.Name

        $exists = az cognitiveservices account show --name $azureResource.Name --resource-group $azureResource.ResourceGroup --query id --out tsv

        if ( $exists ) {
            Write-Host "Fetching Cognitive Service key "$azureResource.Name

            $cogServicesKey = az cognitiveservices account keys list --name $azureResource.Name -g $azureResource.ResourceGroup --query key1 --out tsv

            if ( $cogServicesKey -and $cogServicesKey.Length -gt 0 ) {
                Add-Param -Name ($azureResource.Parameter + "Key") -Value $cogServicesKey
            }
        }
    }

    Save-Parameters
}
    
function Get-AzureMapsSubscriptionKey {
    if ($mapscfg.enable) {
        $mapsKey = az maps account keys list --name $params.maps --resource-group $config.resourceGroupName --query primaryKey --out tsv
        Add-Param -Name "mapsSubscriptionKey" -Value $mapsKey
    
        Save-Parameters
    }
}

# Get Service Bus Namespace Connection String
function Get-ServiceBusConnectionString {
    if ($servicebuscfg.enable) {
        foreach ($azureResource in $servicebuscfg.Items) {
            # TODO check to capture connection string 
            $connection = az servicebus namespace authorization-rule keys list `
                --resource-group $config.resourceGroupName `
                --namespace-name $azureResource.Name `
                --name $azureResource.AuthorizationRule `
                --query primaryConnectionString `
                --output tsv
            
            Add-Param -Name ($azureResource.Id + "ConnectionString") -Value $connection

            Save-Parameters
        }
    }
}

# Get Redis Cache Connection String
function Get-RedisConnectionString {
    if ($rediscfg.enable) {
        foreach ($azureResource in $rediscfg.Items) {
            # TODO check to capture connection string not the key
            $primaryKey = (az redis list-keys --name $azureResource.Name --resource-group $config.resourceGroupName | ConvertFrom-Json).primaryKey
            $connection = ($azureResource.Name + ".redis.cache.windows.net:6380,password=$primaryKey,ssl=True,abortConnect=False,ConnectRetry=3,ConnectTimeout=5000,SyncTimeout=5000")
    
            Add-Param -Name ($azureResource.Id + "ConnectionString") -Value $connection

            Save-Parameters
        }
    }
}

# Get Cosmos Connection strings
function Get-CosmosTableConnectionString() {
    if ($cosmoscfg.enable) {
        foreach ($azureResource in $cosmoscfg.Items) {
            $list = (az cosmosdb keys list --type connection-strings `
                    --resource-group $config.resourceGroupName `
                    --name $azureResource.Name | ConvertFrom-Json)
            $value = $list.connectionStrings | where-Object -Property Description -EQ  "Primary Table Connection String"
 
            Add-Param -Name ($azureResource.Id + "ConnectionString") -Value $value.connectionString

            Save-Parameters
        }
    }
}
#endregion

#region Azure Cognitive Search 
    
function Initialize-SearchConfig {

    if ($searchcfg.searchBlobPartitions) {
        Write-Host "Blob partitionning enabled ..."
        for ($i = 0; $i -lt $searchcfg.searchBlobPartitions.Count; $i++) {
            $partitionName = $searchcfg.searchBlobPartitions[$i]
    
            $indexerPath = join-path $global:envpath "services" "search" "indexers" "documents.json"
            if ( test-path $indexerPath) {
                # Create a partition datasource for documents
                $datasource = Get-Content -Path (join-path $global:envpath "services" "search" "datasources" "documents.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ($config.name + "-documents-" + $i)
                $jsonobj.container | Add-Member -MemberType NoteProperty -Name "query" -Value $partitionName -ErrorAction Ignore
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "services" "search" "datasources" ("documents-" + $i + ".json")) -Force
    
                # Create a partition indexer for documents
                $datasource = Get-Content -Path (join-path $global:envpath "services" "search" "indexers" "documents.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ($config.name + "-documents-" + $i)
                $jsonobj.dataSourceName = ($config.name + "-documents-" + $i)
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "services" "search" "indexers" ("documents-" + $i + ".json")) -Force    
            }
    
            $indexerPath = join-path $global:envpath "services" "search" "indexers" "images.json"
            if ( test-path $indexerPath) {
                # Create a partition datasource for images
                $datasource = Get-Content -Path (join-path $global:envpath "services" "search" "datasources" "images.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ($config.name + "-images-" + $i)
                $jsonobj.container | Add-Member -MemberType NoteProperty -Name "query" -Value $partitionName -ErrorAction Ignore
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "services" "search" "datasources" ("images-" + $i + ".json")) -Force
    
                # Create a partition indexer for images
                $datasource = Get-Content -Path (join-path $global:envpath "services" "search" "indexers" "images.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ($config.name + "-images-" + $i)
                $jsonobj.dataSourceName = ($config.name + "-images-" + $i)
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "services" "search" "indexers" ("images-" + $i + ".json")) -Force
    
            }
        }
        Remove-item (join-path $global:envpath "services" "search" "indexers" "documents.json") -Force -ErrorAction SilentlyContinue
        Remove-Item (join-path $global:envpath "services" "search" "indexers" "images.json") -Force -ErrorAction SilentlyContinue
    }
    
    Initialize-SearchParameters    
}
    
function Initialize-SearchParameters {
    
    Write-Debug -Message "Create/Update Search Configuration"
    
    # Get the list of Synonyms Maps
    $synonymmaps = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "synonyms") -Recurse
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($config.name + "-" + $item)
        Add-Param -Name $item"SynonymMap" -Value $value
        $synonymmaps += $value
    }
    Add-Param "searchSynonymMaps" $synonymmaps
    Write-Debug -Message "Parameters Synonyms created"
    
    # Get the list of SkillsSets
    $skillslist = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "skillsets")
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($config.name + "-" + $item)
        Add-Param -Name $item"SkillSet" -Value $value
        $skillslist += $value
    }
    Add-Param -Name "searchSkillSets" -Value $skillslist
    Write-Debug -Message "Parameters SkillSet created"
    
    # Get the list of Indexes
    $indexeslist = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "indexes")
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($config.name + "-" + $item)
        Add-Param -Name $item"Name" -Value $value
        $indexeslist += $value
    }
    Add-Param -Name "searchIndexes" -Value ($indexeslist | Join-String -Property $_ -Separator ",")
    Add-Param -Name "searchIndexesList" -Value $indexeslist
    Write-Debug -Message "Parameters Indexes created"
    
    # Get the list of DataSources
    $datasourceslist = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "datasources")
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($config.name + "-" + $item)
        Add-Param -Name $item"DataSource" -Value $value
        Add-Param -Name $item"StorageContainerName" -Value $item            
        $datasourceslist += $value
    }
    Add-Param -Name "searchDataSources" -Value $datasourceslist
    Write-Debug -Message "Parameters DataSources created"
    
    # Get the list of Indexers
    $indexersList = @()
    $indexersStemList = @()

    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "indexers")
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($config.name + "-" + $item)
        Add-Param -Name $item"Indexer" -Value $value
        $indexersList += $value
        $indexersStemList += $item
    }
    Add-Param -Name "searchIndexers" -Value ($indexersList | Join-String -Property $_ -Separator ",")
    Add-Param -Name "searchIndexersList" -Value $indexersList
    Add-Param -Name "searchIndexersStemList" -Value $indexersStemList
    Write-Debug -Message "Parameters Indexers created"
}
    
function Get-SearchServiceKeys {
    Write-Host "Fetching Search service keys..." -ForegroundColor DarkBlue

    foreach ($azureResource in $searchcfg.Items) {

        Write-Host "Provisionning Search Service "$azureResource.Name

        $searchServiceKey = az search admin-key show --resource-group $azureResource.ResourceGroup --service-name $azureResource.Name --query primaryKey --out tsv
        # TODO use ID trick 
        Add-Param -Name "searchServiceKey" -Value $searchServiceKey
            
        $searchServiceQueryKey = az search query-key list --resource-group $azureResource.ResourceGroup --service-name $azureResource.Name --query [0].key --out tsv
        # TODO use ID trick 
        Add-Param -Name "searchServiceQueryKey" -Value $searchServiceQueryKey
    
        Save-Parameters
    }
}

function Invoke-SearchAPI {
    param (
        [string]$url,
        [string]$body,
        [string]$method = "PUT"
    )
    
    if (! $params.searchServiceKey) {
        Get-SearchServiceKeys
    }

    $headers = @{
        'api-key'      = $params.searchServiceKey
        'Content-Type' = 'application/json'
        'Accept'       = 'application/json'
    }
    $baseSearchUrl = "https://" + $params.searchServiceName + ".search.windows.net"
    $fullUrl = $baseSearchUrl + $url
    
    Write-Host -Message ("Calling Search API " + $method + ": '" + $fullUrl + "'")
    
    Invoke-RestMethod -Uri $fullUrl -Headers $headers -Method $method -Body $body | ConvertTo-Json -Depth 100
}

function Get-SearchMgtUrl () {
    param (
        [Parameter(Mandatory = $false)]
        [string]$ServiceName,
        [Parameter(Mandatory = $false)]
        [string]$ResourceGroup,
        [Parameter(Mandatory = $false)]
        [string]$ApiVersion
    )

    if (-not $ServiceName) {
        $ServiceName = $params.searchServiceName
    }
    if (-not $ResourceGroup) {
        $ResourceGroup = $config.resourceGroupName
    }

    $mgturl = "https://management.azure.com/subscriptions/" + $config.subscriptionId + "/resourceGroups/" + $ResourceGroup + "/providers/Microsoft.Search/searchServices/" + $ServiceName

    if ($ApiVersion){
        $mgturl += "?api-version=" + $ApiVersion
    }
    else {
        $mgturl += "?api-version=" + $searchcfg.Parameters.searchManagementVersion
    }
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
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "aliases") -Recurse
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
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "synonyms") -Recurse
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
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "indexes") -Recurse
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
    
function Remove-SearchIndex {
    param (
        [string]$name,
        [switch]$DeleteAliases
    )
    if ( $name ) {

        if ( $DeleteAliases) {
            Update-SearchAliases -method DELETE
        }

        $headers = @{
            'api-key'      = $params.searchServiceKey
            'Content-Type' = 'application/json'
            'Accept'       = 'application/json'
        }
        $url = ("/indexes/" + $name + "?api-version=" + $searchcfg.Parameters.searchVersion)
        $baseSearchUrl = "https://" + $params.searchServiceName + ".search.windows.net"
        $fullUrl = $baseSearchUrl + $url

        Invoke-RestMethod -Uri $fullUrl -Headers $headers -Method Delete
    }
}
    
function Update-SearchDataSource {
    param (
        [string]$name
    )
    Write-Debug -Message "Creating/Updating existing Search DataSource(s)"
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "datasources")
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
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "skillsets")
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
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "indexers")
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
    
function Search-Query {
    param (
        [string]$query = "*"
    )
    $headers = @{
        'api-key'      = $params.searchServiceKey
        'Content-Type' = 'application/json'
        'Accept'       = 'application/json'
    }
    $baseSearchUrl = "https://" + $params.searchServiceName + ".search.windows.net"
    $fullUrl = $baseSearchUrl + "/indexes/" + $params.indexName + "/docs?search=" + $query + "&api-version=" + $searchcfg.Parameters.searchVersion
    
    Write-Debug -Message ("CallingGet  api: '" + $fullUrl + "'")
    Invoke-RestMethod -Uri $fullUrl -Headers $headers -Method Get
};
    
# https://docs.microsoft.com/en-us/rest/api/searchservice/reset-indexer
    
# POST https://[service name].search.windows.net/indexers/[indexer name]/reset?api-version=[api-version]  
#   Content-Type: application/json  
#   api-key: [admin key]
    
function Reset-SearchIndexer {
    param (
        [string]$name
    )
    Write-Host "Reset Search Indexer(s)"
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "indexers")
    
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
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "indexers")
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
    
        if ( $name ) {
            if ( $jsonobj.name.indexOf($name) -ge 0) {
                Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "/run?api-version=" + $searchcfg.Parameters.searchVersion) -method "POST" 
            }
        }
        else {
            Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "/run?api-version=" + $searchcfg.Parameters.searchVersion) -method "POST" 
        }
    }
};
    
function Get-SearchIndexersStatus {
    $indexersStatus = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "indexers")
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
    foreach ($azureResource in $searchcfg.Items) {

        Write-Host "Search Service details for "$azureResource.Name

        $exists = az search service show --name $azureResource.Name --resource-group $azureResource.ResourceGroup --query id --out tsv

        if ( $exists ) {
            $mgturl = Get-SearchMgtUrl -ServiceName $azureResource.Name -ResourceGroup $azureResource.ResourceGroup

            az rest --method GET --url $mgturl
        }
    }
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

function Enable-SemanticSearch () {
    param (
        [Parameter(Mandatory = $false)]
        [string]$ServiceName,
        [Parameter(Mandatory = $false)]
        [string]$ResourceGroup
    )

    $mgturl = Get-SearchMgtUrl -ServiceName $ServiceName -ResourceGroup $ResourceGroup -ApiVersion $searchcfg.Parameters.searchManagementVersion

    Push-Location (Join-Path $global:envpath "services" "search" "semantic")
    az rest --method PUT --url $mgturl --body '@enable.json'
    Pop-Location
}

function Disable-SemanticSearch () {
    param (
        [Parameter(Mandatory = $false)]
        [string]$ServiceName,
        [Parameter(Mandatory = $false)]
        [string]$ResourceGroup
    )

    $mgturl = Get-SearchMgtUrl -ServiceName $ServiceName -ResourceGroup $ResourceGroup -ApiVersion $searchcfg.Parameters.searchManagementVersion

    Push-Location (Join-Path $global:envpath "services" "search" "semantic")
    az rest --method PUT --url $mgturl --body '@disable.json'
    Pop-Location
}

function Suspend-Search {
    # Suspend the indexers default scheduling.
    # https://learn.microsoft.com/en-us/azure/search/search-howto-schedule-indexers?tabs=rest#configure-a-schedule
    Write-Host "Suspend Indexers' Scheduling..."

    $files = Get-ChildItem -File -Path (join-path $global:envpath "services" "search" "indexers")
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
        $jsonobj.schedule = $null
        $updatedCfg = (Convertto-json $jsonobj -Depth 100)
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

    function Format-ErrorKey ($searchError) {
        $id = $searchError.key.Split("&")[0].Replace("localId=", "");
        $id = [System.Web.HttpUtility]::UrlDecode($id);
        # Find the base url of the document url to remove it.
        foreach ($storageUrl in $params.StorageContainerAddresses) {
            if ($id.indexOf($storageUrl) -ge 0) {
                $baseurl = $storageUrl
            }
        }
        return [System.Web.HttpUtility]::UrlDecode($id.Replace($baseurl + "/", ""));
    }

    # $now = Get-Date -Format "yyyyMMddHHmmss"

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
        if (-not $SkipHistory) {
            $executions = $status.executionHistory

            # $executions | format-table -AutoSize

            foreach ($exec in $executions) {
            
                if ( $exec.itemsFailed -gt 0) {
                    Write-Host ("failed " + $exec.startTime + " " + $exec.endTime + " " + $exec.itemsProcessed + " " + $exec.itemsFailed) -ForegroundColor DarkMagenta
                }
                else {
                    Write-Host ($exec.status + " " + $exec.startTime + " " + $exec.endTime + " " + $exec.itemsProcessed + " " + $exec.itemsFailed) -ForegroundColor DarkGreen
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
        [string]$SourcePath,
        [string]$Timestamp,
        [string]$subfolder=""
    )
    
    $releasePath = Join-Path $global:envpath "releases"
    Test-DirectoryExistence $releasePath
    
    Test-DirectoryExistence (join-path $releasePath "functions")
    Test-DirectoryExistence (join-path $releasePath "functions" "windows")
    Test-DirectoryExistence (join-path $releasePath "functions" "linux")

    Test-DirectoryExistence (join-path $releasePath "webapps")
    Test-DirectoryExistence (join-path $releasePath "webjobs")
    
    
    $releases = Get-ChildItem -Directory $SourcePath -Recurse | Where-Object { $_.Name -match $Timestamp }

    foreach ($release in $releases) {
        Write-host "Zipping "$release.Name
        $reldestpath = join-path $SourcePath ($release.Name + ".zip")
        Push-Location $release.FullName
        Compress-Archive -Path ".\*" -DestinationPath $reldestpath -Force
        Pop-Location
    
        $zipname = ((Join-Path $releasePath $subfolder $release.Parent.Name $release.Name.Replace($Timestamp, "")) + "latest.zip")
        Write-Host "Copying to release "$zipname
        Copy-Item $reldestpath $zipname -Force

        # Clean the build folder (disk space reclaim)
        Remove-Item $release.FullName -Force -Recurse
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

# function Get-FunctionSettings {
#     param (
#         [string]$Name
#     )
    
#     $settings = az functionapp config appsettings list --name $Name --resource-group $functionscfg.ResourceGroup | ConvertFrom-Json
    
#     return $settings
# }

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
    
                $storekey = $params.techStorageConnectionString
    
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
    Add-KeyVaultFunctionsPolicies
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
    
    $deploymentdir = Test-DirectoryExistence (join-Path $global:envpath "build" "functions")
    
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
                
                        if (Test-Path -Path $respath) {
                            Write-Host ("Cleaning old directory " + $functionApp.Name) -ForegroundColor DarkCyan
                            Remove-Item -Path $respath -Recurse -ErrorAction SilentlyContinue -Force
                            Start-Sleep -Seconds 5
                        }

                        # Test if the directory exists, create it if not
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

    if ( -not $LinuxOnly) {
        Compress-Release -SourcePath (Join-Path $deploymentdir "windows") -Timestamp $now -subfolder "functions"
    }
    
    if ( -not $WindowsOnly) {
        Compress-Release -SourcePath (Join-Path $deploymentdir "linux") -Timestamp $now -subfolder "functions"
    }
    
    # add build version evertytime we build the webapp
    Add-Param -Name "FunctionsBuildVersion" -Value $now
        
    Sync-Config

    if ( $Publish ) {
        Publish-Functions -LinuxOnly:$LinuxOnly -WindowsOnly:$WindowsOnly
    }
    if ( $KeyVaultPolicies ) {
        Add-KeyVaultFunctionsPolicies        
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
    
    $basepath = join-path "releases" "functions"

    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.Services) {
            if (! $plan.IsLinux) {
                if (-not $LinuxOnly) {
                    Write-host "Publishing Windows function "$functionApp.Name
    
                    $releasepath = join-path $basepath "windows" ($functionApp.Name + ".publish.latest.zip")
                    az webapp deployment source config-zip --resource-group $plan.ResourceGroup --name $functionApp.Name --src $releasepath
                }
            }
            else {
                if (-not $WindowsOnly) {
                    if ($functionApp.Path) {
                        Write-host "Publishing Python function "$functionApp.Name
    
                        $releasepath = join-path $basepath "linux" ($functionApp.Name + ".publish.latest.zip")
    
                        $unzipPath = join-path $basepath "linux" $functionApp.Name
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

function Publish-FunctionsSettings() {
    param (
        [switch] $LinuxOnly,
        [switch] $WindowsOnly
    )
    function Publish-FunctionSettings() {
        param (
            $plan,
            $functionApp
        )
    
        $settingspath = "services/functions/" + $functionApp.Id + ".json" 
        
        if (Test-Path $settingspath) {
            az webapp config appsettings set -g $plan.ResourceGroup -n $functionApp.Name --settings @$settingspath
        }
    
        $settingspath = "services/functions/" + $functionApp.Id + "." + $config.id + ".json"
    
        if (Test-Path $settingspath) {
            az webapp config appsettings set -g $plan.ResourceGroup -n $functionApp.Name --settings @$settingspath
        }
    }
    
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
                    Add-Param -Name ($functionApp.Id + "." + $function.Name + ".url") -Value $url
        
                    try {
                        $fkey = az functionapp function keys list -g $plan.ResourceGroup -n $functionApp.Name --function-name $function.Name --query default --out tsv
                        if ( $fkey ) {
                            $furl = $url + "?code=" + $fkey
                            Write-host $furl
                            Add-Param -Name ($functionApp.Id + "." + $function.Name) -Value $furl
                            Add-Param -Name ($functionApp.Id + "." + $function.Name + ".key") -Value $fkey    
                        }
                    }
                    catch {
                        Add-Param -Name ($functionApp.Id + "." + $function.Name) -Value $url
                        Add-Param -Name ($functionApp.Id + "." + $function.Name + ".key") -Value ""                    
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
                        
                        $storekey = $params.techStorageConnectionString
    
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
                    if ( !(Test-WebAppExistence $webApp.Name) && !($webApp.Name.endswith("job"))) {
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

            # App Service Logging
            az webapp log config --application-logging azureblobstorage `
                --resource-group $plan.ResourceGroup `
                --name $webApp.Name `
                --detailed-error-messages true `
                --failed-request-tracing true

            # Assign a system managed identity
            az webapp identity assign -g $plan.ResourceGroup -n $webApp.Name
            Write-Host "Assigned System identity " -ForegroundColor DarkYellow
                
            # FTP State to FTPS Only
            az webapp config set -g $plan.ResourceGroup -n $webApp.Name --ftps-state FtpsOnly
            Write-Host "FTPS Only " -ForegroundColor DarkYellow

            # Disable FTP and SCM Basic Authentication
            $parent ="sites/"+$webApp.Name 
            az resource update --resource-group $plan.ResourceGroup `
            --name ftp `
            --namespace Microsoft.Web `
            --resource-type basicPublishingCredentialsPolicies `
            --parent $parent `
            --set properties.allow=false

            az resource update --resource-group $plan.ResourceGroup `
            --name scm `
            --namespace Microsoft.Web `
            --resource-type basicPublishingCredentialsPolicies `
            --parent $parent `
            --set properties.allow=false

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
    
    $deploymentdir = Test-DirectoryExistence (join-Path $global:envpath "build" "webapps")
    
    $now = Get-Date -Format "yyyyMMddHHmmss"
    function publish_windows($function) {
        Write-Host $pwd
        $buildpath = join-path $deploymentdir ($function + ".publish." + $now)
        Write-Host $buildpath
        try {
            dotnet publish -c RELEASE -o $buildpath | Out-Null
        }
        catch {
            Write-Host "Exception occurred in publish: "$_.Exception.Message -ForegroundColor DarkRed
        }
        return $buildpath
    }
    function publish_linux($function) {
        Write-Host $pwd
        $buildpath = join-path $deploymentdir ($function + ".publish." + $now)
        Write-Host $buildpath
        dotnet publish -r linux-x64 --self-contained false -c RELEASE -o $buildpath | Out-Null
        return $buildpath
    }
    
    # dotnet publish -r linux-x64 --self-contained false
    
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            if (-not $webApp.Image) {
    
                Write-Host "Building Cross-Platform WebApp "$webApp.Name -ForegroundColor DarkGreen
                # Build the corresponding Web App
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
    
    Compress-Release -SourcePath $deploymentdir -Timestamp $now
    
    # add build version evertytime we build the webapp
    Add-Param -Name "WebAppBuildVersion" -Value $now
    Sync-Parameters

    Sync-Config
    
    if ( $Publish ) {
        Publish-WebApps -LinuxOnly:$LinuxOnly -WindowsOnly:$WindowsOnly 
    }
    if ( $KeyVaultPolicies ) {
        Add-KeyVaultWebAppsPolicies 
    }
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
        Write-Host "Deploying plan "$plan.Name -ForegroundColor DarkYellow
      
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
    
            $settingspath = "services/webapps/" + $webApp.Id + ".json" 
    
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
    
            $settingspath = "services/webapps/" + $webApp.Id + "." + $config.id + ".json"
    
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

#region Azure WebJobs
function New-WebJobs {    
   
    foreach ($plan in $webjobscfg.AppPlans) {
      
        # Create a Windows plan
        if (!(Test-AppPlanExistence $plan.Name)) {
            az appservice plan create -g $plan.ResourceGroup `
                --name $plan.Name `
                --location $config.location `
                --sku $plan.Sku   
        }

        # Create the webjob app service 
        if ( !(Test-WebAppExistence $plan.AppserviceName)) {
            az webapp create `
                --name $plan.AppserviceName `
                --plan $plan.Name `
                --resource-group $plan.ResourceGroup `
                --https-only true `
                --runtime 'dotnet:6'
                            
            if ($config.stagingUIEnabled) {
                az webapp deployment slot create --name $plan.AppserviceName `
                    --resource-group $plan.ResourceGroup `
                    --https-only true `
                    --slot staging `
                    --configuration-source $plan.AppserviceName    
            }
        }
        # Use 64 bits worker process
        az webapp config set -g $plan.ResourceGroup -n $plan.AppserviceName --use-32bit-worker-process false

        # App Service Logging
        az webapp log config --application-logging azureblobstorage `
            --resource-group $plan.ResourceGroup `
            --name $plan.AppserviceName `
            --detailed-error-messages true `
            --failed-request-tracing true   
    
        # Assign a system managed identity
        az webapp identity assign -g $plan.ResourceGroup -n $plan.AppserviceName
            
        # FTP State to FTPS Only
        az webapp config set -g $plan.ResourceGroup -n $plan.AppserviceName --ftps-state FtpsOnly
    }
}

function Build-WebJobs {
    param (
        [switch] $Publish,
        [switch] $KeyVaultPolicies,
        [switch] $Settings
    )
    
    $deploymentdir = Test-DirectoryExistence (join-Path $global:envpath "build" "webjobs")
    
    $now = Get-Date -Format "yyyyMMddHHmmss"
    function publish_windows($function) {
        Write-Host $pwd
        $buildpath = join-path $deploymentdir ($function + ".publish." + $now)
        Write-Host $buildpath
        try {
            dotnet publish -c RELEASE -o $buildpath | Out-Null
        }
        catch {
            Write-Host "Exception occurred in publish: "$_.Exception.Message -ForegroundColor DarkRed
        }
        return $buildpath
    }
       
    # dotnet publish -r linux-x64 --self-contained false
    
    foreach ($plan in $webjobscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            Write-Host "Building Cross-Platform WebApp "$webApp.Name -ForegroundColor DarkGreen
            # Build the corresponding Web App
            $appLocation = (join-path $global:workpath ".." $webApp.Path)
            Write-Host $appLocation -ForegroundColor DarkGreen
    
            Push-Location $appLocation
                
            $respath = publish_windows $webApp.Name
                
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
    Compress-Release -SourcePath $deploymentdir -TimeStamp $now
    
    # add build version evertytime we build the webapp
    Add-Param -Name "WebAppBuildVersion" -Value $now
    Sync-Parameters
    
    Sync-Config
    
    if ( $Publish ) {
        Publish-WebJobs 
    }
    if ( $KeyVaultPolicies ) {
        Add-KeyVaultWebJobsPolicies
    }
    if ( $Settings ) {
        Publish-WebJobsSettings 
    }
}  

function Restore-WebJobs {
    
    New-WebJobs
    
    Build-WebJobs -Publish -KeyVaultPolicies -Settings

    Set-WebAppServicesAccessRestriction
}

function Publish-WebJobs {
    param (
        [switch] $Production,
        [string] $Slot = "staging"
    )
    
    if (-not $config.stagingUIEnabled) {
        $Production = $true
    }
    
    Push-Location $global:envpath 
    foreach ($plan in $webjobscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            $releasepath = "releases/webjobs/" + $webApp.Name + ".publish.latest.zip"
            
            $kuduCredentials = getKuduCreds $plan.AppserviceName $plan.ResourceGroup
            webjobDeployment $plan.AppserviceName $webApp.Name $kuduCredentials $releasepath
        }
    }
    Pop-Location
}

function getKuduCreds($appName, $resourceGroupName) {
    Write-Host "Getting Publish credentials" -ForegroundColor DarkYellow
  
    $user = az webapp deployment list-publishing-profiles -n $appName -g $resourceGroupName `
        --query "[?publishMethod=='MSDeploy'].userName" -o tsv
  
    $pass = az webapp deployment list-publishing-profiles -n $appName -g $resourceGroupName `
        --query "[?publishMethod=='MSDeploy'].userPWD" -o tsv
  
    $pair = "$($user):$($pass)"
  
    $encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
  
    return $encodedCreds
}

function webjobDeployment([string]$appName, [string]$webjobName, [string]$encodedCreds, [string]$publishZip) {
    Write-Host ("Deploying the webjob : " + $webjobName + " for Web App: " + $appName) -ForegroundColor DarkYellow
    Write-Host ("====================================================================")
    try {
        $ZipHeaders = @{
            Authorization         = "Basic {0}" -f $encodedCreds
            "Content-Disposition" = "attachment; filename=run.cmd"
        }
  
        # upload the job using the Kudu WebJobs API
        Invoke-WebRequest -Uri https://$appName.scm.azurewebsites.net/api/continuouswebjobs/$webjobName -Headers $ZipHeaders `
            -InFile $publishZip -ContentType "application/zip" -Method Put
       
        Write-Host ("Webjob deployed in Ready state") -ForegroundColor DarkYellow
        az webapp webjob continuous start --webjob-name $webjobName --name $appName --resource-group $config.resourceGroupName
  
        Write-Host ("Web Job now started in continous mode") -ForegroundColor DarkYellow
        Write-Host ("====================================================================")
    }
    catch {
        Write-Host ("Error occurred while deploying the web job : " + $webjobName + " Message : " + $_.Exception.Message) -ForegroundColor Red
    }
}

function Publish-WebJobsSettings {
    param (
        [switch] $Production,
        [string] $Slot = "staging"
    )
    
    if (-not $config.stagingUIEnabled) {
        $Production = $true
    }
    
    # Make sure we have the latest configuration & parameters in
    Sync-Config
    
    Push-Location $global:envpath
    foreach ($plan in $webjobscfg.AppPlans) {
        foreach ($webApp in $plan.Services) {
            Write-Host $webApp
    
            $settingspath = "services/webjobs/" + $webApp.Id + ".json" 
    
            if (Test-Path $settingspath) {
                Write-Host ("Path Exists " + $settingspath)

                if ($production) {
                    az webapp config appsettings set -g $plan.ResourceGroup `
                        -n $plan.AppserviceName `
                        --settings @$settingspath
                }
                else {
                    az webapp config appsettings set -g $plan.ResourceGroup `
                        -n $plan.AppserviceName `
                        --slot $Slot `
                        --settings @$settingspath
                }
            }
        }
    }
    
    $settingspath = "services/webjobs/" + $webApp.Id + "." + $config.id + ".json"
    
    if (Test-Path $settingspath) {
        if ($production) {
            az webapp config appsettings set -g $plan.ResourceGroup `
                -n $plan.AppserviceName `
                --settings @$settingspath
        }
        else {
            az webapp config appsettings set -g $plan.ResourceGroup `
                -n $plan.AppserviceName `
                --slot $Slot `
                --settings @$settingspath
        }
    }
                             
    Pop-Location
}
#endregion

#region Docker 

function Build-DockerImages {
    param (
        [string] $Id,
        [string] $Prefix,
        [switch] $WebApp,
        [switch] $WebJob,
        [switch] $Local
    )
    
    $dockercfg.Images | Foreach-Object -ThrottleLimit 1 -Parallel {
        #Action that will run in Parallel. Reference the current object via $PSItem and bring in outside variables with $USING:varname
        $image = $PSItem

        Write-Host ("Docker Image Id:" + $image.Id + " Name:" + $image.Name)
    
        $build = $false
  
        if ($USING:WebApp) {
            if ($image.webapp) {
                $build = $true
            }
        }
        elseif ($USING:WebJob) {
            if ($image.webjob) {
                $build = $true
            }
        }
        elseif (-not $USING:Id) {
            if ($USING:Prefix) {
                if ($image.Id.StartsWith($USING:Prefix)) {
                    $build = $true
                }
            }
            else {
                $build = $true
            }
        }
        elseif ($USING:Id -and ($USING:Id -eq $image.Id)) {
            $build = $true
        }
  
        if ($build) {
            Write-Host ("Building " + $image.Name) -ForegroundColor DarkYellow
  
            $imgdockerfile = join-path $USING:workpath $image.Path Dockerfile
  
            if ($image.BuildContext) {
                Push-Location (join-path $USING:workpath $image.BuildContext)
            }
            else {
                Push-Location (join-path $USING:workpath $image.Path)
            }
  
            if ($USING:Local) {
                docker build -t $image.Name -f $imgdockerfile .
            }
            else {
                az acr build --platform linux --image $image.Name --registry $USING:params.acr --file $imgdockerfile --only-show-errors . 
            }
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
        return $true
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

function Add-KeyVaultFunctionsPolicies {
    
    # Shared Policies for Functions
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.Services) {
            $principalId = az functionapp identity show -n $functionApp.Name -g $plan.ResourceGroup --query principalId --out tsv
    
            az keyvault set-policy -n $params.keyvault --object-id $principalId --secret-permissions get 
        }
    }
}

function Add-KeyVaultWebAppsPolicies {

    foreach ($plan in $webappscfg.AppPlans) {
    
        foreach ($webApp in $plan.Services) {
    
            $principalId = az webapp identity show -n $webApp.Name -g $plan.ResourceGroup --query principalId --output tsv
    
            az keyvault set-policy --name $params.keyvault --object-id $principalId --secret-permissions get
    
            if ($config.stagingUIEnabled -and -not $plan.IsLinux) {
    
                $principalId = az webapp identity show -n $webApp.Name -g $plan.ResourceGroup --slot staging --query principalId --output tsv
    
                az keyvault set-policy -n $params.keyvault --object-id $principalId --secret-permissions get
    
            }
    
        }
    
    }
    
}
    
function Add-KeyVaultWebJobsPolicies {
    
    foreach ($plan in $webjobscfg.AppPlans) {
        $principalId = az webapp identity show -n $plan.AppserviceName -g $plan.ResourceGroup --query principalId --output tsv
        
        az keyvault set-policy -n $params.keyvault --object-id $principalId --secret-permissions get
    
        if ($config.stagingUIEnabled) {
    
            $principalId = az webapp identity show -n $plan.AppserviceName -g $plan.ResourceGroup --slot staging --query principalId --output tsv
    
            az keyvault set-policy -n $params.keyvault --object-id $principalId --secret-permissions get
        }
    
    }
    
}
#endregion
    
#region Solution 

function Clear-Solution() {

    Remove-Item $global:envpath -Recurse -Force
}

function Build-Solution {
    param (
        [switch] $Publish
    )
    Build-DockerImages
    Build-Functions
    Build-WebApps
    Build-WebJobs

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

    # if ($RebuildIndex) {
    #     Remove-SearchIndex -name $params. -DeleteAliases
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

function Publish-Environment {
    param (
        [switch] $CloudShell
    )
    
    Sync-Config
    
    $dirName = [System.IO.Path]::GetFileName($global:envpath)
    
    Push-Location $global:envpath
        
    if ( Test-Path "build") {
        Remove-Item "build" -Recurse -Force
    }
    $reldestpath = join-path ".." ($dirName + ".zip")
    Compress-Archive -Path ".\*" -DestinationPath $reldestpath -Force
    
    if ( $CloudShell) {
        az storage file upload --account-name $params.techStorageAccountName --account-key $params.techStorageAccountKey --share-name "cloudshell" --source $reldestpath
    }
    Pop-Location
}

function Get-Environment {
    param (
        [string] $Name
    )
      
    $reldestpath = join-path $Name ".zip"
    
    az storage file download --account-name $params.techStorageAccountName --account-key $params.techStorageAccountKey --share-name "cloudshell" --path $reldestpath --dest ".."
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

    Restore-WebJobs
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
                    Add-Param -Name ($azureResource.Parameter + "Key") -Value $cogServicesKey
                }
    
                # Endpoint
                $cogEndpoint = az cognitiveservices account show -n $azureResource.Name -g $azureResource.ResourceGroup --query properties.endpoint --out tsv 
                if ( $cogEndpoint -and $cogEndpoint.Length -gt 0 ) {
                    Add-Param -Name ($azureResource.Parameter + "Endpoint") -Value $cogEndpoint
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

    az cognitiveservices account deployment create `
        -g $openaicfg.Items[0].ResourceGroup `
        -n $openaicfg.Items[0].Name `
        --deployment-name $DeploymentName `
        --model-name $ModelName `
        --model-version $ModelVersion  `
        --model-format OpenAI `
        --scale-settings-scale-type "Standard"    
}

function Remove-OpenAIModel() { 
    param (
        [string] $DeploymentName
    )

    az cognitiveservices account deployment delete `
        -g $openaicfg.Items[0].ResourceGroup `
        -n $openaicfg.Items[0].Name `
        --deployment-name $DeploymentName    
}
#endregion

#region AKS

function Get-AKSCredentials {
    if ($akscfg.enable) {
        $existingAKS = az aks list --query [].name --out tsv
        foreach ($azureResource in $akscfg.Items) {
            if ($azureResource.Name -in $existingAKS) {
                # Get AKS credentials
                az aks get-credentials -n $azureResource.Name -g $azureResource.ResourceGroup            
            }
        }
    }
}

function Initialize-AKS() {

    kubectl apply -f (join-path $global:envpath "services/aks/config-map.yaml")
    
    # Deploy-FunctionsAKS
    # Deploy-WebAppsAKS
    kubectl apply -f (join-path $global:envpath "services/aks/webapps/tika.yaml")

    # For each docker image, find the corresponding yaml file and apply it to the cluster
    # kubectl apply -f (join-path $global:envpath "services/aks/ingress.yaml")
}
function Deploy-FunctionsAKS() {

    $template = Get-Content (join-path $global:envpath "services/aks/functions/template.yaml") -Raw

    foreach ($plan in $functionscfg.AppPlans) {
        Write-Host "Plan "$plan.Name
        foreach ($functionApp in $plan.Services) {
            # Load the function and corresponding settings
            $settingspath = "services/functions/" + $functionApp.Id + ".json" 
        
        }
    }
}

#endregion


Export-ModuleMember -Function *
