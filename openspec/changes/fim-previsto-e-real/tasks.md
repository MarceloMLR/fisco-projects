## 1. Roadmap

- [x] 1.1 Status-3 "Em andamento" no `docs/roadmap.md`

## 2. Dia da conclusão informado

- [x] 2.1 O dia informado no domínio (`SetProgress`, `SetReached`, `IsValidDoneDay`), nos casos de uso e nos pedidos do progresso e do marco, com os testes do domínio e da API dos cenários "Concluir num dia informado", "Dia da conclusão recusado" e "Marco atingido num dia informado", e o contrato da API atualizado
- [x] 2.2 "Concluída em" no diálogo do progresso e "Alterar dia" na tarefa concluída e no marco atingido, com os testes de tela dos cenários de "Alterar o dia da conclusão pela tela"
- [x] 2.3 A demonstração com o dia planejado nas tarefas concluídas e nos marcos atingidos, com os testes de `web/scripts/demo` do cenário "Fim real na demonstração"

## 3. Regras de tela

- [x] 3.1 `plannedEndOf`, `actualEndOf` e `phaseEndsOf` em `schedule.ts`, com os testes dos cenários de "Fim previsto e fim real"

## 4. Lista, Expandir e Excel

- [x] 4.1 Colunas "Fim previsto" (só com a linha de base) e "Fim real" na Lista, fora da edição na tabela, com os testes de tela dos cenários de "Fim previsto e fim real na Lista"
- [x] 4.2 As duas colunas na tabela completa do Expandir, com o "Fim previsto" pela linha de base ligada em "Exibir", com os testes do cenário "Fim previsto e real no Expandir"
- [x] 4.3 "Fim previsto (linha de base)" e "Fim real" depois de "Fim" no Excel, com o teste do cenário "Fim previsto e real no Excel"
- [x] 4.4 Telas conferidas nos temas claro e escuro e no celular; `npm run lint`, `npm run typecheck` e `npm test` limpos

## 5. Documentação e verificação

- [x] 5.1 `CLAUDE.md`, `docs/glossario.md` e o ADR-0167 no índice, com o fim previsto, o fim real e o dia informado, e `openspec validate --all --strict` limpo
- [ ] 5.2 Verificação: `dotnet test`, `dotnet format`, os testes do frontend e a demonstração carregada mostrando as colunas na Lista, no Expandir e no Excel

## Workflow follow-up

- PR com a aprovação de Marcelo, archive na mesma branch, roadmap "Concluída" e o merge por Marcelo.
