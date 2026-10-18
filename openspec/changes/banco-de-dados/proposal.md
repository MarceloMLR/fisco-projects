# Proposta: banco de dados (Fundação-2)

## Why

Os módulos de negócio, a partir da Acesso-1, precisam gravar dados. Antes disso, o backend precisa de um jeito único de acessar o Azure SQL em que cada módulo seja dono das próprias tabelas, num schema próprio (ADR-0047), com migrações por módulo, datas em UTC (ADR-0013), chaves GUID v7 geradas pela aplicação (ADR-0061) e testes contra um SQL Server de verdade em contêiner (ADR-0048, ADR-0056).

## What Changes

- Projeto compartilhado `FiscoProjects.Persistence` com o contexto base dos módulos (`ModuleDbContext`), o registro de cada contexto com schema e histórico de migrações próprios, as convenções comuns (UTC, chave gerada pela aplicação) e a verificação do banco. Decisão registrada no ADR-0075.
- EF Core com o provedor do SQL Server, num banco só por instalação e uma connection string (`ConnectionStrings:FiscoProjects`), lida da configuração e nunca do repositório.
- Migrações por módulo, aplicadas de uma vez para todos os módulos registrados; automáticas ao subir a API só no ambiente de desenvolvimento.
- `/health` passa a verificar o banco e responde 503 quando ele está fora do ar ou não configurado.
- SQL Server local no Docker (`compose.yaml`, senha no `.env`, que não vai para o Git) e nos testes, com Testcontainers e a mesma imagem.
- Ferramenta `dotnet-ef` fixada no manifesto de ferramentas do repositório.
- Regras de arquitetura: `Domain` e `Application` não usam a persistência compartilhada, e ela não usa módulos.
- Guia de módulos e `CLAUDE.md` com o banco de cada módulo e os comandos de desenvolvimento local.

## Non-goals

- Tabelas de negócio e o primeiro módulo (Acesso-1).
- Auditoria automática ao salvar (Acesso-3); o contexto base só deixa o ponto de extensão.
- Banco no Azure, pacote de migração no pipeline e publicação (Fundação-6 e Fundação-7).
- Pipeline de CI (Fundação-3), embora os testes já rodem em qualquer máquina com Docker.
- Backup, retenção e monitoramento (Operação-3 e Operação-4).

## Capabilities

### New Capabilities

- `persistencia`: como os módulos guardam dados: schema e migrações por módulo, aplicação das migrações, datas em UTC e chaves geradas pela aplicação.

### Modified Capabilities

- `plataforma-api`: a saúde da aplicação passa a incluir o acesso ao banco.
- `arquitetura-modular`: as regras passam a cobrir o projeto compartilhado de persistência.

## Impact

- Código novo: `src/FiscoProjects.Persistence`, testes de persistência com SQL Server em contêiner, `compose.yaml`, `.env.example`, `.config/dotnet-tools.json`.
- Código alterado: host da API (registro da persistência, verificação do banco, migração no desenvolvimento), testes de arquitetura e da API.
- Dependências novas: `Microsoft.EntityFrameworkCore.SqlServer` (MIT), `Microsoft.EntityFrameworkCore.Design` (MIT, só em tempo de desenvolvimento), `Testcontainers.MsSql` (MIT, só nos testes) e a imagem `mcr.microsoft.com/mssql/server` 2022, usada só no desenvolvimento e nos testes.
- Para rodar os testes é preciso Docker em execução.
- ADRs implementados: 0047 (schema por módulo), 0048, 0013 (UTC), 0061 (GUID v7), 0057 (saúde com o banco). ADR novo: 0075.
