# Tasks

## 1. Persistência compartilhada e regras de arquitetura

- [x] 1.1 Criar `src/FiscoProjects.Persistence` com os pacotes do EF Core e do SQL Server, referenciado pelo host, e incluir o ADR-0075 no índice de ADRs; verificar que `dotnet build -warnaserror` passa
- [x] 1.2 Atualizar os testes de arquitetura: `Persistence` e `Testing` não são módulos, `Domain` e `Application` não usam `FiscoProjects.Persistence`, e ela não usa módulos; testar os cenários "Domínio usa a persistência compartilhada", "Persistência compartilhada usa um módulo" e "Assemblies que não são módulos" com exemplos; verificar com `dotnet test`

## 2. Contexto base dos módulos

- [x] 2.1 Criar `ModuleDbContext<TSelf>`, `IModuleSchema`, `AddModuleDbContext<T>`, o registro de contextos, a fábrica de tempo de design e as convenções (UTC e `Id` gerado pela aplicação); verificar que `dotnet build -warnaserror` passa
- [x] 2.2 Criar `tests/FiscoProjects.Testing` com o `SqlServerFixture` (Testcontainers, imagem fixada) e os contextos de exemplo `alpha` e `beta` com migrações geradas pelo `dotnet ef` (manifesto de ferramentas em `dotnet-tools.json`); verificar que `dotnet ef migrations list` mostra as migrações dos dois contextos
- [x] 2.3 Criar `tests/FiscoProjects.Persistence.Tests` com os cenários "Módulo com schema próprio", "Dois módulos no mesmo banco", "Data e hora em UTC", "Data e hora local" e "Entidade gravada"; verificar com `dotnet test`

## 3. Migrações

- [x] 3.1 Criar o `ModuleMigrator` para aplicar as migrações de todos os contextos registrados; testar "Banco vazio" e "Migrações já aplicadas"; verificar com `dotnet test`
- [x] 3.2 Aplicar as migrações ao subir a API só no desenvolvimento; testar "API sobe no desenvolvimento" e "API sobe em produção"; verificar com `dotnet test`

## 4. Saúde do banco

- [x] 4.1 Criar o `DatabaseHealthCheck` e registrá-lo no `/health`; ajustar o teste "Aplicação no ar" para usar o banco do contêiner e testar "Banco fora do ar" e "Banco não configurado"; verificar com `dotnet test`

## 5. Desenvolvimento local e documentação

- [x] 5.1 Criar `compose.yaml` e `.env.example` com o SQL Server local e os user secrets da API; verificar que `docker compose up -d` sobe o banco e que a API no desenvolvimento responde 200 em `/health`
- [x] 5.2 Atualizar o guia de módulos (contexto, schema, migrações, transações) e a seção 6 do `CLAUDE.md` (Docker, banco local, `dotnet ef`); verificar que os comandos documentados rodam como escritos

## 6. Verificação final

- [x] 6.1 Rodar `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `openspec validate --all --strict` e o `cspell`, todos sem erro

## Workflow follow-up

- Abrir o PR e marcar a Fundação-2 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: ADR-0075 como aceito, `/opsx:archive`, roadmap como "Concluída" e merge.
