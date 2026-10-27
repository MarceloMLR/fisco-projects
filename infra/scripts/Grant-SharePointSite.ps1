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

Running it again keeps the right as it is.

.EXAMPLE
./infra/scripts/Grant-SharePointSite.ps1
#>
[CmdletBinding()]
param(
    [string] $ApplicationName = 'FiscoProjects'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

foreach ($module in 'Microsoft.Graph.Authentication', 'Microsoft.Graph.Sites') {
    if (-not (Get-Module -ListAvailable -Name $module)) {
        throw "Falta o módulo ${module}: Install-Module Microsoft.Graph.Authentication, Microsoft.Graph.Sites -Scope CurrentUser"
    }
}

$terraform = Join-Path $PSScriptRoot '..\terraform'
$clientId = terraform "-chdir=$terraform" output -raw entra_client_id
$personal = Get-Content (Join-Path $terraform 'local.auto.tfvars') -Raw
$siteUrl = [regex]::Match($personal, 'sharepoint_site_url\s*=\s*"([^"]+)"').Groups[1].Value
if (-not $clientId -or -not $siteUrl) {
    throw 'Falta o aplicativo do Entra (terraform output entra_client_id) ou o sharepoint_site_url do local.auto.tfvars.'
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
