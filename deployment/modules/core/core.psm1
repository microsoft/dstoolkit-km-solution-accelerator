#region Modules
# function Update-Modules {
#     Import-Module (join-path $modulePath "infra") -Global -DisableNameChecking -Force
#     Import-Module (join-path $modulePath "core") -Global -DisableNameChecking -Force
#     Import-Module (join-path $modulePath "vnet") -Global -DisableNameChecking -Force    
# }
#endregion 

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

function Import-Functions() {
    # Import Other configurations like functions
    $global:functionscfg = [string] (Get-Content -Path (join-path $global:envpath "config" "functions" "config.json"))
    $global:functionscfg = ConvertFrom-Json $global:functionscfg

    Import-ConfigParameters $global:functionscfg
}
function Import-WebAppsConfig() {
    # Import Other configurations like functions
    $global:webappscfg = [string] (Get-Content -Path (join-path $global:envpath "config" "webapps" "config.json"))
    $global:webappscfg = ConvertFrom-Json $global:webappscfg

    Import-ConfigParameters $global:webappscfg
}
function Import-DockerConfig() {
    # Import Other configurations like functions
    $global:dockercfg = [string] (Get-Content -Path (join-path $global:envpath "config" "docker" "config.json"))
    $global:dockercfg = ConvertFrom-Json $global:dockercfg

    Import-ConfigParameters $global:dockercfg
}
function Import-StorageConfig() {
    # Import Other configurations like functions
    $global:storagecfg = [string] (Get-Content -Path (join-path $global:envpath "config" "storage" "config.json"))
    $global:storagecfg = ConvertFrom-Json $global:storagecfg

    Import-ConfigParameters $global:storagecfg
}

