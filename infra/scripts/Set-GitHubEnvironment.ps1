<#
.SYNOPSIS
Prepares the GitHub environment whose pipeline publishes an environment of the installation (ADR-0115).

.DESCRIPTION
Creates the environment on GitHub and sets its variables: the identity of the pipeline (from terraform output), the
tenant, the subscription and who operates the installation, from local.auto.tfvars. The environment dev accepts
deployments only from main; prod accepts only the tags of the versions (v*) and waits for the approval of whoever runs
the script, who may approve the deployments they start (ADR-0125). None of them is a secret: the pipeline reaches Azure by federation. GitHub shows them in the public
logs, so the e-mail of the alerts is not one of them: the pipeline reads it from the action group (ADR-0116). Needs the GitHub CLI signed in with the repo scope, the
Azure CLI on the subscription and the environment already applied. Running it again updates the values.

.EXAMPLE
./infra/scripts/Set-GitHubEnvironment.ps1 -Environment dev

.EXAMPLE
./infra/scripts/Set-GitHubEnvironment.ps1 -Environment prod
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

# dev: deployments only from main. prod: only from the tags of the versions, after the approval of whoever runs this.
$settings = @{ deployment_branch_policy = @{ protected_branches = $false; custom_branch_policies = $true } }
$policy = @{ name = 'main'; type = 'branch' }
if ($Environment -eq 'prod') {
    $settings.reviewers = @(@{ type = 'User'; id = [long](gh api user --jq .id) })
    $settings.prevent_self_review = $false
    $policy = @{ name = 'v*'; type = 'tag' }
}
$settings | ConvertTo-Json -Depth 4 -Compress |
    gh api --method PUT "repos/$Repository/environments/$Environment" --input - | Out-Null
$policies = @(gh api "repos/$Repository/environments/$Environment/deployment-branch-policies" --jq '.branch_policies[] | "\(.type) \(.name)"')
if ($policies -notcontains "$($policy.type) $($policy.name)") {
    gh api --method POST "repos/$Repository/environments/$Environment/deployment-branch-policies" -f "name=$($policy.name)" -f "type=$($policy.type)" | Out-Null
}

# Through the standard input, so that the quotes of the list reach GitHub as they are.
foreach ($name in $variables.Keys) {
    $variables[$name] | gh variable set $name --env $Environment --repo $Repository | Out-Null
}

$only = if ($Environment -eq 'prod') { 'só para as tags v*, com a sua aprovação' } else { 'só para a main' }
Write-Host "Environment $Environment pronto no GitHub, $only, com as variáveis $($variables.Keys -join ', ')."
