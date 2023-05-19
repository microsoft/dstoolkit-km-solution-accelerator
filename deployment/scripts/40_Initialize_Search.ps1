param([string]$EnvironmentName)

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

Write-Host $ScriptDir

if ($EnvironmentName) {
    Write-Host "Selected Environment "$EnvironmentName
    $initPath = Join-Path $ScriptDir ".." "init_env.ps1"
    .  $initPath -Name $EnvironmentName -NoLogin
}

Start-Transcript

# Configure the Search Service
Initialize-Search

Stop-Transcript