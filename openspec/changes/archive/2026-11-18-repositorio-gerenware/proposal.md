## Why

O repositório ainda se chama `fisco-projects`. O ADR-0163 decidiu renomeá-lo para `gerenware`, e o nome entra no subject dos tokens que o deploy usa para entrar no Azure, então o código precisa estar pronto antes da renomeação.

## What Changes

- `github_repository` dos ambientes `dev` e `prod` com o nome novo do repositório, e o `Set-GitHubEnvironment.ps1` apontando para `MarceloMLR/gerenware`, com os testes do pipeline.
- ADR-0163 e a nota no discovery.
- A renomeação no GitHub, o `apply` da credencial federada e a troca do endereço nos clones ficam com Marcelo, depois do merge.

## Capabilities

### New Capabilities

### Modified Capabilities

Nenhuma: as specs não citam o nome do repositório.

## Non-goals

- Renomear o repositório pela sessão: é uma configuração da conta de Marcelo.
- Mudar os workflows: eles não citam o nome do repositório.

## Impact

- `infra/terraform/environments/*.tfvars`, `infra/scripts/Set-GitHubEnvironment.ps1` e `tests/GerenWare.Pipeline.Tests`.
- Entre a renomeação e o `apply`, o `deploy-dev` não entra no Azure.
