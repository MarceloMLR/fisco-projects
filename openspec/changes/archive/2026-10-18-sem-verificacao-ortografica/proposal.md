# Proposta: sem verificação de ortografia (Fundação-3.1)

## Why

A verificação de ortografia com o cspell, criada na Fundação-3, mais atrapalha do que ajuda: no VS Code, a extensão Code Spell Checker sem o dicionário de português marca como erro cerca de 100 palavras dos documentos, e cada nome fictício ou termo novo precisa entrar na lista do `cspell.json` para o PR passar. Marcelo pediu no chat para retirar o cspell do projeto e do VS Code. Nenhum ADR trata da ortografia; ela só existe na spec `integracao-continua`.

## What Changes

- A verificação do pull request deixa de ter a etapa de ortografia: sai a etapa `spelling` do workflow e o script `scripts/ci/spelling.sh`.
- Saem o `cspell.json` da raiz e o `scripts/ci/spelling.json`.
- `scripts/ci/package.json` e o lockfile ficam só com o OpenSpec CLI; saem o `cspell` e o `@cspell/dict-pt-br`.
- `.vscode/extensions.json` deixa de recomendar o Code Spell Checker e o dicionário de português.
- Os testes do workflow passam a esperar as cinco etapas restantes.
- `CLAUDE.md` sem as instruções de ortografia.

## Non-goals

- Outra ferramenta de ortografia no lugar do cspell.
- Mudar textos, comentários ou nomes de testes que foram escritos de um jeito ou de outro por causa do cspell: continuam válidos como estão.
- A regra "Protect main": a verificação exigida continua sendo a `verify`, com o mesmo nome.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `integracao-continua`: o requisito "Verificações em todo pull request" deixa de incluir a ortografia, e o cenário "Todas as verificações no pull request" passa a ter cinco etapas.

## Impact

- Arquivos removidos: `cspell.json`, `scripts/ci/spelling.json`, `scripts/ci/spelling.sh`.
- Arquivos alterados: `.github/workflows/pull-request.yml`, `scripts/ci/package.json` e `package-lock.json`, `.vscode/extensions.json`, `tests/FiscoProjects.Pipeline.Tests/PullRequestWorkflowTests.cs`, `CLAUDE.md`, `docs/roadmap.md`.
- Dependências removidas, só das verificações: `cspell` e `@cspell/dict-pt-br`.
- ADR tocado: 0055 (verificações do PR em scripts do repositório), sem mudar a decisão.
- Fora do repositório: a extensão Code Spell Checker já foi desinstalada do VS Code de Marcelo, a pedido dele.
