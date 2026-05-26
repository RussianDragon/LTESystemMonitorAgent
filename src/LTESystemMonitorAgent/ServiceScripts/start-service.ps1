$ErrorActionPreference = 'Stop'

$serviceName = 'LTESystemMonitorAgent'
$displayName = 'LTE System Monitor Agent'
$description = 'Collects system metrics and dispatches them to HTTP API.'
$applicationDirectory = Join-Path $PSScriptRoot 'LTESystemMonitorAgent'
$exePath = Join-Path $applicationDirectory 'LTESystemMonitorAgent.exe'

if (-not (Test-Path -LiteralPath $exePath)) {
    $exePath = Join-Path $PSScriptRoot 'LTESystemMonitorAgent.exe'
}

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)

    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Restart-AsAdministrator {
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        "`"$PSCommandPath`""
    )

    Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -Verb RunAs
}

if (-not (Test-Administrator)) {
    Write-Host 'Requesting administrator privileges...'
    Restart-AsAdministrator
    exit 0
}

try {
    if (-not (Test-Path -LiteralPath $exePath)) {
        throw "Application file was not found: $exePath"
    }

    $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

    if ($null -eq $service) {
        Write-Host "Installing service $serviceName..."

        New-Service `
            -Name $serviceName `
            -BinaryPathName "`"$exePath`"" `
            -DisplayName $displayName `
            -Description $description `
            -StartupType Automatic | Out-Null
    }

    $service = Get-Service -Name $serviceName

    if ($service.Status -ne 'Running') {
        Write-Host "Starting service $serviceName..."
        Start-Service -Name $serviceName
    }
    else {
        Write-Host "Service $serviceName is already running."
    }

    Get-Service -Name $serviceName | Format-List Name, Status, StartType
}
catch {
    Write-Error $_
    Read-Host 'Press Enter to close'
    exit 1
}

Read-Host 'Press Enter to close'
