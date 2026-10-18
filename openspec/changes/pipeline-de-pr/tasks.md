# Tasks

## 1. Scripts de verificação

- [ ] 1.1 Criar `scripts/ci/build.sh`, `test.sh` e `format.sh` e declarar a auditoria do NuGet no `Directory.Build.props`; verificar que os três passam na branch e falham, sem commitar a quebra, com um aviso de compilação, um teste quebrado e um arquivo fora do padrão
- [ ] 1.2 Criar `scripts/ci/package.json` com lockfile (OpenSpec CLI 1.14.3, `cspell`, `@cspell/dict-pt-br`), `.nvmrc`, `scripts/ci/spelling.json`, `specs.sh` e `spelling.sh`; verificar que passam e falham, sem commitar a quebra, com uma spec inválida e uma palavra errada

## 2. Workflow

- [ ] 2.1 Criar `.github/workflows/pull-request.yml` com o job `verify`, uma etapa por script, ações fixadas pelo commit, `ubuntu-24.04`, permissão só de leitura e sem segredos
- [ ] 2.2 Criar `tests/FiscoProjects.Pipeline.Tests` com os cenários "Pull request para a main", "Todas as verificações no pull request", "Comando que falha reprova a verificação", "Pacote com vulnerabilidade conhecida", "Etapas do workflow", "Script chamado de outra pasta", "Sem segredos" e "Versões fixadas"; verificar com `dotnet test`

## 3. Documentação e verificação final

- [ ] 3.1 Atualizar a seção 6 do `CLAUDE.md` com os scripts de verificação e o nome da verificação exigida; verificar que os comandos documentados rodam como escritos
- [ ] 3.2 Rodar todos os scripts de `scripts/ci` na branch, sem erro
- [ ] 3.3 Abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions
- [ ] 3.4 Com a autorização de Marcelo, exigir a verificação `verify` na regra "Protect main" e conferir pela API

## Workflow follow-up

- Marcar a Fundação-3 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: `/opsx:archive`, roadmap como "Concluída" e merge.
