## Why

A Fundação-6 esperava uma assinatura do Azure, que Marcelo agora tem (avaliação gratuita, com limite de gastos). Ela é o primeiro passo para testar o FiscoProjects de ponta a ponta na nuvem: o ambiente de desenvolvimento e demonstração no Azure, que a Fundação-7 publica a cada merge e que recebe depois as integrações com o Microsoft 365 (M365-1 a M365-4).

## What Changes

- Terraform em `infra/terraform`, com os módulos do ADR-0053: App Service Linux com identidade gerenciada, Azure SQL só com Entra ID, Key Vault, Log Analytics e Application Insights, teste de disponibilidade e os quatro alertas do ADR-0112, e o aplicativo no Entra ID.
- Parâmetros por ambiente em `environments/` (o de desenvolvimento e demonstração de Marcelo, com o banco Basic), valores pessoais fora do repositório e o estado numa conta de armazenamento criada pelo `infra/scripts/Prepare-TerraformState.ps1`.
- ADR-0114: o banco Basic no lugar da oferta gratuita no ambiente de desenvolvimento, porque os processos em segundo plano a esgotariam em dias; o acesso ao banco só pelo Entra ID; o ambiente em `Development` até as integrações; e a conexão do Application Insights direto nas configurações.
- Verificação do PR com o job `infra` (`scripts/ci/infra.sh`): `terraform fmt`, `init` sem o estado e `validate`, com o Terraform fixado.
- O ambiente de desenvolvimento criado na assinatura de Marcelo; a aplicação ainda não é publicada (Fundação-7).

## Capabilities

### New Capabilities
- `infraestrutura`: a instalação no Azure pelo Terraform, sem senhas, com backups, monitoramento, estado protegido e um ambiente de desenvolvimento que se apaga sem deixar custos.

### Modified Capabilities
- `integracao-continua`: o job `infra` na verificação do pull request.

## Impact

- `infra/terraform`, `infra/scripts`, `scripts/ci/infra.sh`, `scripts/ci/verdict.sh`, o workflow do PR e os testes de `tests/FiscoProjects.Pipeline.Tests`.
- `docs/instalacao.md`, `docs/custos-estimados.md`, ADR-0114, `CLAUDE.md` e o roadmap.
- Conta de Marcelo: o ambiente usa o crédito da avaliação (App Service B1, banco Basic, teste de disponibilidade e alertas, cerca de US$ 25 por mês) e é apagado com `terraform destroy` quando não for mais usado.
