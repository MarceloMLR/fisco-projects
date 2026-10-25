<#
.SYNOPSIS
Creates the storage account of the Terraform state of an installation (ADR-0053), before its first apply.

.DESCRIPTION
One storage account per installation, in its own resource group, with blob versioning, soft delete of blobs and
containers, HTTPS only, no public access and no shared keys: Terraform reaches it with Entra ID (use_azuread_auth).
The person signed in to the Azure CLI gets the role to read and write the state. Running it again changes nothing.

.EXAMPLE
./infra/scripts/Prepare-TerraformState.ps1 -Installation mlr
#>
[CmdletBinding()]
param(
    # Short code of the installation, the same of the .tfvars (2 to 6 lowercase letters or digits).
    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z0-9]{2,6}$')]
    [string] $Installation,

    [string] $Location = 'brazilsouth'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$group = "rg-fiscoprojects-$Installation-tfstate"
$account = "stfp${Installation}tfstate"
$container = 'tfstate'
$tags = @('aplicacao=FiscoProjects', "instalacao=$Installation", 'uso=estado-do-terraform')

Write-Host "Assinatura: $(az account show --query name -o tsv)"
az provider register --namespace Microsoft.Storage --wait | Out-Null

az group create --name $group --location $Location --tags @tags --output none
az storage account create --name $account --resource-group $group --location $Location `
    --sku Standard_LRS --kind StorageV2 --https-only true --min-tls-version TLS1_2 `
    --allow-blob-public-access false --allow-shared-key-access false --tags @tags --output none
az storage account blob-service-properties update --account-name $account --resource-group $group `
    --enable-versioning true `
    --enable-delete-retention true --delete-retention-days 30 `
    --enable-container-delete-retention true --container-delete-retention-days 30 --output none

$me = az ad signed-in-user show --query id -o tsv
$scope = az storage account show --name $account --resource-group $group --query id -o tsv
# No parentheses in the query: on Windows, az is a .cmd file, and cmd.exe breaks on them.
$assigned = @(az role assignment list --assignee $me --scope $scope --role 'Storage Blob Data Contributor' --query '[].id' -o tsv)
if ($assigned.Count -eq 0) {
    az role assignment create --assignee-object-id $me --assignee-principal-type User `
        --role 'Storage Blob Data Contributor' --scope $scope --output none
}

# The role takes a few minutes to reach the storage account.
for ($attempt = 1; $attempt -le 20; $attempt++) {
    $PSNativeCommandUseErrorActionPreference = $false
    az storage container create --name $container --account-name $account --auth-mode login --output none 2>$null
    $created = $LASTEXITCODE -eq 0
    $PSNativeCommandUseErrorActionPreference = $true
    if ($created) {
        break
    }
    if ($attempt -eq 20) {
        throw "O contêiner $container não foi criado: confira o papel 'Storage Blob Data Contributor' na conta $account."
    }
    Start-Sleep -Seconds 15
}

Write-Host "Estado do Terraform pronto: conta $account, contêiner $container, no grupo $group."
