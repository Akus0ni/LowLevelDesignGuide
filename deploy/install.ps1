#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs the LLD Guide API on this PC as a Windows service behind ngrok (spec 002).

.DESCRIPTION
    Idempotent: safe to run again after pulling new code (it stops the service, republishes and restarts it).
    Steps:
      1. Check prerequisites (.NET SDK, ngrok agent).
      2. Publish the API to C:\LldGuide\api and the admin console to C:\LldGuide\admin.
      3. Register the "LldGuideApi" Windows service (virtual account, automatic start, restart on failure).
      4. Create %ProgramData%\LldGuide\{data,secrets} and the backup folder with tight ACLs.
      5. Create any missing secret with "lld-admin hosting init" (JWT key, status key pair, ntfy topic).
      6. Render ngrok.yml and install ngrok as a Windows service.
      7. Keep the PC awake on AC power.
      8. Register the daily backup task (13:00, catches up after a missed run).
      9. Smoke check: local and public /health.
    Data, secrets and backups are never deleted. See docs/hosting.md.

.PARAMETER Domain
    Your free ngrok domain, e.g. "example-name.ngrok-free.dev" (no https://).

.PARAMETER AuthToken
    Your ngrok authtoken. Asked for (hidden input) only when ngrok.yml does not exist yet.

.EXAMPLE
    .\deploy\install.ps1 -Domain example-name.ngrok-free.dev
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z0-9-]+(\.[a-z0-9-]+)+$')]
    [string] $Domain,

    [string] $AuthToken,

    [int] $Port = 5280,

    # No spaces: the path is passed unquoted to sc.exe (Windows PowerShell 5.1 mangles embedded quotes).
    [ValidatePattern('^[^ ]+$')]
    [string] $InstallRoot = 'C:\LldGuide',

    [string] $BackupPath = 'D:\Applications\LldGuide\backups'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ServiceName = 'LldGuideApi'
$ServiceAccount = "NT SERVICE\$ServiceName"
$TaskName = 'LldGuide Backup'
$RepoRoot = Split-Path -Parent $PSScriptRoot
$ApiDir = Join-Path $InstallRoot 'api'
$AdminDir = Join-Path $InstallRoot 'admin'
$AdminExe = Join-Path $AdminDir 'lld-admin.exe'
$DataRoot = Join-Path $env:ProgramData 'LldGuide'
$SecretsDir = Join-Path $DataRoot 'secrets'
$NgrokConfig = Join-Path $DataRoot 'ngrok.yml'
$InstallingUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name

# Well-known SIDs keep the ACLs language-independent.
$System = '*S-1-5-18'
$Administrators = '*S-1-5-32-544'
$Users = '*S-1-5-32-545'

function Write-Step([string] $Message) { Write-Host "==> $Message" -ForegroundColor Cyan }

function Invoke-Native([string] $File, [string[]] $Arguments) {
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "'$File $($Arguments -join ' ')' failed with exit code $LASTEXITCODE." }
}

function Set-ExactAcl([string] $Path, [string[]] $Grants) {
    # Replace inherited permissions with exactly these grants (inherited by children).
    Invoke-Native 'icacls.exe' (@($Path, '/inheritance:r', '/grant:r') + $Grants + @('/Q'))
}

# 1. Prerequisites ---------------------------------------------------------------------------------------------
Write-Step 'Checking prerequisites'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK not found. Install .NET 10 first (see README).' }
$ngrok = Get-Command ngrok -ErrorAction SilentlyContinue
if (-not $ngrok) { throw 'ngrok not found on PATH. Follow docs/hosting.md section 1, open a new terminal, then re-run.' }
$ngrokExe = $ngrok.Source

# 2. Publish ---------------------------------------------------------------------------------------------------
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing -and $existing.Status -ne 'Stopped' -and $PSCmdlet.ShouldProcess($ServiceName, 'Stop service')) {
    Write-Step "Stopping $ServiceName"
    Stop-Service -Name $ServiceName
    $existing.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
}

