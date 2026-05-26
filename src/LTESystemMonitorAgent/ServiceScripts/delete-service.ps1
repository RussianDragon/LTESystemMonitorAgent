$ErrorActionPreference = 'Stop'

$serviceName = 'LTESystemMonitorAgent'

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
    $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

    if ($null -eq $service) {
        Write-Host "Service $serviceName is not installed."
        Read-Host 'Press Enter to close'
        exit 0
    }

    if ($service.Status -ne 'Stopped') {
        Write-Host "Stopping service $serviceName..."
        Stop-Service -Name $serviceName

        $service = Get-Service -Name $serviceName
        $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
    }

    Write-Host "Deleting service $serviceName..."
    & sc.exe delete $serviceName | Out-Host

    Write-Host "Service $serviceName was deleted."
}
catch {
    Write-Error $_
    Read-Host 'Press Enter to close'
    exit 1
}

Read-Host 'Press Enter to close'
