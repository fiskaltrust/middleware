#Requires -RunAsAdministrator
<#
.SYNOPSIS
    One-time setup script to add HTTP URL reservations so the test launcher
    can bind HTTP ports without running as administrator.

.DESCRIPTION
    WCF HTTP self-hosting on .NET Framework requires a URL reservation (urlacl)
    for non-admin processes. Run this script once as administrator to grant
    the current user permission to bind the specified ports.

.PARAMETER Ports
    The HTTP ports to reserve. Defaults to 1200 and 1500.

.EXAMPLE
    # Run as administrator:
    .\Setup-UrlReservations.ps1
    .\Setup-UrlReservations.ps1 -Ports 1500,1501,1502
#>
param(
    [int[]]$Ports = @(1200, 1500)
)

$userName = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name

foreach ($port in $Ports) {
    $url = "http://+:$port/"
    Write-Host "Adding URL reservation: $url for user $userName" -ForegroundColor Cyan
    netsh http add urlacl url=$url user=$userName
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Success" -ForegroundColor Green
    } else {
        Write-Host "  Failed (may already exist)" -ForegroundColor Yellow
    }
}

Write-Host "`nDone. You can now run the test launcher without admin rights." -ForegroundColor Green