if ($PSCmdlet.ShouldProcess($InstallRoot, 'Publish API and admin console')) {
    Write-Step "Publishing to $InstallRoot"
    Invoke-Native 'dotnet' @('publish', (Join-Path $RepoRoot 'src\Lld.Api\Lld.Api.csproj'), '-c', 'Release', '-o', $ApiDir, '--nologo', '-v', 'q')
    Invoke-Native 'dotnet' @('publish', (Join-Path $RepoRoot 'src\Lld.Admin\Lld.Admin.csproj'), '-c', 'Release', '-o', $AdminDir, '--nologo', '-v', 'q')
}

# 3. API service -----------------------------------------------------------------------------------------------
if ($PSCmdlet.ShouldProcess($ServiceName, 'Register Windows service')) {
    Write-Step "Registering service $ServiceName"
    $binPath = Join-Path $ApiDir 'Lld.Api.exe'
    if (-not $existing) {
        Invoke-Native 'sc.exe' @('create', $ServiceName, "binPath=", $binPath, 'start=', 'auto', 'obj=', $ServiceAccount, 'DisplayName=', 'LLD Guide API')
        Invoke-Native 'sc.exe' @('description', $ServiceName, 'LLD Guide API (self-hosted behind ngrok). See docs/hosting.md.')
    }
    else {
        Invoke-Native 'sc.exe' @('config', $ServiceName, "binPath=", $binPath, 'start=', 'auto', 'obj=', $ServiceAccount)
    }
    # Restart after 5 s, 10 s, then 60 s; reset the failure count after a day.
    Invoke-Native 'sc.exe' @('failure', $ServiceName, 'reset=', '86400', 'actions=', 'restart/5000/restart/10000/restart/60000')
}

