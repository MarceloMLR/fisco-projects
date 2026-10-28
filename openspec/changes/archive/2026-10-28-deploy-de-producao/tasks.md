## 1. Banco

- [x] 1.1 `DatabaseAccess` na `FiscoProjects.Persistence` e o `MigrateCommand` da API, com os testes "Migrações pelo comando da publicação" e "Acesso da aplicação ao banco"

## 2. Publicação

- [x] 2.1 `version.sh`, `migrate.sh` e `PLAN_MUST_MATCH` no `plan.sh`, com os testes "Produção só por tag de versão", "Produção com a infraestrutura diferente", "Migrações antes da publicação" e "Produção sem dados de demonstração"
- [x] 2.2 `.github/workflows/deploy-prod.yml`, com os testes "Publicação em produção por versão" e "Pipeline sem segredos" nos dois workflows
- [x] 2.3 Terraform (`ci` no grupo do banco, `prod.tfvars`, `prod.backend.hcl`) e `Set-GitHubEnvironment.ps1` com a aprovação e as tags, com os testes "Identidade do pipeline com poder limitado" e "Produção com aprovação"

## 3. Documentação e conferência

- [x] 3.1 ADR-0125, `docs/instalacao.md`, `CLAUDE.md` e roadmap (Operação-5)
- [x] 3.2 Build sem avisos, testes, `dotnet format`, `infra.sh`, lint e `openspec validate --all --strict`

## Workflow follow-up

- PR, aprovação de Marcelo, archive na mesma branch e merge por Marcelo.
