# Tasks

## 1. Domínio e banco

- [x] 1.1 Incluir `ScheduleItem.ParentId`, a configuração do EF Core e a migração `ScheduleSubtasks`; verificar com o teste de banco do módulo
- [x] 1.2 Aplicar as regras de nível, a mudança de fase da tarefa-resumo e a recusa de progresso e novas datas nela; verificar com os testes de domínio "Incluir subtarefa", "Subtarefa fora do nível", "Mudar a fase da tarefa-resumo" e "Progresso de uma tarefa-resumo"
- [x] 1.3 Calcular a tarefa-resumo a cada mudança (`Summarize`) e ajustar o `ScheduleFigures`; verificar com "Datas e progresso da tarefa-resumo", "Tarefa-resumo atrasada", "Progresso sem contar a tarefa-resumo" e "Tarefa vencida"
- [x] 1.4 Restringir as dependências a quem faz o trabalho e passar as dependências na primeira e na última subtarefa; verificar com "Primeira subtarefa herda as dependências", "Depender de uma tarefa-resumo", "Empurrar subtarefas" e "Excluir a última subtarefa"
- [x] 1.5 Excluir a tarefa-resumo com as subtarefas; verificar com "Excluir tarefa com subtarefas"

## 2. API e auditoria

- [x] 2.1 Incluir `parentId` no pedido e `parentId` e `hasSubtasks` na resposta, com a ordem de árvore e o `canUpdateProgress` da tarefa-resumo; verificar com os testes de casos de uso e da API dos cenários do grupo 1
- [x] 2.2 Mostrar "Dentro de" pelo nome na auditoria; verificar com "Subtarefa na auditoria"
- [x] 2.3 Atualizar o contrato (`npm run api:update`)

## 3. Tela

- [x] 3.1 Mostrar as subtarefas na Lista e na Linha do tempo, com a tarefa-resumo como resumo; verificar com "Subtarefas na lista" e "Subtarefas na linha do tempo"
- [x] 3.2 Incluir "Dentro de", "Nova subtarefa", o detalhe da tarefa-resumo e o filtro de "Depende de"; verificar com "Nova subtarefa pela tela" e "Detalhe da tarefa-resumo"

## 4. Documentação e verificação

- [ ] 4.1 Atualizar `docs/discovery.md`, o glossário, o `CLAUDE.md` e o roadmap; verificar lendo os trechos alterados
- [ ] 4.2 Rodar `scripts/ci` sem erros nem avisos e conferir a Lista e a Linha do tempo com subtarefas nos temas claro e escuro, guardando as capturas para o resumo

## Workflow follow-up

- Abrir o PR, atualizar o roadmap para "PR aberto" e avisar Marcelo com as decisões e as capturas.
- Depois da aprovação, arquivar a change, aceitar o ADR-0088, marcar no ADR-0019 a linha substituída, marcar a Cronograma-4 como concluída e esperar o merge.