# 4. Folders and ACLs ------------------------------------------------------------------------------------------
if ($PSCmdlet.ShouldProcess($DataRoot, 'Create folders and set ACLs')) {
    Write-Step 'Creating folders and setting permissions'
    foreach ($dir in @($InstallRoot, (Join-Path $DataRoot 'data'), $SecretsDir, $BackupPath)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    # Binaries: everyone can run them; only administrators can change them.
    Set-ExactAcl $InstallRoot @("${System}:(OI)(CI)F", "${Administrators}:(OI)(CI)F", "${Users}:(OI)(CI)RX")
    # Database + secrets: the service, administrators and you (to run lld-admin without elevation).
    Set-ExactAcl $DataRoot @("${System}:(OI)(CI)F", "${Administrators}:(OI)(CI)F", "${ServiceAccount}:(OI)(CI)M", "${InstallingUser}:(OI)(CI)M")
    # Backups hold password hashes: no service access needed (the backup task runs as SYSTEM).
    Set-ExactAcl $BackupPath @("${System}:(OI)(CI)F", "${Administrators}:(OI)(CI)F", "${InstallingUser}:(OI)(CI)M")
}

# 5. Secrets ---------------------------------------------------------------------------------------------------
if ($PSCmdlet.ShouldProcess($SecretsDir, 'Create missing secrets (lld-admin hosting init)')) {
    Write-Step 'Creating missing secrets'
    Invoke-Native $AdminExe @('hosting', 'init', '--public-url', "https://$Domain")
}

# 6. ngrok -----------------------------------------------------------------------------------------------------
if ($PSCmdlet.ShouldProcess($NgrokConfig, 'Write ngrok config and install the ngrok service')) {
    Write-Step 'Configuring ngrok'
    if (-not $AuthToken -and (Test-Path $NgrokConfig)) {
        $match = Select-String -Path $NgrokConfig -Pattern '^\s*authtoken:\s*(\S+)' | Select-Object -First 1
        if ($match) { $AuthToken = $match.Matches[0].Groups[1].Value }
    }
    if (-not $AuthToken) {
        $secure = Read-Host 'ngrok authtoken (input hidden)' -AsSecureString
        $AuthToken = [System.Net.NetworkCredential]::new('', $secure).Password
    }
    if (-not $AuthToken) { throw 'An ngrok authtoken is required (dashboard.ngrok.com > Your Authtoken).' }

    $template = Get-Content -Raw (Join-Path $PSScriptRoot 'ngrok.template.yml')
    $config = $template.Replace('__AUTHTOKEN__', $AuthToken).Replace('__DOMAIN__', $Domain).Replace('__PORT__', "$Port")
    [System.IO.File]::WriteAllText($NgrokConfig, $config, [System.Text.UTF8Encoding]::new($false))
    # The authtoken is a credential: SYSTEM (the ngrok service) and administrators only.
    Invoke-Native 'icacls.exe' @($NgrokConfig, '/inheritance:r', '/grant:r', "${System}:F", "${Administrators}:F", '/Q')
    Invoke-Native $ngrokExe @('config', 'check', '--config', $NgrokConfig)

    if (Get-Service -Name 'ngrok' -ErrorAction SilentlyContinue) {
        & $ngrokExe service stop | Out-Null
        Invoke-Native $ngrokExe @('service', 'uninstall')
    }
    Invoke-Native $ngrokExe @('service', 'install', '--config', $NgrokConfig)
}

# 7. Power -----------------------------------------------------------------------------------------------------
if ($PSCmdlet.ShouldProcess('Power plan', 'Never sleep or hibernate on AC power')) {
    Write-Step 'Keeping the PC awake on AC power'
    Invoke-Native 'powercfg.exe' @('/change', 'standby-timeout-ac', '0')
    Invoke-Native 'powercfg.exe' @('/change', 'hibernate-timeout-ac', '0')
}

# 8. Backup task -----------------------------------------------------------------------------------------------
if ($PSCmdlet.ShouldProcess($TaskName, 'Register daily backup task')) {
    Write-Step "Registering scheduled task '$TaskName' (daily 13:00, catches up after a missed run)"
    $action = New-ScheduledTaskAction -Execute $AdminExe -Argument 'backup run' -WorkingDirectory $AdminDir
    $trigger = New-ScheduledTaskTrigger -Daily -At '13:00'
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -ExecutionTimeLimit (New-TimeSpan -Minutes 30)
    $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force | Out-Null
}

# 9. Start and smoke check -------------------------------------------------------------------------------------
if ($PSCmdlet.ShouldProcess('Services', 'Start and check health')) {
    Write-Step 'Starting services'
    Start-Service -Name $ServiceName
    Invoke-Native $ngrokExe @('service', 'start')

    $checks = @(
        @{ Name = 'local'; Url = "http://localhost:$Port/health" },
        @{ Name = 'public'; Url = "https://$Domain/health" }
    )
    foreach ($check in $checks) {
        $ok = $false
        for ($i = 0; $i -lt 30 -and -not $ok; $i++) {
            try {
                $response = Invoke-WebRequest -Uri $check.Url -UseBasicParsing -TimeoutSec 5 -Headers @{ 'ngrok-skip-browser-warning' = '1' }
                $ok = $response.StatusCode -eq 200
            }
            catch { Start-Sleep -Seconds 2 }
        }
        if ($ok) { Write-Host "    $($check.Name) health: OK ($($check.Url))" -ForegroundColor Green }
        else { Write-Warning "$($check.Name) health did not answer: $($check.Url). See docs/hosting.md > Troubleshooting." }
    }
}

Write-Host ''
Write-Host 'Done. Next steps (docs/hosting.md):' -ForegroundColor Green
Write-Host "  - First admin user:   & '$AdminExe' users create <you@example.com> --role admin"
Write-Host "  - Request budget:     & '$AdminExe' status budget"
Write-Host '  - Put the public key and topic printed above into the app settings, then rebuild the app.'
