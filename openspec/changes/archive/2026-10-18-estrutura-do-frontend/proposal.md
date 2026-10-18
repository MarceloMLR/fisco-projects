# Proposta: estrutura do frontend (Fundação-4)

## Why

As telas da v1 são em React com TypeScript (ADR-0049), servidas pelo mesmo host da API (ADR-0047, ADR-0054). Antes da primeira tela de verdade (Fundação-5 e Acesso-1), o projeto precisa do frontend montado com as regras que valem para todas: TypeScript estrito, ESLint e Prettier (ADR-0061), testes (ADR-0056), textos só em arquivos de tradução (ADR-0013), tipos gerados do contrato da API (ADR-0061) e verificação no PR (ADR-0055).

## What Changes

- Aplicação React com TypeScript estrito em `web/`, com Vite, ESLint, Prettier, Vitest e Testing Library. Ferramentas registradas no ADR-0076.
- Textos da interface em `web/src/locales/pt-BR.json`, com chaves em inglês e react-i18next. Texto escrito direto numa tela reprova o lint, e chave que não existe reprova a checagem de tipos.
- Contrato OpenAPI versionado em `web/src/api/openapi.json`, gerado pelo build do backend sob demanda, e tipos TypeScript gerados dele com o openapi-typescript. Contrato ou tipos desatualizados reprovam os testes.
- O host da API serve o build do frontend: caminhos fora de `/api` que não são arquivos recebem a página do frontend, arquivos do build com nome versionado vão com cache longo e a página principal sem cache. Caminho inexistente sob `/api` continua com 404 em Problem Details.
- Uma página inicial provisória, sem estilo, só com o nome do produto e um aviso de que a interface está em construção. O visual entra na Fundação-5.
- Verificação do PR com um script novo, `frontend.sh`: licenças das dependências, pacotes vulneráveis, formatação, lint, tipos, testes e build.
- `CLAUDE.md`, `.editorconfig`, `cspell.json` e extensões do VS Code atualizados para o frontend.

## Non-goals

- MUI, tokens do design system, IBM Plex, barra lateral e layout (Fundação-5).
- Rotas, cliente HTTP, MSW e tratamento do 401 (com a primeira chamada à API, na Acesso-1).
- Datas e horas no fuso da instalação (Acesso-5).
- Build do frontend na publicação (Fundação-7) e testes de ponta a ponta com Playwright (depois do ambiente de desenvolvimento no ar).
- Cabeçalhos de segurança (CSP e similares), com a autenticação na Acesso-1.
- Verificação de licenças dos pacotes do NuGet (fica registrada como pendência no resumo).

## Capabilities

### New Capabilities

- `aplicacao-web`: como o frontend é entregue pela aplicação e as regras comuns das telas: textos em arquivos de tradução e tipos da API gerados do contrato.

### Modified Capabilities

- `plataforma-api`: o cenário "Rota inexistente" passa a valer para caminhos sob `/api`, já que os demais caminhos vão para o frontend.
- `integracao-continua`: as verificações do PR passam a incluir o frontend, as licenças e os pacotes vulneráveis do frontend.

## Impact

- Código novo: `web/` (aplicação, configuração, testes, traduções, contrato e tipos gerados), `scripts/ci/frontend.sh`, testes do host para o frontend e para o contrato.
- Código alterado: host da API (arquivos do frontend e rota de fallback), `FiscoProjects.Api.csproj` (geração do contrato no build), workflow do PR, testes do pipeline, `.gitignore`.
- Dependências novas, todas MIT, ISC, BSD ou Apache-2.0: React 19, react-i18next e i18next (produção); Vite, TypeScript, ESLint e plugins, Prettier, Vitest, Testing Library, jsdom, openapi-typescript e license-checker-rseidelsohn (desenvolvimento); `Microsoft.Extensions.ApiDescription.Server` (MIT, só no build).
- ADR novo: 0076 (Vite e openapi-typescript). ADRs implementados: 0013 (textos em tradução), 0047 (host serve o React), 0049, 0055 (frontend no PR), 0056 (Vitest e Testing Library), 0061 (TypeScript estrito, ESLint, Prettier, tipos do contrato, licenças).
