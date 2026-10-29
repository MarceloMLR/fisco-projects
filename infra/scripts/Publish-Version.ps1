<#
.SYNOPSIS
Publishes the version of this package in an installation updated outside the pipeline of the repository (ADR-0127).

.DESCRIPTION
Runs from the folder of the package of a version (fiscoprojects-vX.Y.Z.zip, unzipped), after the terraform apply of the
version, with the Azure CLI signed in to the subscription of the installation as one of its operators
(operator_object_ids), who administer the database. The names come from installation and environment in
infra/terraform/local.auto.tfvars, as in infra/terraform/main.tf. It:

1. reads the managed identity of the application from the App Service, and stops without it;
2. outside Development, applies the migrations of every module with the migrate command of the package, connected by
   Entra ID with the account of the Azure CLI, and gives the identity of the application the reading and the writing of
   the data (ADR-0125); the firewall of the database is opened to the address of this computer only while it runs;
3. publishes app.zip in the App Service and waits for /health.

It needs the Azure CLI and the ASP.NET Core Runtime 10 (dotnet). Running it again with the same version changes nothing.

.EXAMPLE
./infra/scripts/Publish-Version.ps1

.EXAMPLE
./infra/scripts/Publish-Version.ps1 -ClientAddress 203.0.113.10
#>
[CmdletBinding()]
param(
    # Public address of this computer, for the firewall of the database; asked to api.ipify.org when not given.
    [string] $ClientAddress
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..' '..'))
$package = Join-Path $root 'app.zip'
$settings = Join-Path $root 'infra' 'terraform' 'local.auto.tfvars'
foreach ($file in $package, $settings) {
    if (-not (Test-Path $file)) {
        throw "Falta $file. Rode na pasta do pacote da versão, com os arquivos da instalação (docs/instalacao.md)."
    }
}

$parameters = Get-Content $settings -Raw
function Get-Parameter([string] $Name) {
    [regex]::Match($parameters, "(?m)^\s*$Name\s*=\s*`"([^`"]*)`"").Groups[1].Value
}
$installation = Get-Parameter 'installation'
$environment = Get-Parameter 'environment'
$aspnetcore = Get-Parameter 'aspnetcore_environment'
if (-not $aspnetcore) {
    $aspnetcore = 'Production'
}
if (-not $installation -or -not $environment) {
    throw 'Faltam installation e environment no local.auto.tfvars da instalação.'
}

# The names of infra/terraform/main.tf.
$name = "fiscoprojects-$installation-$environment"
$group = "rg-$name"
$app = "app-$name"
$server = "sql-$name"
$address = "https://$app.azurewebsites.net"
$versionFile = Join-Path $root 'VERSION'
$version = if (Test-Path $versionFile) { (Get-Content $versionFile -Raw).Trim() } else { 'sem número' }
Write-Host "Versão $version na instalação $name, assinatura $(az account show --query name -o tsv)."

$principal = az webapp identity show --resource-group $group --name $app --query principalId --output tsv
if (-not $principal) {
    throw "O App Service $app não tem identidade gerenciada: aplique antes o Terraform da versão (docs/instalacao.md)."
}

# In Development, the application applies its own migrations, as in the dev environment of the repository.
if ($aspnetcore -ne 'Development') {
    if (-not $ClientAddress) {
        $ClientAddress = (Invoke-RestMethod -Uri 'https://api.ipify.org').ToString().Trim()
    }
    $rule = 'publicacao-temporaria'
    $folder = Join-Path ([IO.Path]::GetTempPath()) "fiscoprojects-$([IO.Path]::GetRandomFileName())"
    $variables = @{
        ASPNETCORE_ENVIRONMENT            = $aspnetcore
        ConnectionStrings__FiscoProjects  = "Server=tcp:$server.database.windows.net,1433;Database=sqldb-fiscoprojects;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False"
        Database__AppIdentity__Name       = $app
        Database__AppIdentity__ObjectId   = $principal
        # Only the account of the Azure CLI, never another one signed in to Visual Studio on this computer.
        AZURE_TOKEN_CREDENTIALS           = 'AzureCliCredential'
    }
    try {
        Expand-Archive -Path $package -DestinationPath $folder -Force
        az sql server firewall-rule create --resource-group $group --server $server --name $rule `
            --start-ip-address $ClientAddress --end-ip-address $ClientAddress --output none
        foreach ($variable in $variables.GetEnumerator()) {
            Set-Item "Env:$($variable.Key)" $variable.Value
        }

        # A new rule of the firewall may take a moment to hold; the migrations change nothing when run again.
        for ($attempt = 1; $attempt -le 3; $attempt++) {
            $PSNativeCommandUseErrorActionPreference = $false
            dotnet (Join-Path $folder 'FiscoProjects.Api.dll') migrate
            $migrated = $LASTEXITCODE -eq 0
            $PSNativeCommandUseErrorActionPreference = $true
            if ($migrated) {
                break
            }
            if ($attempt -eq 3) {
                throw "As migrações falharam em $server; nada foi publicado."
            }
            Write-Host 'As migrações falharam; tentando de novo em 30 segundos.'
            Start-Sleep -Seconds 30
        }
        Write-Host "Migrações aplicadas em $server."
    }
    finally {
        foreach ($variable in $variables.Keys) {
            Remove-Item "Env:$variable" -ErrorAction SilentlyContinue
        }
        $PSNativeCommandUseErrorActionPreference = $false
        az sql server firewall-rule delete --resource-group $group --server $server --name $rule --output none 2>$null
        $PSNativeCommandUseErrorActionPreference = $true
        Remove-Item $folder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# The startup is followed here, by /health: the tracking of the Azure CLI can keep waiting after the site is up.
az webapp deploy --resource-group $group --name $app --src-path $package --type zip --track-status false --output none
Start-Sleep -Seconds 20
for ($attempt = 1; $attempt -le 60; $attempt++) {
    try {
        $status = (Invoke-WebRequest -Uri "$address/health" -SkipHttpErrorCheck -TimeoutSec 30).StatusCode
    }
    catch {
        # The App Service may refuse the connection while it restarts with the new package.
        $status = 0
    }
    if ($status -eq 200) {
        Write-Host "Publicada: $address"
        return
    }
    Start-Sleep -Seconds 10
}
throw "$address/health não respondeu 200 em 10 minutos."