function Import-CognitiveServicesConfig() {
    # Import Other configurations like functions
    $global:cogservicescfg  = [string] (Get-Content -Path (join-path $global:envpath "config" "cogservices" "config.json"))
    $global:cogservicescfg  = ConvertFrom-Json $global:cogservicescfg

    Import-ConfigParameters $global:cogservicescfg
}
function Import-ContainerRegistryConfig() {
    # Import Other configurations like functions
    $global:conregistrycfg = [string] (Get-Content -Path (join-path $global:envpath "config" "containerregistry" "config.json"))
    $global:conregistrycfg = ConvertFrom-Json $global:conregistrycfg

    Import-ConfigParameters $global:conregistrycfg
}
function Import-keyvaultConfig() {
    # Import Other configurations like functions
    $global:keyvaultcfg = [string] (Get-Content -Path (join-path $global:envpath "config" "keyvault" "config.json"))
    $global:keyvaultcfg = ConvertFrom-Json $global:keyvaultcfg

    Import-ConfigParameters $global:keyvaultcfg
}
function Import-searchserviceConfig() {
    # Import Other configurations like functions
    $global:searchservicecfg = [string] (Get-Content -Path (join-path $global:envpath "config" "search" "config.json"))
    $global:searchservicecfg = ConvertFrom-Json $global:searchservicecfg

    Import-ConfigParameters $global:searchservicecfg
}
function Import-bingConfig() {
    # Import Other configurations like functions
    $global:bingcfg = [string] (Get-Content -Path (join-path $global:envpath "config" "bing" "config.json"))
    $global:bingcfg = ConvertFrom-Json $global:bingcfg

    Import-ConfigParameters $global:bingcfg
}
function Import-mapsConfig() {
    # Import Other configurations like functions
    $global:mapscfg = [string] (Get-Content -Path (join-path $global:envpath "config" "maps" "config.json"))
    $global:mapscfg = ConvertFrom-Json $global:mapscfg

    Import-ConfigParameters $global:mapscfg
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
    
    Sync-Config
    Sync-Parameters

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

    Import-Functions
    Import-DockerConfig
    Import-WebAppsConfig
    Import-StorageConfig
    Import-CognitiveServicesConfig
    Import-ContainerRegistryConfig
    Import-keyvaultConfig
    Import-searchserviceConfig

    Import-bingConfig
    Import-mapsConfig

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
    
    Add-ServicesParameters

    if ($PSVersionTable.Platform -eq "Win32NT") {
        Write-Debug "Decrypt secured strings..."
        $parameterslist = Get-Member -InputObject $global:params -MemberType NoteProperty

        foreach ($prop in $parameterslist) {
            if ( $prop.Name.endswith("Key") -or $prop.Name.endswith("ConnectionString") ) {
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

function Add-ServicesParameters {

    if ( Test-FileExistence (Join-path $global:envpath ("pricing." + $config.id + ".json"))) {
        Add-ExtendedParameters ("pricing." + $config.id + ".json")
    }
    else {
        Add-ExtendedParameters "pricing.json"
    }

    if ( Test-FileExistence (Join-path $global:envpath ("services." + $config.id + ".json"))) {
        Add-ExtendedParameters ("services." + $config.id + ".json")
    }
    else {
        Add-ExtendedParameters "services.json"
    }
  
    # Container
    $dataStorageContainerName = $params.storageContainers[0];
    Add-Param "dataStorageContainerName" $dataStorageContainerName
    
    # Create the containers entries for UI SAS access
    $StorageContainerAddresses = @()
    foreach ($container in $params.storageContainers) {
        $url = "https://" + $global:params.dataStorageAccountName + ".blob.core.windows.net/" + $container
        $StorageContainerAddresses += $url
    }
    Add-Param "StorageContainerAddressesAsString" $([String]::Join(',', $StorageContainerAddresses))
    
    Initialize-SearchConfig
}

function Add-Param($name, $value) {
    if ( $global:params.PSobject.Properties.name -eq $name) {
        $global:params.$name = $value
    }
    else {
        $global:params | Add-Member -MemberType NoteProperty -Name $name -Value $value -ErrorAction Ignore
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
function Save-Parameters() {
    
    # Create a blank object 
    $securedparams = New-Object -TypeName PSObject

    if ($global:params) {
        $parameterslist = Get-Member -InputObject $global:params -MemberType NoteProperty

        foreach ($prop in $parameterslist) {
    
            $propValue = Get-ParamValue $prop.Name
            
            if ($PSVersionTable.Platform -eq "Win32NT") {
                if ( $prop.Name.endswith("Key") -or $prop.Name.endswith("ConnectionString") ) {
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
    
function Save-Config() {
    $global:config | ConvertTo-Json -Depth 100 -Compress | Out-File -FilePath (Join-Path $global:envpath "config.json") -Force -Encoding utf8
}
    
function Sync-Config() {
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
    
    Write-Debug -Message "Configuration synched "
}

function Sync-Parameters() {

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
}
    
function Sync-Modules() {
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
    
function Get-DeploymentOverlayPath() {
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
    
    Sync-Config
        
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
    
function Get-ContainerFilesList ($container, $path) {
    
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
    
    az storage fs file metadata update --file-system $container --path $path --account-name $params.dataStorageAccountName --account-key $params.storageAccountKey  --metadata AzureSearch_RetryTag=$now
    
}
#endregion
    
function Get-AllServicesKeys() {
    Get-AppInsightsInstrumentationKey
    Get-TechStorageAccountParameters
    Get-DataStorageAccountParameters
    Get-CognitiveServiceKey
    Get-AzureMapsSubscriptionKey
    Get-FunctionsKeys
    Get-SearchServiceKeys
    
    Sync-Parameters
}
function Get-AppInsightsInstrumentationKey {
    $tuples = Get-Parameters "appInsightsService"
    
    foreach ($tuple in $tuples) {
        $key = az monitor app-insights component show --app $tuple.Value -g $config.resourceGroupName --query instrumentationKey  --out tsv
    
        if ( $key -and $key.length -gt 0 ) {
            Add-Param ($tuple.Name + "Key") $key
        }            
    }
    Save-Parameters
    
    $tuples = Get-Parameters "appInsightsWindows"
    
    foreach ($tuple in $tuples) {
        $key = az monitor app-insights component show --app $tuple.Value -g $config.resourceGroupNameWindows --query instrumentationKey  --out tsv
    
        if ( $key -and $key.length -gt 0 ) {
            Add-Param ($tuple.Name + "Key") $key
        }            
    }
    Save-Parameters
    
    $tuples = Get-Parameters "appInsightsLinux"
    
    foreach ($tuple in $tuples) {
        $key = az monitor app-insights component show --app $tuple.Value -g $config.resourceGroupNameLinux --query instrumentationKey  --out tsv
    
        if ( $key -and $key.length -gt 0 ) {
            Add-Param ($tuple.Name + "Key") $key
        }            
    }
    Save-Parameters
}
    
function Get-TechStorageAccountParameters {
    
    $techStorageAccountKey = az storage account keys list --account-name $params.techStorageAccountName -g $config.resourceGroupName --query [0].value  --out tsv
    Add-Param "techStorageAccountKey" $techStorageAccountKey
    
    $techStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=' + $params.techStorageAccountName + ';AccountKey=' + $techStorageAccountKey + ';EndpointSuffix=core.windows.net'
    Add-Param "techStorageConnectionString" $techStorageConnectionString
    
    Save-Parameters
}
function Get-DataStorageAccountParameters {
    
    $global:storageAccountKey = az storage account keys list --account-name $params.dataStorageAccountName -g $config.resourceGroupName --query [0].value --out tsv
    Add-Param "storageAccountKey" $global:storageAccountKey
    
    $global:storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=' + $params.dataStorageAccountName + ';AccountKey=' + $global:storageAccountKey + ';EndpointSuffix=core.windows.net'
    Add-Param "storageConnectionString" $global:storageConnectionString
    
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
                Add-Param ($azureResource.Parameter+"Key") $cogServicesKey
            }
        }
    }

    Save-Parameters
}
    
function Get-AzureMapsSubscriptionKey {
    
    if ($params.mapSearchEnabled) {
        $mapsKey = az maps account keys list --name $params.maps --resource-group $config.resourceGroupName --query primaryKey --out tsv
        Add-Param "mapsSubscriptionKey" $mapsKey
    
        Save-Parameters
    }
}
    
#region SEARCH
    
function Initialize-SearchConfig {

    if ($searchservicecfg.searchBlobPartitions) {
        Write-Host "Blob partitionning enabled ..."
        for ($i = 0; $i -lt $searchservicecfg.searchBlobPartitions.Count; $i++) {
            $partitionName = $searchservicecfg.searchBlobPartitions[$i]
    
            $indexerPath = join-path $global:envpath "config" "search" "indexers" "documents.json"
            if ( test-path $indexerPath) {
                # Create a partition datasource for documents
                $datasource = Get-Content -Path (join-path $global:envpath "config" "search" "datasources" "documents.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ($config.name + "-documents-" + $i)
                $jsonobj.container | Add-Member -MemberType NoteProperty -Name "query" -Value $partitionName -ErrorAction Ignore
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "config" "search" "datasources" ("documents-" + $i + ".json")) -Force
    
                # Create a partition indexer for documents
                $datasource = Get-Content -Path (join-path $global:envpath "config" "search" "indexers" "documents.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ($config.name + "-documents-" + $i)
                $jsonobj.dataSourceName = ($config.name + "-documents-" + $i)
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "config" "search" "indexers" ("documents-" + $i + ".json")) -Force    
            }
    
            $indexerPath = join-path $global:envpath "config" "search" "indexers" "images.json"
            if ( test-path $indexerPath) {
                # Create a partition datasource for images
                $datasource = Get-Content -Path (join-path $global:envpath "config" "search" "datasources" "images.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ($config.name + "-images-" + $i)
                $jsonobj.container | Add-Member -MemberType NoteProperty -Name "query" -Value $partitionName -ErrorAction Ignore
                $jsonobj | ConvertTo-Json -Depth 100 | Out-File -FilePath $(join-path $global:envpath "config" "search" "datasources" ("images-" + $i + ".json")) -Force
    
                # Create a partition indexer for images
                $datasource = Get-Content -Path (join-path $global:envpath "config" "search" "indexers" "images.json") -Raw
                $jsonobj = ConvertFrom-Json $datasource
                $jsonobj.name = ($config.name + "-images-" + $i)
                $jsonobj.dataSourceName = ($config.name + "-images-" + $i)
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
        $value = ($config.name + "-" + $item)
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
        $value = ($config.name + "-" + $item)
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
        $value = ($config.name + "-" + $item)
        Add-Param $item"Name" $value
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
        $value = ($config.name + "-" + $item)
        Add-Param $item"DataSource" $value
        Add-Param $item"StorageContainerName" $item            
        $datasourceslist += $value
    }
    Add-Param "searchDataSources" $datasourceslist
    Write-Debug -Message "Parameters DataSources created"
    
    # Get the list of Indexers
    $indexersList = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexers")
    foreach ($file in $files) {
        $item = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $value = ($config.name + "-" + $item)
        Add-Param $item"Indexer" $value
    
        $indexersList += $value
    }
    Add-Param "searchIndexers" ($indexersList | Join-String -Property $_ -Separator ",")
    Add-Param "searchIndexersList" $indexersList
    Write-Debug -Message "Parameters Indexers created"
}
    
function Get-SearchServiceKeys {
    
    $global:searchServiceKey = az search admin-key show --resource-group $config.resourceGroupName --service-name $params.searchServiceName  --query primaryKey --out tsv
    Add-Param "searchServiceKey" $global:searchServiceKey
        
    $searchServiceQueryKey = az search query-key list --resource-group $config.resourceGroupName --service-name $params.searchServiceName  --query [0].key --out tsv
    Add-Param "searchServiceQueryKey" $searchServiceQueryKey
    
    Save-Parameters
}
function Invoke-SearchAPI {
    param (
        [string]$url,
        [string]$body,
        [string]$method = "PUT"
    )
    
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
        Invoke-SearchAPI -url ("/aliases/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion) -body $configBody -method $method
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
        Invoke-SearchAPI -url ("/synonymmaps/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion) -body $configBody -method $method
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
                Invoke-SearchAPI -url ("/indexes/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion + "&allowIndexDowntime=" + $AllowIndexDowntime) -body $configBody
            }    
        }
        else {
            Invoke-SearchAPI -url ("/indexes/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion + "&allowIndexDowntime=" + $AllowIndexDowntime) -body $configBody
        }
    }
}
    
function Remove-SearchIndex {
    param (
        [string]$name
    )
    if ( $name ) {
        $headers = @{
            'api-key'      = $params.searchServiceKey
            'Content-Type' = 'application/json'
            'Accept'       = 'application/json'
        }
        $url = ("/indexes/" + $name + "?api-version=" + $searchservicecfg.Parameters.searchVersion)
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
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "datasources")
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody

        if ( $name ) {
            if ($jsonobj.name.indexOf($name) -ge 0) {
                Invoke-SearchAPI -url ("/datasources/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion) -body $configBody
            }    
        }
        else {
            Invoke-SearchAPI -url ("/datasources/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion) -body $configBody
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
                Invoke-SearchAPI -url ("/skillsets/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion) -body $configBody
            }    
        }
        else {
            Invoke-SearchAPI -url ("/skillsets/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion) -body $configBody
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
                Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion) -body $configBody
            }    
        }
        else {
            if ($jsonobj.name.indexOf("spo") -ge 0) {
                Write-Host "Skipping SharePoint Indexer re-configuration." -ForegroundColor DarkRed
            }
            else {
                Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion) -body $configBody
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
    $fullUrl = $baseSearchUrl + "/indexes/" + $params.indexName + "/docs?search=" + $query + "&api-version=" + $searchservicecfg.Parameters.searchVersion
    
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
    
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexers")
    
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
    
        if ( $name ) {
            if ( $jsonobj.name.indexOf($name) -ge 0) {
                Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "/reset?api-version=" + $searchservicecfg.Parameters.searchVersion) -method "POST"
            }    
        }
        else {
            Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "/reset?api-version=" + $searchservicecfg.Parameters.searchVersion) -method "POST"
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
                Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "/run?api-version=" + $searchservicecfg.Parameters.searchManagementVersion) -method "POST" 
            }
        }
        else {
            Invoke-SearchAPI -url ("/indexers/" + $jsonobj.name + "/run?api-version=" + $searchservicecfg.Parameters.searchManagementVersion) -method "POST" 
        }
    }
};
    
function Get-SearchIndexersStatus {
    $indexersStatus = @()
    $files = Get-ChildItem -File -Path (join-path $global:envpath "config" "search" "indexers")
    foreach ($file in $files) {
        $configBody = [string] (Get-Content -Path $file.FullName)
        $jsonobj = ConvertFrom-Json $configBody
        $status = Invoke-SearchAPI -Method GET -url ("/indexers/" + $jsonobj.name + "/status?api-version=" + $searchservicecfg.Parameters.searchVersion)
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
    # if ($item) {
    #     $indexerBody = [string] (Get-Content -Path (join-path $global:envpath $("\config\search\indexers\"+$item+".json")))
    #     $jsonobj = ConvertFrom-Json $indexerBody
    #     $baseSearchUrl = "https://"+$params.searchServiceName+".search.windows.net"
    #     $fullUrl = $baseSearchUrl + "/indexers/"+$jsonobj.name+"/status?api-version="+$searchservicecfg.Parameters.searchVersion
        
    #     Write-Host "CallingGet  api: '"$fullUrl"'";
    #     Invoke-RestMethod -Uri $fullUrl -Headers $headers -Method Get    
    # }
    # else {
    #     Write-Host "Please provide an indexer name.";
    # }
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
            $status = Invoke-SearchAPI -Method GET -url ("/indexers/" + $jsonobj.name + "?api-version=" + $searchservicecfg.Parameters.searchVersion)    
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
        $status = Invoke-SearchAPI -Method GET -url ("/indexers/" + $jsonobj.name + "/status?api-version=" + $searchservicecfg.Parameters.searchVersion)    
        return $(ConvertFrom-Json $status)
    }
    else {
        Write-Host "Please provide an indexer name.";
    }
}
    
function Get-SearchServiceDetails() {
    # az rest --method GET --url ("https://management.azure.com/subscriptions/" + $config.subscriptionId + "/resourceGroups/" + $config.resourceGroupName + "/providers/Microsoft.Search/searchServices/" + $params.searchServiceName + "?api-version=" + $searchservicecfg.Parameters.searchManagementVersion)
    az rest --method GET --url ("https://management.azure.com/subscriptions/" + $config.subscriptionId + "/resourceGroups/" + $config.resourceGroupName + "/providers/Microsoft.Search/searchServices/" + $params.searchServiceName + "?api-version=2021-04-01-Preview")
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
    Invoke-SearchAPI -url ("/indexers/documents/resetdocs?api-version=" + $searchservicecfg.Parameters.searchVersion) -method "POST" -body $body
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
    
#region Build functions
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
    Test-DirectoryExistence (join-path $releasePath "ui")
     
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
    # $excludeFolders = @('.venv', '.vscode', '__pycache__', 'tests', 'entities')
    # $excludeFoldersRegex = $excludeFolders -join '|'
        
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
    
        foreach ($functionApp in $plan.FunctionApps) {
            if ($plan.IsLinux) {
                $imageName = $params.acr + "/" + $functionApp.Image
    
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
                            # Create a Function App
                            az functionapp create --name $functionApp.Name `
                                --storage-account $params.techStorageAccountName `
                                --plan $plan.Name `
                                --resource-group $plan.ResourceGroup `
                                --functions-version $functionApp.Version `
                                --os-type Linux `
                                --https-only true `
                                --app-insights $params.appInsightsService `
                                --deployment-container-image-name $imageName                        
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
    
                # Use 64 bits worker process
                az functionapp config set -g $plan.ResourceGroup -n $functionApp.Name --use-32bit-worker-process false
            }
    
            # Assign a system managed identity
            az functionapp identity assign -g $plan.ResourceGroup -n $functionApp.Name
    
            # FTP State to FTPS Only
            az functionapp config set -g $plan.ResourceGroup -n $functionApp.Name --ftps-state FtpsOnly

            # HTTPS Only flag => now integrated in the create command
            # az functionapp update  -g $plan.ResourceGroup -n $functionApp.Name --set httpsOnly=true        
        }
    }
}
function Build-Functions () {
    param (
        [switch] $Publish,
        [switch] $LinuxOnly,
        [switch] $WindowsOnly
    )
    
    $deploymentdir = Test-DirectoryExistence (join-Path $global:envpath "build")
    
    $now = Get-Date -Format "yyyyMMddHHmmss"
    function build($function) {
        dotnet publish -c RELEASE -o (join-path $deploymentdir "windows" ($function + ".publish." + $now))
    }   
    
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.FunctionApps) {
            # Windows
            if (-not $plan.IsLinux) {
                if ( -not $LinuxOnly ) {
                    Write-Host ("Building Windows Function App" + $functionApp.Name) -ForegroundColor DarkCyan
    
                    # Build the configured functions
                    Push-Location (join-path $global:workpath ".." $functionApp.Path)
                    build $functionApp.Name
                    Pop-Location
                }
            }
            else {
                if ( -not $WindowsOnly) {
                    if ($functionApp.Path) {
                        Write-Host ("Building Linux-Python Function App" + $functionApp.Name) -ForegroundColor DarkCyan
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
        
    Sync-Parameters
    
    if ($Publish) {
        Publish-Functions -LinuxOnly:$LinuxOnly -WindowsOnly:$WindowsOnly
    }
}
    
function Publish-Functions() {   
    param (
        [switch] $LinuxOnly,
        [switch] $WindowsOnly
    )
        
    Push-Location $global:envpath
    
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.FunctionApps) {
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
    
function Upgrade-Functions() {   
    Push-Location $global:envpath
    
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.FunctionApps) {
            az functionapp config appsettings set --settings FUNCTIONS_EXTENSION_VERSION=~4 --resource-group $plan.ResourceGroup --name $functionApp.Name
    
            if (! $plan.IsLinux) {
                # For Windows function apps only, also enable .NET 6.0 that is needed by the runtime
                az functionapp config set --net-framework-version v6.0 --resource-group $plan.ResourceGroup --name $functionApp.Name
            }
        }
    }
    Pop-Location
}
    
function Restart-Functions() {   
    Push-Location $global:envpath
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.FunctionApps) {
            az functionapp stop --resource-group $plan.ResourceGroup --name $functionApp.Name
            az functionapp start --resource-group $plan.ResourceGroup --name $functionApp.Name 
        }
    }
    Pop-Location
}
    
function Publish-FunctionsSettings() {
    # Make sure we have the latest configuration & parameters in
    Sync-Config
    Sync-Parameters
    
    Push-Location $global:envpath
    foreach ($plan in $functionscfg.AppPlans) {                    
        foreach ($functionApp in $plan.FunctionApps) {
            $settingspath = "config/functions/" + $functionApp.Id + ".json" 
    
            if (Test-Path $settingspath) {
                az webapp config appsettings set -g $plan.ResourceGroup -n $functionApp.Name --settings @$settingspath
            }
    
            $settingspath = "config/functions/" + $functionApp.Id + "." + $config.id + ".json"
    
            if (Test-Path $settingspath) {
                az webapp config appsettings set -g $plan.ResourceGroup -n $functionApp.Name --settings @$settingspath
            }
        }
    }
    Pop-Location
}

function New-FunctionsKeys() {
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.FunctionApps) {
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
        foreach ($functionApp in $plan.FunctionApps) {
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
    
    Sync-Parameters
}
    
function Test-Functions() {
    param (
        [switch] $Local
    )

    # $results=@()

    foreach ($plan in $functionscfg.AppPlans) {
        Write-Host "--------------------"
        Write-Host "Testing Plan "$plan.name -ForegroundColor DarkCyan
        foreach ($functionApp in $plan.FunctionApps) {
            Write-Host "Testing App "$functionApp.name -ForegroundColor DarkYellow
            foreach ($function in $functionApp.Functions) {
                Write-Host "Testing Function "$function.name -ForegroundColor DarkBlue
                $url = az functionapp function show -g $plan.ResourceGroup -n $functionApp.Name --function-name $function.Name --query invokeUrlTemplate --out tsv
                if ($url) {
                    # $uri= [uri]::new($url)
                    # $uri.Scheme="https"
                    $url = $url.Replace("http://", "https://")   
                    try {
                        $fkey = az functionapp function keys list -g $plan.ResourceGroup -n $functionApp.Name --function-name $function.Name --query default --out tsv
                        if ( $fkey ) {
                            $furl = $url + "?code=" + $fkey
                        }
                    }
                    catch {
                    }
    
                    Write-Host $furl
    
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
    
        foreach ($webApp in $plan.WebApps) {
            if ($plan.IsLinux) {
                if (-not $WindowsOnly) {
                    $imageName = $params.acr + "/" + $webApp.Image
    
                    if ( !(Test-WebAppExistence $webApp.Name)) {
                        # Create a Web App
                        az webapp create --name $webApp.Name `
                            --plan $plan.Name `
                            --resource-group $plan.ResourceGroup `
                            --https-only true `
                            --deployment-container-image-name $imageName
                        
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
                    if ( !(Test-WebAppExistence $webApp.Name)) {
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
    
            # Assign a system managed identity
            az webapp identity assign -g $plan.ResourceGroup -n $webApp.Name
                
            # FTP State to FTPS Only
            az webapp config set -g $plan.ResourceGroup -n $webApp.Name --ftps-state FtpsOnly

            # HTTPS Only flag => integrated in the create command
            # az webapp update  -g $plan.ResourceGroup -n $webApp.Name --set httpsOnly=true
        }
    }
}
function Build-WebApps {
    param (
        [switch] $Publish,
        [switch] $LinuxOnly,
        [switch] $WindowsOnly
    )
    
    $deploymentdir = Test-DirectoryExistence (join-Path $global:envpath "build")
    
    $now = Get-Date -Format "yyyyMMddHHmmss"
    function publish_windows($function) {
        Write-Host $pwd
        $buildpath = join-path $deploymentdir "ui" ($function + ".publish." + $now)
        Write-Host $buildpath
        dotnet publish -c RELEASE -o $buildpath | Out-Null
        return $buildpath
    }
    function publish_linux($function) {
        Write-Host $pwd
        $buildpath = join-path $deploymentdir "ui" ($function + ".publish." + $now)
        Write-Host $buildpath
        dotnet publish -r linux-x64 --self-contained false -c RELEASE -o $buildpath | Out-Null
        return $buildpath
    }
    
    # dotnet publish -r linux-x64 --self-contained false
    
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.WebApps) {
            if (-not $webApp.Image) {
    
                Write-Host "Building Cross-Platform WebApp "$webApp.Name -ForegroundColor DarkGreen
                # Build the corresponding Web App
                $appLocation = (join-path $global:workpath ".." $webApp.Path)
                Write-Host $appLocation -ForegroundColor DarkGreen
    
                Push-Location $appLocation
                if ($plan.IsLinux) {
                    if (-not $WindowsOnly) {
                        $respath = publish_linux $webApp.Name
                    }
                }
                else {
                    if (-not $LinuxOnly) {
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
    
    Sync-Parameters
    
    if ( $Publish ) { Publish-WebApps -LinuxOnly:$LinuxOnly -WindowsOnly:$WindowsOnly } 
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
        foreach ($webApp in $plan.WebApps) {
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
    
        foreach ($webApp in $plan.WebApps) {
    
            if (-not $webApp.Image) {
                if (-not $plan.IsLinux) {
    
                    $releasepath = "releases/ui/" + $webApp.Name + ".publish.latest.zip"
    
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
    Sync-Parameters
    
    Push-Location $global:envpath
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.WebApps) {
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
    Sync-Parameters        
}
    
#endregion
    
#region Key Vault Methods
function Initialize-KeyVault {
    Add-KeyVaultSecrets
}
function Add-KeyVaultSecrets {
    
    $secretExpiryDate = ((get-date).ToUniversalTime().AddYears(2)).ToString("yyyy-MM-ddTHH:mm:ssZ")
    
    $params.PSObject.Properties | ForEach-Object {
        if ( $_.Name.endswith("Key") -or $_.Name.endswith("ConnectionString") ) {
            az keyvault secret set --name $_.Name --value $_.Value --vault-name $params.keyvault
            az keyvault secret set-attributes --vault-name $params.keyvault --name $_.Name --expires $secretExpiryDate
            Write-Host ("Added Secret to the Keyvault " + $_.Name) -ForegroundColor Green    
        }
    }
}
function Add-KeyVaultFunctionsPolicies {
    
    # Shared Policies for Functions
    foreach ($plan in $functionscfg.AppPlans) {
        foreach ($functionApp in $plan.FunctionApps) {
            $principalId = az functionapp identity show -n $functionApp.Name -g $plan.ResourceGroup --query principalId
    
            az keyvault set-policy -n $params.keyvault -g $plan.ResourceGroup --object-id $principalId --secret-permissions get 
        }
    }
}
function Add-KeyVaultWebAppsPolicies {
    foreach ($plan in $webappscfg.AppPlans) {
        foreach ($webApp in $plan.WebApps) {
            $principalId = az webapp identity show -n $webApp.Name -g $plan.ResourceGroup --query principalId
            az keyvault set-policy -n $params.keyvault -g $plan.ResourceGroup --object-id $principalId --secret-permissions get 
    
            if ($config.stagingUIEnabled -and -not $plan.IsLinux) {
                $principalId = az webapp identity show -n $webApp.Name -g $plan.ResourceGroup --slot staging --query principalId
                az keyvault set-policy -n $params.keyvault -g $plan.ResourceGroup --object-id $principalId --secret-permissions get 
            }
        }
    }
}
    
#endregion
    
#region Solution methods
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
    Sync-Parameters
    
    Publish-Functions
    Publish-WebApps
    
    # Publishing settings will restart all app services.
    Publish-FunctionsSettings
    Publish-WebAppsSettings
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
    Sync-Parameters
    
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
#endregion
    
Export-ModuleMember -Function *
