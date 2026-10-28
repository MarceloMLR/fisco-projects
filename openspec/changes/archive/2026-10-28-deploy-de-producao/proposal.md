## Why

O `deploy-dev` publica o ambiente de desenvolvimento a cada merge, mas ainda não há como levar uma versão para a produção de uma instalação. O ADR-0115 deixou para esta change as migrações da produção, que a aplicação não aplica sozinha fora do desenvolvimento (ADR-0114). É a Operação-5 do roadmap.

## What Changes

- Workflow `deploy-prod`, acionado à mão na tag da versão (`v1.2.3`), no environment `prod` do GitHub, que aceita só as tags `v*` e pede a aprovação de quem opera (ADR-0125).
- Ordem na produção: confere a versão (`version.sh`), confere a infraestrutura (`plan.sh`, que agora para a publicação quando há diferença), monta o pacote, aplica as migrações (`migrate.sh`) e publica, esperando o `/health`. Sem dados de demonstração.
- Comando `migrate` da API: aplica as migrações de todos os módulos sem subir o servidor e dá à identidade gerenciada da aplicação só a leitura e a gravação dos dados (`DatabaseAccess`).
- Terraform: a identidade do pipeline entra no grupo dos administradores do banco quando a aplicação não é administradora; `environments/prod.tfvars` e `prod.backend.hcl` da instalação `mlr`, com os padrões da produção.
- `Set-GitHubEnvironment.ps1 -Environment prod` cria o environment com a aprovação e só as tags.

## Capabilities

### New Capabilities

### Modified Capabilities
- `publicacao`: a publicação em produção e o poder da identidade do pipeline da produção no banco.
- `persistencia`: as migrações pela publicação e o acesso da aplicação ao banco na produção.

## Non-goals

- Criar a produção da instalação no Azure: fica com quem opera, pelo `docs/instalacao.md`.
- `terraform apply` pelo pipeline (ADR-0115).
- Desfazer uma versão publicada: publica-se a versão anterior pelo mesmo workflow.
- O guia de instalação completo, que é a Operação-6.

## Impact

- `.github/workflows/deploy-prod.yml`, `scripts/cd` (`version.sh`, `migrate.sh`, `plan.sh`), `infra/terraform` (módulo `ci`, `main.tf`, `environments/prod.*`), `infra/scripts/Set-GitHubEnvironment.ps1`.
- `FiscoProjects.Api` (`Program.cs`, `MigrateCommand`) e `FiscoProjects.Persistence` (`DatabaseAccess`).
- Testes do pipeline e da API.
- `docs/adr/0125-publicacao-em-producao.md`, ADR-0055 (status), `docs/instalacao.md`, `CLAUDE.md`, `docs/roadmap.md`.
- ADRs: 0055, 0114, 0115, 0116, 0125.
