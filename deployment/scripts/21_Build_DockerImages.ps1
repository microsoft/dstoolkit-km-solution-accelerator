param(
    [Parameter(Mandatory=$true)]
    [string]$EnvironmentName,
    [string]$Prefix
)

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

Write-Host $ScriptDir

if ($EnvironmentName) {
    Write-Host "Selected Environment "$EnvironmentName
    $initPath = Join-Path $ScriptDir ".." "init_env.ps1"
    .  $initPath -Name $EnvironmentName -NoLogin
}

Start-Transcript

Build-DockerImages -Prefix $Prefix

Stop-Transcript