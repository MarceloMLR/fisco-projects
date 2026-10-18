# Tasks

## 1. Verificação do PR sem ortografia

- [x] 1.1 Tirar a etapa `spelling` do workflow e apagar `scripts/ci/spelling.sh`, `scripts/ci/spelling.json` e o `cspell.json`; deixar `scripts/ci/package.json` e o lockfile só com o OpenSpec CLI; atualizar o teste "Todas as verificações no pull request" para as cinco etapas; verificar com `dotnet test` (testes do workflow) e `bash scripts/ci/specs.sh`
- [x] 1.2 Tirar o Code Spell Checker e o dicionário de português de `.vscode/extensions.json` e as instruções de ortografia do `CLAUDE.md`; conferir com uma busca por `cspell` e `spelling` no repositório, que só pode achar as changes arquivadas

## 2. Verificação final

- [ ] 2.1 Rodar os scripts de `scripts/ci` sem erro, abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions, já sem a etapa de ortografia

## Workflow follow-up

- Marcar a Fundação-3.1 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: `/opsx:archive` na mesma branch, roadmap como "Concluída" e merge.
