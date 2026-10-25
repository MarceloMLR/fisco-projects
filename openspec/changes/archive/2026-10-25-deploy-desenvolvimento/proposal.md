## Why

Com a infraestrutura criada (Fundação-6), falta pôr o FiscoProjects no ar no Azure. A Fundação-7 publica a aplicação no ambiente de desenvolvimento e demonstração a cada merge na `main`, como previa o ADR-0055, para Marcelo testar o sistema de ponta a ponta e demonstrar de qualquer lugar. Ela também traz a verificação de licenças dos pacotes do NuGet no PR.

## What Changes

- Workflow `deploy-dev` (`.github/workflows/deploy-dev.yml`), a cada merge na `main` e sob demanda, com a lógica em `scripts/cd`: monta o pacote (`publish.sh`), publica no App Service e espera o `/health` (`deploy.sh`), carrega os dados de demonstração que faltam e roda o `terraform plan` (`plan.sh`), que avisa no resumo da execução quando a infraestrutura está diferente do código.
- Identidade do pipeline (módulo `ci` do Terraform): um aplicativo do Entra ID com federação só do environment `dev` do GitHub, restrito à `main`, colaborador do grupo de recursos, gravando no contêiner do estado e lendo o diretório; sem segredos e sem poder de dar papéis ou mudar o Entra ID. O `apply` continua com quem opera a instalação (ADR-0115, no lugar da regra do ADR-0055).
- `infra/scripts/Set-GitHubEnvironment.ps1` prepara o environment do GitHub e as variáveis.
- O job `backend` do PR confere as licenças dos pacotes do NuGet (`scripts/ci/licenses.sh`); o Terraform fixado passa a vir de um script só (`scripts/ci/terraform.sh`).
- O e-mail dos alertas fica sensível no Terraform, porque os planos aparecem nos logs públicos.
- Depois da primeira publicação pela `main` (ADR-0116): a federação pelo subject imutável do GitHub, com os números do dono e do repositório; o e-mail dos alertas fora das variáveis do workflow, que o GitHub mostra nos logs, lido do grupo de ações pelo `plan.sh`; e as leituras que faltavam ao `plan` (`Application.Read.All` e o papel Leitor na conta do estado).

## Capabilities

### New Capabilities
- `publicacao`: a publicação do ambiente de desenvolvimento a cada merge, a identidade do pipeline e a conferência da infraestrutura.

### Modified Capabilities
- `integracao-continua`: as licenças dos pacotes do NuGet na verificação do pull request.

## Impact

- `.github/workflows/deploy-dev.yml`, `scripts/cd`, `scripts/ci/licenses.sh`, `scripts/ci/terraform.sh`, o workflow do PR e os testes de `tests/FiscoProjects.Pipeline.Tests`.
- `infra/terraform` (módulo `ci`, `github_repository` no `dev`), `infra/scripts/Set-GitHubEnvironment.ps1`.
- `docs/instalacao.md`, ADR-0115, ADR-0116, `CLAUDE.md`, glossário e roadmap.
- GitHub de Marcelo: o environment `dev`, só para a `main`, com as variáveis do pipeline; nenhum segredo.
