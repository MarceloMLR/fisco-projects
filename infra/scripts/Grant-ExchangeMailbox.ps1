<#
.SYNOPSIS
Creates the shared mailbox of the e-mails of the installation and lets the application send only from it (ADR-0036).

.DESCRIPTION
The application sends the e-mails through Microsoft Graph, from a shared mailbox, without Mail.Send in the Entra ID,
which would let it send as anyone. This script, in Exchange Online:

1. creates the shared mailbox, when it does not exist yet, and blocks the sign-in of its account, so it is no person
   of the company directory (ADR-0119);
2. registers the application of terraform output entra_client_id as a service principal of Exchange;
3. creates a management scope with only that mailbox and gives the application the role "Application Mail.Send"
   restricted to it (RBAC for Applications);
4. checks with Test-ServicePrincipalAuthorization that the application sends from the mailbox and not from the mailbox
   of the person who runs it.

The address comes from -Mailbox or from email_mailbox in local.auto.tfvars; without either, it is fiscoprojects@ the
default domain of the tenant. It signs in to Exchange Online as an Exchange Administrator or a Global Administrator,
needs the module ExchangeOnlineManagement and the Azure CLI signed in to the same tenant:

    Install-Module ExchangeOnlineManagement -Scope CurrentUser

Running it again keeps everything as it is. Changes of the permissions of an application take from 30 minutes to
2 hours to reach the sending; the check is immediate.

