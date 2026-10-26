## Why

O ambiente `dev` no Azure mostrava a mensagem de ativação do Syncfusion na Linha do tempo: a chave estava só nos segredos da máquina de Marcelo. Ele criou o segredo `Syncfusion--LicenseKey` no Key Vault do `dev` e a configuração do App Service que aponta para ele, como manda o passo 5 da instalação (Fundação-6.1, a pedido de Marcelo).

## What Changes

- `infra/terraform/environments/dev.tfvars`: `syncfusion_license_in_key_vault = true`, para o Terraform escrever a mesma configuração que já está no ar e não apagá-la no próximo `apply` (ADR-0051, ADR-0087).

## Capabilities

### New Capabilities

### Modified Capabilities

Nenhuma: a regra já existe (ADR-0087 e passo 5 de `docs/instalacao.md`); muda só a configuração do ambiente de Marcelo.

## Impact

- `infra/terraform/environments/dev.tfvars` e o roadmap. O `apply` no `dev` não muda a configuração do Syncfusion, que já está igual.
