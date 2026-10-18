# Design: banco de dados (Fundação-2)

## Context

Motivação em proposal.md. O backend tem host, `SharedKernel` com `Entity` (GUID v7) e testes de arquitetura que descobrem módulos pelo nome do assembly (`FiscoProjects.<Modulo>`), excluindo `Api` e `SharedKernel`. Não há módulo de negócio. Docker está disponível no ambiente de nuvem e na máquina de Marcelo (Docker Desktop).

## Goals / Non-Goals

**Goals:**

- O primeiro módulo (Acesso-1) só declara o próprio contexto e as entidades; schema, histórico de migrações, convenções e verificação do banco já existem.
- Testes de persistência rodam contra SQL Server de verdade, isolados por banco.

**Non-Goals:**

- Repositórios genéricos ou unidade de trabalho própria: cada caso de uso usa o contexto do módulo pela `Infrastructure`.
- Auditoria e eventos de domínio no salvar (Acesso-3).

## Decisions

### 1. Um contexto por módulo, num banco só

Cada módulo tem um contexto que herda de `ModuleDbContext<TSelf>` e declara o schema por um membro estático (`IModuleSchema.Schema`, nome do módulo em minúsculas, por exemplo `tickets`). O contexto base aplica `HasDefaultSchema`, e o registro configura o histórico de migrações `__EFMigrationsHistory` dentro do mesmo schema e o assembly de migrações do módulo.

- Todos os módulos usam o mesmo banco e a mesma connection string (`ConnectionStrings:FiscoProjects`), como uma instalação tem um Azure SQL (ADR-0048).
- Alternativa: um banco por módulo. Multiplica custo e configuração sem ganho para uma instalação pequena.
- Alternativa: um contexto único para o sistema. Quebra a posse das tabelas por módulo (ADR-0047).

### 2. Projeto compartilhado `FiscoProjects.Persistence`

Contexto base, registro (`AddModuleDbContext<T>`), fábrica de tempo de design para o `dotnet ef`, aplicação das migrações e verificação do banco ficam num projeto compartilhado, usado só pela `Infrastructure` dos módulos e pelo host (ADR-0075, proposto nesta change). Os testes de arquitetura passam a não tratá-lo como módulo e a garantir que ele não usa módulos e que `Domain` e `Application` não o usam.

### 3. Convenções no contexto base

- Propriedades `DateTime` e `DateTime?` passam por um conversor: na gravação, data local vira UTC e data sem indicação é tratada como UTC; na leitura, a data vem marcada como UTC. `DateOnly` fica como `date` no banco, sem conversão.
- O `Id` de toda entidade que herda de `Entity` é marcado como nunca gerado pelo banco; o valor é o GUID v7 criado pela aplicação.
- O provedor usa tentativas de novo em falhas passageiras (`EnableRetryOnFailure`), recomendadas para o Azure SQL.
- Fica um ponto de extensão no salvar para a auditoria da Acesso-3.

### 4. Aplicação das migrações

- `ModuleMigrator` aplica, em sequência, as migrações pendentes de todos os contextos registrados. O EF Core cria o banco se não existir e usa uma trava para evitar duas aplicações ao mesmo tempo.
- No desenvolvimento, um serviço de inicialização do host chama o `ModuleMigrator` antes de a API atender (registrado só quando o ambiente é `Development`, podendo ser desligado com `Database:MigrateOnStartup=false`).
- Fora do desenvolvimento, o host nunca altera o banco ao subir; a publicação aplica um pacote de migração (migration bundle) do EF Core (Fundação-7, ADR-0055).
- Alternativa: migrar ao subir também em produção. Mais simples, mas mistura publicação e execução e esconde falhas de migração atrás de uma API que não sobe.

### 5. Verificação do banco no `/health`

`DatabaseHealthCheck` abre uma conexão e executa uma consulta simples, com tempo limite curto. Sem connection string, responde não saudável. A resposta do `/health` continua só com o status (`Healthy` ou `Unhealthy`), sem detalhes. Status 503 quando não saudável (padrão do ASP.NET Core).

### 6. SQL Server local e nos testes

- Imagem `mcr.microsoft.com/mssql/server:2022-CU27-GDR1-ubuntu-22.04`, fixada, no `compose.yaml` e no Testcontainers, para o desenvolvimento e os testes usarem a mesma versão. Edição Developer, gratuita para desenvolvimento e testes.
- A senha local vem do `.env` (fora do Git; `.env.example` mostra o formato). A connection string de desenvolvimento fica nos user secrets da API.
- Nos testes, `SqlServerFixture` (Testcontainers.MsSql) sobe um contêiner por projeto de teste, como fixture de assembly do xUnit v3, e cada teste usa um banco com nome próprio.
- Contextos de exemplo (`alpha` e `beta`), com migrações geradas pelo `dotnet ef`, ficam no projeto de apoio `tests/FiscoProjects.Testing`, usado pelos testes de persistência e da API. Esse projeto também não é tratado como módulo.

### 7. Ferramentas

`dotnet-ef` 10.0.13 no manifesto de ferramentas do repositório (`dotnet-tools.json`), instalado com `dotnet tool restore`. `Microsoft.EntityFrameworkCore.Design` entra só com `PrivateAssets` nos projetos que geram migrações.

## Risks / Trade-offs

- [O SQL Server ordena `uniqueidentifier` pelos últimos bytes, então o GUID v7 não chega em ordem ao índice clusterizado e causa fragmentação] → Irrelevante no volume esperado (5 a 6 pessoas por instalação). Se uma tabela crescer muito, a change dela pode trocar a chave clusterizada.
- [Testes passam a exigir Docker] → Docker está no ambiente de nuvem, na máquina de Marcelo e nos executores do GitHub Actions (Fundação-3). O `CLAUDE.md` passa a dizer isso.
- [Dois projetos de teste sobem dois contêineres ao mesmo tempo, cerca de 2 GB de memória cada] → Aceitável hoje; se pesar, os testes podem compartilhar um contêiner.
- [`EnableRetryOnFailure` não permite transações abertas pelo código sem a estratégia de execução] → Quando um caso de uso precisar de transação explícita, ele usa `Database.CreateExecutionStrategy()`; fica registrado no guia de módulos.

## Migration Plan

Primeiro uso de banco no projeto; não há dados a migrar. Para desfazer, basta reverter o PR.

## Open Questions

Nenhuma.
