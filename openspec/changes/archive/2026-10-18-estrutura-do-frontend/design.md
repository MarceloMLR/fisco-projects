# Design: estrutura do frontend (Fundação-4)

## Context

Motivação em proposal.md. O host (`ApiSetup`) já tem Problem Details em todos os ambientes, `/health`, o contrato em `/openapi/v1.json` só no desenvolvimento e o grupo `/api`, ainda sem rotas. A API roda em `http://localhost:5175` no desenvolvimento. O CI roda os scripts de `scripts/ci` no Node 24 (`.nvmrc`) e no .NET 10. Não existe `web/`.

## Goals / Non-Goals

**Goals:**

- A Fundação-5 e a Acesso-1 só acrescentam telas, sem decidir ferramentas nem regras comuns.
- Os erros comuns (texto fora da tradução, contrato esquecido, licença proibida) reprovam sozinhos, sem depender de revisão.

**Non-Goals:**

- Estilo e componentes: a página provisória usa o estilo padrão do navegador, já que cores, espaços e cantos só podem vir dos tokens (Fundação-5).

## Decisions

### 1. Ferramentas e versões (ADR-0076)

React 19.3, Vite 8.3, Vitest 5.0, Testing Library, jsdom, ESLint 10 com typescript-eslint, Prettier 3, i18next 26 com react-i18next 17, openapi-typescript 7 e license-checker-rseidelsohn 5. Versões exatas no `package.json` (`save-exact` no `.npmrc`), com lockfile, e `engines` com o Node 24.

- TypeScript 5.9.3, não o 6 ou o 7: é a versão mais nova que o typescript-eslint (menor que 6.1) e o openapi-typescript (5.x) aceitam. Troca quando as duas ferramentas aceitarem.
- `eslint-plugin-jsx-a11y` fica de fora: ainda não aceita o ESLint 10. Volta quando aceitar, junto com as telas da Fundação-5.

### 2. Estrutura de `web/`

```
web/
  index.html, vite.config.ts, eslint.config.js, tsconfig*.json, .prettierrc.json, .npmrc
  src/
    main.tsx, App.tsx, App.test.tsx
    i18n/        configuração do i18next, tipos das chaves e testes
    locales/     pt-BR.json
    api/         openapi.json e schema.ts (gerados) e o teste de tipos
    test/        setup do Testing Library e fixtures de lint
```

TypeScript `strict` com `noUncheckedIndexedAccess`, `noImplicitOverride`, `noUnusedLocals`, `noUnusedParameters` e `verbatimModuleSyntax`. ESLint com `strictTypeChecked`, regras do React Hooks e do React Refresh.

### 3. Textos

- Um namespace só (`translation`), em `src/locales/pt-BR.json`, idioma fixo pt-BR na v1 (ADR-0013).
- As chaves são tipadas a partir do próprio JSON (`CustomTypeOptions` do i18next): chave inexistente é erro de tipo.
- `eslint-plugin-i18next` com `no-literal-string` em modo JSX: texto visível escrito direto num componente é erro de lint. Testes e fixtures ficam de fora da regra.
- O título da aba (`<title>` do `index.html`) é o nome do produto, que não se traduz.

### 4. Contrato e tipos

- `Microsoft.Extensions.ApiDescription.Server` gera o contrato no build do host, mas só quando pedido (`OpenApiGenerateDocumentsOnBuild=false` por padrão). `npm run api:update`, em `web/`, gera o `openapi.json` e roda o openapi-typescript para o `schema.ts`.
- Teste do backend "Contrato versionado desatualizado": compara o `/openapi/v1.json` publicado no desenvolvimento com o `web/src/api/openapi.json`, ignorando `servers`, que depende do endereço da requisição.
- Teste do frontend "Tipos do frontend desatualizados": gera os tipos em memória a partir do `openapi.json` e compara com o `schema.ts`.
- Alternativa: gerar o contrato em todo build e comparar com o Git no CI. Reescreve arquivos a cada build e exige a pasta limpa para a checagem; os testes pegam o mesmo erro na máquina e no CI.

### 5. Host serve o frontend

- O Vite gera o build em `src/FiscoProjects.Api/wwwroot` (fora do Git). O host usa `UseStaticFiles` com cache de um ano, `immutable`, para `/assets/*` (nomes versionados pelo Vite) e `no-cache` para o resto, inclusive a página principal.
- `MapFallback("/api/{**path}")` devolve 404 sem corpo, que vira Problem Details; `MapFallbackToFile("index.html")` responde os demais caminhos que não são arquivo. Rotas explícitas (`/health`, `/openapi`, módulos) continuam valendo antes do fallback.
- No desenvolvimento, `npm run dev` sobe o Vite em `http://localhost:5173`, que repassa `/api`, `/health` e `/openapi` para a API em `5175`.
- Os testes do host usam uma pasta temporária como raiz dos arquivos estáticos, com uma página e um arquivo de build falsos.

### 6. Verificação no PR

`scripts/ci/frontend.sh`, etapa nova entre `format` e `specs`:

1. `npm ci`;
2. licenças de todas as dependências, inclusive as de desenvolvimento, com lista permitida: MIT, MIT-0, ISC, BSD-2-Clause, BSD-3-Clause, Apache-2.0, 0BSD, CC0-1.0, CC-BY-3.0, CC-BY-4.0, BlueOak-1.0.0, Unlicense, MPL-2.0 e Python-2.0 (as CC-BY só aparecem em dados de ferramentas de desenvolvimento);
3. `npm audit --omit=dev` (só o que vai para o navegador);
4. Prettier, ESLint, tipos, testes e build.

O workflow ganha a etapa `frontend`, que roda mesmo depois de uma falha anterior, como as outras.

## Risks / Trade-offs

- [O build do Vite escreve dentro da pasta do host] → É uma pasta gerada e fora do Git; a publicação (Fundação-7) roda o build do frontend antes do `dotnet publish`.
- [`api:update` chama o `dotnet build` a partir do npm] → Funciona no Windows e no Linux; os testes avisam quando o passo for esquecido.
- [ESLint 10 é recente e alguns plugins ainda não o aceitam] → Os plugins usados declaram suporte; o de acessibilidade espera.
- [A troca para o Node 26, depois de 28/10/2026, muda também o `engines`] → Está na mesma change da troca de versão (ADR-0060).
- [`npm audit` depende do registro do npm no CI] → Se o registro cair, a verificação falha e é rodada de novo.

## Migration Plan

Sem dados. Para desfazer, basta reverter o PR.

## Open Questions

Nenhuma.
