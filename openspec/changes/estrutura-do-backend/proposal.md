# Proposta: estrutura do backend (Fundação-1)

## Why

O repositório ainda não tem código. Antes do primeiro módulo de negócio, o backend precisa nascer com a estrutura do monolito modular (ADR-0047) e com as regras de dependência garantidas por testes, para que nenhuma change posterior misture camadas ou módulos. Também precisa nascer com as convenções de API e de código do ADR-0061 e as versões fixadas do ADR-0060, que valem para todo o resto da v1.

## What Changes

- Solução .NET 10 com o host `FiscoProjects.Api` e o `FiscoProjects.SharedKernel`, na estrutura de pastas do ADR-0047 (`src/`, `src/Modules/`, `tests/`).
- Convenção dos módulos: projeto `FiscoProjects.<Modulo>` com as camadas `Domain`, `Application`, `Infrastructure` e `Endpoints`, e projeto `FiscoProjects.<Modulo>.Contracts`, com um guia curto de como criar um módulo. Nenhum módulo de negócio é criado nesta change; o primeiro vem na Acesso-1.
- Testes de arquitetura que aplicam as regras de dependência a todo módulo existente e que provam, com exemplos de módulos válidos e inválidos, que cada regra pega a violação.
- Endpoint `/health`, que responde se a aplicação está no ar (as verificações de banco e do Microsoft Graph entram nas changes desses recursos, ADR-0057).
- Erros da API sempre em Problem Details (RFC 9457), inclusive rota inexistente e erro inesperado, sem expor detalhes internos.
- Contrato OpenAPI gerado pelo ASP.NET Core, publicado só no ambiente de desenvolvimento.
- Rotas dos módulos agrupadas sob `/api`.
- Tipo base de entidade no `SharedKernel` com identificador GUID versão 7.
- Configuração de build comum: `global.json` fixando o SDK do .NET 10, nullable habilitado, avisos tratados como erro, `.editorconfig` verificado por `dotnet format`, versões de pacotes centralizadas.
- `CLAUDE.md` com os comandos reais de build, teste e formatação do backend.

## Non-goals

- Banco de dados, EF Core, migrações e Docker (Fundação-2).
- Pipeline de CI no GitHub Actions (Fundação-3).
- Frontend, geração dos tipos TypeScript a partir do OpenAPI e servir o React pelo host (Fundação-4).
- Autenticação, sessão e autorização (Acesso-1 e Acesso-2); por enquanto não há endpoint protegido.
- Qualquer módulo de negócio ou regra de domínio.
- Infraestrutura no Azure, monitoramento e alertas (Fundação-6 e Operação-3).
- Interface visual: esta change não tem tela.

## Capabilities

### New Capabilities

- `arquitetura-modular`: organização do backend em módulos e camadas, regras de dependência entre camadas e entre módulos verificadas por testes, e identificadores de entidade em GUID versão 7.
- `plataforma-api`: comportamento comum da API HTTP: saúde da aplicação, formato dos erros, contrato OpenAPI e prefixo das rotas.

### Modified Capabilities

Nenhuma (não há specs anteriores).

## Impact

- Código novo: `src/FiscoProjects.Api`, `src/FiscoProjects.SharedKernel`, `src/Modules/README.md`, `tests/FiscoProjects.Architecture.Tests`, `tests/FiscoProjects.Api.Tests`, `tests/FiscoProjects.SharedKernel.Tests`.
- Arquivos de build na raiz: `FiscoProjects.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`.
- Dependências novas, todas com licença permitida pelo ADR-0061: `Microsoft.AspNetCore.OpenApi` (MIT), xUnit v3 (Apache 2.0), AwesomeAssertions (Apache 2.0), NSubstitute (BSD), ArchUnitNET (Apache 2.0), `Microsoft.AspNetCore.Mvc.Testing` (MIT).
- `CLAUDE.md`, seção 6, passa a ter os comandos do backend.
- ADRs implementados: 0047, 0060, 0061. ADRs seguidos: 0056 (testes) e 0057 (endereço de saúde).
