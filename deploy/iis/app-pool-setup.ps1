<#
.SYNOPSIS
  Provision the Prima Nota site and application pool on IIS (Windows Server).

.DESCRIPTION
  Idempotent: re-running this script reconfigures the site without destroying data.
  Run as Administrator on the IIS host.

.PARAMETER Environment
  Deployment environment label: Staging or Production.

.PARAMETER SiteName
  IIS site name. Defaults to "PrimaNota.$Environment".

.PARAMETER PhysicalPath
  Absolute path where the published artifact lives (the site root).

.PARAMETER Hostname
  HTTPS binding hostname (e.g. primanota-staging.azienda.local).

.PARAMETER CertificateThumbprint
  Thumbprint of the HTTPS certificate already installed in the LocalMachine\My store.

.PARAMETER AppPoolUser
  Managed service account used by the app pool. Defaults to a MSA name.

.EXAMPLE
  PS> .\app-pool-setup.ps1 -Environment Staging `
       -PhysicalPath D:\Apps\PrimaNota.Staging `
       -Hostname primanota-staging.azienda.local `
       -CertificateThumbprint 0123ABCD...
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Staging', 'Production')]
    [string] $Environment,

    [string] $SiteName = "PrimaNota.$Environment",

    [Parameter(Mandatory = $true)]
    [string] $PhysicalPath,

    [Parameter(Mandatory = $true)]
    [string] $Hostname,

    [Parameter(Mandatory = $true)]
    [string] $CertificateThumbprint,

    [string] $AppPoolUser = "PrimaNotaPool$Environment"
)

$ErrorActionPreference = 'Stop'

Import-Module WebAdministration

$appPoolName = $SiteName

# --- App Pool ---------------------------------------------------------------
if (-not (Test-Path "IIS:\AppPools\$appPoolName")) {
    New-WebAppPool -Name $appPoolName | Out-Null
    Write-Host "Created app pool $appPoolName"
}

Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name managedRuntimeVersion -Value ''  # No managed .NET
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name startMode -Value 'AlwaysRunning'
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name recycling.periodicRestart.time -Value '00:00:00'
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name processModel.identityType -Value 'ApplicationPoolIdentity'

# --- Site -------------------------------------------------------------------
if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    New-Website -Name $SiteName `
        -PhysicalPath $PhysicalPath `
        -ApplicationPool $appPoolName `
        -HostHeader $Hostname `
        -Ssl `
        -Port 443 | Out-Null
    Write-Host "Created site $SiteName bound to $Hostname:443"
} else {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $PhysicalPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $appPoolName
}

# --- HTTPS binding certificate ---------------------------------------------
$binding = Get-WebBinding -Name $SiteName -Protocol https -ErrorAction SilentlyContinue
if (-not $binding) {
    New-WebBinding -Name $SiteName -Protocol https -Port 443 -HostHeader $Hostname -SslFlags 1 | Out-Null
}

$sslPath = "IIS:\SslBindings\!443!$Hostname"
if (-not (Test-Path $sslPath)) {
    Get-Item "Cert:\LocalMachine\My\$CertificateThumbprint" | New-Item $sslPath -Force | Out-Null
    Write-Host "Bound certificate $CertificateThumbprint to $Hostname:443"
}

# --- Start ------------------------------------------------------------------
if ((Get-WebAppPoolState -Name $appPoolName).Value -ne 'Started') {
    Start-WebAppPool -Name $appPoolName
}

if ((Get-WebsiteState -Name $SiteName).Value -ne 'Started') {
    Start-Website -Name $SiteName
}

Write-Host "Prima Nota site '$SiteName' ready at https://$Hostname/"
