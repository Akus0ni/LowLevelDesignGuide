#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Removes the LLD Guide API service, the ngrok service and the backup task (spec 002).

.DESCRIPTION
    Keeps the database, secrets, ngrok config and backups, and prints where they are.
    Power settings are left as they are (restore them in Settings > System > Power if you want).
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $InstallRoot = 'C:\LldGuide',
    [string] $BackupPath = 'D:\Applications\LldGuide\backups'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ServiceName = 'LldGuideApi'
$TaskName = 'LldGuide Backup'
$DataRoot = Join-Path $env:ProgramData 'LldGuide'

if ((Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) -and $PSCmdlet.ShouldProcess($ServiceName, 'Stop and delete service')) {
    Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
    & sc.exe delete $ServiceName | Out-Null
    Write-Host "Removed service $ServiceName"
}

$ngrok = Get-Command ngrok -ErrorAction SilentlyContinue
if ($ngrok -and (Get-Service -Name 'ngrok' -ErrorAction SilentlyContinue) -and $PSCmdlet.ShouldProcess('ngrok', 'Stop and uninstall service')) {
    & $ngrok.Source service stop | Out-Null
    & $ngrok.Source service uninstall | Out-Null
    Write-Host 'Removed service ngrok'
}

if ((Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) -and $PSCmdlet.ShouldProcess($TaskName, 'Unregister task')) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "Removed scheduled task '$TaskName'"
}

Write-Host ''
Write-Host 'Kept (delete by hand only if you are sure):'
Write-Host "  Binaries:            $InstallRoot"
Write-Host "  Database + secrets:  $DataRoot"
Write-Host "  Backups:             $BackupPath"
