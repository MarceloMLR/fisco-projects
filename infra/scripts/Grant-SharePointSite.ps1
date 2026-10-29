<#
.SYNOPSIS
Gives the application of the installation the right to write in its SharePoint site, and nowhere else (ADR-0030, ADR-0120).

.DESCRIPTION
The application asks Microsoft Graph with the permission Sites.Selected, which the Terraform gives with the consent of
the administrator: it reaches no site until one is given to it. This script gives the site of sharepoint_site_url, from
local.auto.tfvars, with the role "write" (read and write files, nothing about permissions), to the application of
terraform output entra_client_id. It signs in to Microsoft Graph as a person who may manage every site (the delegated
permission Sites.FullControl.All, asked at the sign-in), such as a Global Administrator or a SharePoint Administrator,
and needs the modules Microsoft.Graph.Authentication and Microsoft.Graph.Sites:

    Install-Module Microsoft.Graph.Authentication, Microsoft.Graph.Sites -Scope CurrentUser

The site and the application may also come from -SiteUrl and -ClientId, so an administrator of the company may run
it with only the package of the version and the values given by who operates. Running it again keeps the right as it
is.

.EXAMPLE
./infra/scripts/Grant-SharePointSite.ps1

.EXAMPLE
./infra/scripts/Grant-SharePointSite.ps1 -ClientId 00000000-0000-0000-0000-000000000000 -SiteUrl https://empresa.sharepoint.com/sites/Projetos
#>
[CmdletBinding()]
param(
    [string] $ApplicationName = 'FiscoProjects',
    # The application of the installation in Entra ID; read from terraform output entra_client_id when not given.
    [string] $ClientId,
    # The SharePoint site of the files; read from sharepoint_site_url in local.auto.tfvars when not given.
    [string] $SiteUrl
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

foreach ($module in 'Microsoft.Graph.Authentication', 'Microsoft.Graph.Sites') {
    if (-not (Get-Module -ListAvailable -Name $module)) {
        throw "Falta o módulo ${module}: Install-Module Microsoft.Graph.Authentication, Microsoft.Graph.Sites -Scope CurrentUser"
    }
}

$terraform = Join-Path $PSScriptRoot '..\terraform'
if (-not $ClientId) {
    $ClientId = terraform "-chdir=$terraform" output -raw entra_client_id
}
$personal = Join-Path $terraform 'local.auto.tfvars'
if (-not $SiteUrl -and (Test-Path $personal)) {
    $SiteUrl = [regex]::Match((Get-Content $personal -Raw), '(?m)^\s*sharepoint_site_url\s*=\s*"([^"]+)"').Groups[1].Value
}
if (-not $ClientId -or -not $SiteUrl) {
    throw 'Falta o aplicativo do Entra (-ClientId ou terraform output entra_client_id) ou o site (-SiteUrl ou sharepoint_site_url do local.auto.tfvars).'
}

$site = [Uri] $siteUrl
Connect-MgGraph -TenantId (az account show --query tenantId -o tsv) -Scopes 'Sites.FullControl.All' -NoWelcome
$found = Get-MgSite -SiteId "$($site.Host):$($site.AbsolutePath.TrimEnd('/'))"

# Only "write": the application never manages permissions in SharePoint (ADR-0030).
$current = Get-MgSitePermission -SiteId $found.Id -All |
    Where-Object { $_.GrantedToIdentitiesV2.Application.Id -contains $clientId } |
    Select-Object -First 1
if ($current) {
    if ((@($current.Roles) -join ',') -ne 'write') {
        Update-MgSitePermission -SiteId $found.Id -PermissionId $current.Id -BodyParameter @{ roles = @('write') } | Out-Null
    }
    Write-Host "O aplicativo $ApplicationName já escreve no site $($found.DisplayName)."
}
else {
    New-MgSitePermission -SiteId $found.Id -BodyParameter @{
        roles               = @('write')
        grantedToIdentities = @(@{ application = @{ id = $clientId; displayName = $ApplicationName } })
    } | Out-Null
    Write-Host "O aplicativo $ApplicationName passou a escrever no site $($found.DisplayName)."
}
