param([string]$EnvironmentName)

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

if ($EnvironmentName) {
    Write-Host "Selected Environment "$EnvironmentName
    $initPath = Join-Path $ScriptDir ".." "init_env.ps1"
    .  $initPath -Name $EnvironmentName -NoLogin
}

Start-Transcript

az config set extension.use_dynamic_install=yes_without_prompt

Get-AllServicesKeys

# Ensure all keys are stored in the KeyVault
Add-KeyVaultSecrets

Stop-Transcript