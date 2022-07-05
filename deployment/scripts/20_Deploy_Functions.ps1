param(
    [string]$EnvironmentName,
    [switch]$DockerBuild,
    [switch]$NoProvision,
    [switch]$Upgrade
    )

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

Write-Host $ScriptDir

if ($EnvironmentName)
{
    Write-Host "Selected Environment "$EnvironmentName
    $initPath=Join-Path $ScriptDir ".." "init_env.ps1"
    .  $initPath -Name $EnvironmentName -NoLogin
}

Write-Host "Start script..."

Start-Transcript

if ($DockerBuild) {
    Build-DockerImages;
}
else {
    Write-Host "Skipping Docker images build..."
}

if ($NoProvision) {
    Write-Host "Skipping Provisionning..."
}
else {
    New-Functions;
}

if ($Upgrade) {
    Upgrade-Functions;
}

# Build & Publish
Build-Functions;

Publish-Functions;

# KV & Settings 
Add-KeyVaultFunctionsPolicies;

Get-FunctionsKeys;
Sync-Parameters;

Publish-FunctionsSettings;

Stop-Transcript