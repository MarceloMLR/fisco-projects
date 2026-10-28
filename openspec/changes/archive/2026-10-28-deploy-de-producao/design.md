## Context

O `deploy-dev` (ADR-0115) roda `publish.sh`, `deploy.sh` e `plan.sh` no environment `dev`, que aceita só a `main`. Na produção, a identidade da aplicação não é administradora do banco (`app_is_database_admin = false`, ADR-0114), e a API só aplica migrações no desenvolvimento (`DevelopmentDatabaseMigration`). O banco aceita só o Entra ID e as conexões dos serviços do Azure. A identidade do pipeline (módulo `ci`) é criada por ambiente, federada ao environment do GitHub de mesmo nome.

## Goals / Non-Goals

**Goals:**
- Publicar uma versão marcada na produção, com aprovação, aplicando as migrações sem dar à aplicação o poder de mudar o banco.

**Non-Goals:**
- `apply` pelo pipeline, criar a produção no Azure e desfazer uma versão.

## Decisions

- **Gatilho.** `workflow_dispatch` sem entradas; a versão é a referência em que o workflow roda (`GITHUB_REF_TYPE` = `tag`, `GITHUB_REF_NAME` = `vX.Y.Z`), conferida pelo `version.sh`. O environment `prod` aceita só as tags `v*` e pede aprovação (`Set-GitHubEnvironment.ps1`: `reviewers` com quem roda o script e `prevent_self_review` desligado, e a política de tags).
- **Conferência que para.** `plan.sh` ganha `PLAN_MUST_MATCH`: com `true`, a diferença vira erro (`::error::`), com o que mudaria no resumo; o `dev` segue com o aviso.
- **Comando `migrate`.** `Program.cs` monta a aplicação e, com `migrate` nos argumentos, roda o `MigrateCommand` e termina, sem `UseFiscoProjectsApi` nem `Run`: os processos em segundo plano e as validações da subida não rodam. O comando aplica o `ModuleMigrator.MigrateAllAsync` e, com `Database:AppIdentity:Name` e `Database:AppIdentity:ObjectId`, chama o `DatabaseAccess` da `FiscoProjects.Persistence`: `EnsureExternalUserAsync` (`CREATE USER ... FROM EXTERNAL PROVIDER WITH OBJECT_ID`, só no Azure SQL) e `GrantDataAsync` (`db_datareader` e `db_datawriter`, que valem para tabelas e sequências).
- **`migrate.sh`.** Lê os nomes do `.tfvars` como o `deploy.sh`, o `principalId` da identidade do App Service pelo Azure CLI e roda `dotnet artifacts/app/FiscoProjects.Api.dll migrate` com `ConnectionStrings__FiscoProjects` (`Authentication=Active Directory Default`), o ambiente do `.tfvars` e a identidade da aplicação.
- **Terraform.** O módulo `ci` ganha `database_admin` e `database_admins_group`; o `main.tf` passa `!var.app_is_database_admin`. `environments/prod.tfvars` e `prod.backend.hcl` da instalação `mlr`, só com os nomes e o repositório: o resto são os padrões da produção do `variables.tf`.
- **Testes.** `DeploymentTests` lê os dois workflows e roda os scripts com substitutos do `az`, do `dotnet` e do `terraform`; `MigrateCommandTests` roda o comando de verdade num banco do contêiner, e o acesso da aplicação é conferido com um usuário do SQL Server, já que o usuário externo só existe no Azure SQL.

## Risks / Trade-offs

- [Versão anterior no banco novo por alguns minutos] → migrações só acrescentam; a que remover algo vai em duas versões.
- [O executor do GitHub fora dos serviços do Azure] → o banco aceita os serviços do Azure; se recusar, quem opera abre o endereço pelo tempo da publicação.
- [Usuário externo sem teste automático] → a criação fica num método só, e a parte portátil (os papéis) é testada no contêiner.
