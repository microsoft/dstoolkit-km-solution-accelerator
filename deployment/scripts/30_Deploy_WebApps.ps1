param(
    [string]$EnvironmentName,
    [switch]$NoProvision
    )

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

Write-Host $ScriptDir

if ($EnvironmentName)
{
    Write-Host "Selected Environment "$EnvironmentName
    $initPath=Join-Path $ScriptDir ".." "init_env.ps1"
    .  $initPath -Name $EnvironmentName -NoLogin
}

# Start-Transcript

# if ($NoProvision) {
#     Write-Host "Skipping Provisionning..."
# }
# else {
#     New-WebApps;
# }

# Ensure we have the webapp docker images built.
Build-DockerImages -WebApp

# Build & Publish
Build-WebApps -Publish -KeyVaultPolicies -Settings

Sync-Config

# Ensure the Web Apps when set are access restricted
# Set-WebAppServicesAccessRestriction

# Stop-Transcript