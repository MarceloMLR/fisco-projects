## 1. Roadmap

- [ ] 1.1 Status-3 "Em andamento" no `docs/roadmap.md`

## 2. Regras de tela

- [ ] 2.1 `plannedEndOf`, `actualEndOf` e `phaseEndsOf` em `schedule.ts`, com os testes dos cenários de "Fim previsto e fim real"

## 3. Lista, Expandir e Excel

- [ ] 3.1 Colunas "Fim previsto" (só com a linha de base) e "Fim real" na Lista, fora da edição na tabela, com os testes de tela dos cenários de "Fim previsto e fim real na Lista"
- [ ] 3.2 As duas colunas na tabela completa do Expandir, com o "Fim previsto" pela linha de base ligada em "Exibir", com os testes do cenário "Fim previsto e real no Expandir"
- [ ] 3.3 "Fim previsto (linha de base)" e "Fim real" depois de "Fim" no Excel, com o teste do cenário "Fim previsto e real no Excel"
- [ ] 3.4 Telas conferidas nos temas claro e escuro e no celular; `npm run lint`, `npm run typecheck` e `npm test` limpos

## 4. Documentação e verificação

- [ ] 4.1 `CLAUDE.md` e `docs/glossario.md` com o fim previsto e o fim real, e `openspec validate --all --strict` limpo
- [ ] 4.2 Verificação: os testes do frontend e a demonstração carregada mostrando as colunas na Lista, no Expandir e no Excel

## Workflow follow-up

- PR com a aprovação de Marcelo, archive na mesma branch, roadmap "Concluída" e o merge por Marcelo.