.EXAMPLE
./infra/scripts/Grant-ExchangeMailbox.ps1
#>
[CmdletBinding()]
param(
    [string] $Mailbox,
    [string] $DisplayName = 'FiscoProjects',
    [string] $ApplicationName = 'FiscoProjects',
    # WAM, the sign-in broker of Windows, fails on some computers; the sign-in then opens in the browser.
    [switch] $DisableWAM
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

if (-not (Get-Module -ListAvailable -Name 'ExchangeOnlineManagement')) {
    throw 'Falta o módulo ExchangeOnlineManagement: Install-Module ExchangeOnlineManagement -Scope CurrentUser'
}

$terraform = Join-Path $PSScriptRoot '..\terraform'
$clientId = terraform "-chdir=$terraform" output -raw entra_client_id
if (-not $clientId) {
    throw 'Falta o aplicativo do Entra (terraform output entra_client_id).'
}

if (-not $Mailbox) {
    $personal = Join-Path $terraform 'local.auto.tfvars'
    if (Test-Path $personal) {
        $Mailbox = [regex]::Match((Get-Content $personal -Raw), 'email_mailbox\s*=\s*"([^"]+)"').Groups[1].Value
    }
}

$servicePrincipalId = az ad sp show --id $clientId --query id -o tsv
Connect-ExchangeOnline -ShowBanner:$false -DisableWAM:$DisableWAM
try {
    if (-not $Mailbox) {
        $domain = (Get-AcceptedDomain | Where-Object Default).DomainName
        $Mailbox = "fiscoprojects@$domain"
    }

    # A new tenant needs this once before custom scopes and role assignments; it cannot be undone.
    if ((Get-OrganizationConfig).IsDehydrated) {
        Enable-OrganizationCustomization
        Write-Host 'Personalização da organização no Exchange liberada.'
    }

    # 1. The shared mailbox, with the sign-in of its account blocked.
    $found = try { Get-EXOMailbox -Identity $Mailbox -ErrorAction Stop } catch { $null }
    if (-not $found) {
        $alias = ($Mailbox -split '@')[0]
        $found = New-Mailbox -Shared -Name $DisplayName -DisplayName $DisplayName -Alias $alias -PrimarySmtpAddress $Mailbox
        Write-Host "Caixa compartilhada $Mailbox criada."
    }
    elseif ($found.RecipientTypeDetails -ne 'SharedMailbox') {
        throw "$Mailbox não é uma caixa compartilhada ($($found.RecipientTypeDetails))."
    }
    else {
        Write-Host "A caixa compartilhada $Mailbox já existia."
    }

    # The account of a new mailbox reaches the Entra ID after a while.
    $enabled = $null
    for ($attempt = 1; $attempt -le 12 -and $null -eq $enabled; $attempt++) {
        $account = (Get-EXOMailbox -Identity $Mailbox -Properties ExternalDirectoryObjectId).ExternalDirectoryObjectId
        $enabled = if ($account) {
            # The Azure CLI does not read accountEnabled by default; Microsoft Graph gives it when asked.
            try { az rest --method get --url "https://graph.microsoft.com/v1.0/users/${account}?`$select=accountEnabled" --query accountEnabled -o tsv 2>$null }
            catch { $null }
        }
        if (-not $enabled) { $enabled = $null; Start-Sleep -Seconds 10 }
    }
    if ($null -eq $enabled) {
        Write-Warning 'A conta da caixa ainda não está no Entra ID: bloqueie a entrada dela no centro de administração do Microsoft 365 (Usuários ativos, Bloquear entrada).'
    }
    elseif ($enabled -ne 'false') {
        try {
            az ad user update --id $account --account-enabled false
            Write-Host 'Entrada da conta da caixa bloqueada.'
        }
        catch {
            Write-Warning 'Não foi possível bloquear a entrada da conta da caixa pelo Azure CLI: bloqueie no centro de administração do Microsoft 365 (Usuários ativos, Bloquear entrada).'
        }
    }

    # 2. The application, as a service principal of Exchange.
    $principal = try { Get-ServicePrincipal -Identity $clientId -ErrorAction Stop } catch { $null }
    if (-not $principal) {
        $principal = New-ServicePrincipal -AppId $clientId -ObjectId $servicePrincipalId -DisplayName $ApplicationName
        Write-Host "Aplicativo $ApplicationName registrado no Exchange."
    }

    # 3. Only the mailbox, and only sending.
    $scopeName = "$ApplicationName - caixa de envio"
    $filter = "PrimarySmtpAddress -eq '$Mailbox'"
    $scope = try { Get-ManagementScope -Identity $scopeName -ErrorAction Stop } catch { $null }
    if (-not $scope) {
        # Right after the customization is enabled, Exchange may still refuse it for up to an hour.
        for ($attempt = 1; ; $attempt++) {
            try {
                New-ManagementScope -Name $scopeName -RecipientRestrictionFilter $filter -ErrorAction Stop | Out-Null
                break
            }
            catch {
                if ($attempt -ge 60 -or "$_" -notmatch 'Enable-OrganizationCustomization') { throw }
                if ($attempt -eq 1) { Write-Host 'O Exchange ainda está liberando a personalização; tentando de novo a cada minuto, por até 1 hora.' }
                Start-Sleep -Seconds 60
            }
        }
    }
    elseif ($scope.RecipientFilter -ne $filter) {
        Set-ManagementScope -Identity $scopeName -RecipientRestrictionFilter $filter
    }

    $names = @($principal.ObjectId, $principal.AppId, $principal.Identity, $principal.Name, $servicePrincipalId, $clientId) |
        Where-Object { $_ } | ForEach-Object { "$_" }
    $assignments = @(Get-ManagementRoleAssignment -Role 'Application Mail.Send' |
        Where-Object { $_.Name -eq "$ApplicationName - Mail.Send" -or ($_.RoleAssigneeType -eq 'ServicePrincipal' -and "$($_.RoleAssigneeName)" -in $names) })
    if ($assignments.Count -eq 0) {
        New-ManagementRoleAssignment -Name "$ApplicationName - Mail.Send" -Role 'Application Mail.Send' `
            -App $servicePrincipalId -CustomResourceScope $scopeName | Out-Null
        Write-Host "O aplicativo $ApplicationName passou a enviar pela caixa $Mailbox."
    }
    else {
        # Any other assignment of the role to the application is restricted to the mailbox too.
        foreach ($assignment in $assignments | Where-Object { "$($_.CustomResourceScope)" -ne $scopeName }) {
            Set-ManagementRoleAssignment -Identity $assignment.Identity -CustomResourceScope $scopeName
        }
        Write-Host "O aplicativo $ApplicationName já envia só pela caixa $Mailbox."
    }

    # 4. Sends from the mailbox, and from no other.
    $allowed = Test-ServicePrincipalAuthorization -Identity $clientId -Resource $Mailbox |
        Where-Object RoleName -eq 'Application Mail.Send'
    if (-not ($allowed | Where-Object InScope)) {
        throw "O aplicativo não envia pela caixa $Mailbox."
    }

    $me = (Get-ConnectionInformation | Select-Object -First 1).UserPrincipalName
    if ($me -and $(try { Get-EXOMailbox -Identity $me -ErrorAction Stop } catch { $null })) {
        $denied = Test-ServicePrincipalAuthorization -Identity $clientId -Resource $me |
            Where-Object RoleName -eq 'Application Mail.Send'
        if ($denied | Where-Object InScope) {
            throw "O aplicativo também envia pela caixa de $me; confira as atribuições do papel Application Mail.Send."
        }
    }

    Write-Host "Conferido: o aplicativo envia só pela caixa $Mailbox."
    Write-Host "No local.auto.tfvars: email_mailbox = `"$Mailbox`""
}
finally {
    Disconnect-ExchangeOnline -Confirm:$false
}
