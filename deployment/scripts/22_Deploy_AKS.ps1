param([string]$EnvironmentName)

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

if ($EnvironmentName)
{
    Write-Host "Selected Environment "$EnvironmentName
    $initPath=Join-Path $ScriptDir ".." "init_env.ps1"
    .  $initPath -Name $EnvironmentName -NoLogin
}

Start-Transcript

Initialize-AKS

Stop-Transcript