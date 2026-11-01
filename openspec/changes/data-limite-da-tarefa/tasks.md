## 1. Roadmap

- [x] 1.1 GanttA-2 em andamento no roadmap

## 2. Domínio e API

- [ ] 2.1 `ScheduleItem.Deadline` com a migração, nos casos de uso de incluir e editar item, no `ItemState` (desfazer) e na auditoria; testes "Data-limite cumprida", "Data-limite só de quem edita" e "Desfazer a data-limite"
- [ ] 2.2 `ScheduleFigures.DeadlineDeviationOf` e a contagem dos estourados; `deadline`, `deadlineDeviation` e `missedDeadlines` no `GET /schedule`; testes "Data-limite estourada" e "Data-limite do marco"
- [ ] 2.3 Visão geral com os itens estourados em "Precisa de atenção"; teste "Data-limite na atenção"
- [ ] 2.4 `Deadline` no XML exportado e na importação, com o `DeadlineOffset` do `SchedulePlanOutline`; teste "Data-limite no MS Project" e a ida e volta

## 3. Tela

- [ ] 3.1 `npm run api:update`; o campo no detalhe, o texto pelo `DueDate`, a coluna na Lista e no Expandir e o resumo; testes "Data-limite no detalhe" e "Data-limite na Lista e no resumo"
- [ ] 3.2 O marcador no `ScheduleGantt` e no `SchedulePrintPage`, com a legenda; teste "Marcador da data-limite"
- [ ] 3.3 A atenção na Visão geral e as colunas do Excel; teste "Data-limite no Excel"
- [ ] 3.4 Datas-limite na demonstração (`plan.ts`), uma estourada; conferência no navegador, no computador e no celular, nos temas claro e escuro

## 4. Documentação

- [ ] 4.1 `CLAUDE.md` e `docs/glossario.md` com a data-limite
