<#
.SYNOPSIS
Prepares the GitHub environment whose pipeline publishes an environment of the installation (ADR-0115).

.DESCRIPTION
Creates the environment on GitHub, accepting deployments only from main, and sets its variables: the identity of the
pipeline (from terraform output), the tenant, the subscription and who operates the installation, from
local.auto.tfvars. None of them is a secret: the pipeline reaches Azure by federation. GitHub shows them in the public
logs, so the e-mail of the alerts is not one of them: the pipeline reads it from the action group (ADR-0116). Needs the GitHub CLI signed in with the repo scope, the
Azure CLI on the subscription and the environment already applied. Running it again updates the values.

.EXAMPLE
./infra/scripts/Set-GitHubEnvironment.ps1 -Environment dev
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('dev', 'prod')]
    [string] $Environment,

    [string] $Repository = 'MarceloMLR/fisco-projects'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$terraform = Join-Path $PSScriptRoot '..\terraform'
$clientId = terraform "-chdir=$terraform" output -raw ci_client_id
if (-not $clientId) {
    throw "Sem identidade do pipeline: confira github_repository no .tfvars de $Environment e aplique o Terraform."
}
$personal = Get-Content (Join-Path $terraform 'local.auto.tfvars') -Raw
$variables = [ordered]@{
    AZURE_CLIENT_ID       = $clientId
    AZURE_TENANT_ID       = az account show --query tenantId -o tsv
    AZURE_SUBSCRIPTION_ID = az account show --query id -o tsv
    OPERATOR_OBJECT_IDS   = [regex]::Match($personal, 'operator_object_ids\s*=\s*(\[[^\]]*\])').Groups[1].Value
}
foreach ($name in $variables.Keys) {
    if (-not $variables[$name]) {
        throw "Sem valor para ${name}: confira local.auto.tfvars e a conta do Azure CLI."
    }
}

# Deployments only from main.
'{"deployment_branch_policy":{"protected_branches":false,"custom_branch_policies":true}}' |
    gh api --method PUT "repos/$Repository/environments/$Environment" --input - | Out-Null
$branches = @(gh api "repos/$Repository/environments/$Environment/deployment-branch-policies" --jq '.branch_policies[].name')
if ($branches -notcontains 'main') {
    gh api --method POST "repos/$Repository/environments/$Environment/deployment-branch-policies" -f name=main -f type=branch | Out-Null
}

# Through the standard input, so that the quotes of the list reach GitHub as they are.
foreach ($name in $variables.Keys) {
    $variables[$name] | gh variable set $name --env $Environment --repo $Repository | Out-Null
}

Write-Host "Environment $Environment pronto no GitHub, só para a main, com as variáveis $($variables.Keys -join ', ')."
