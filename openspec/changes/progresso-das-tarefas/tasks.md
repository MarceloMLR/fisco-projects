# Tasks

## 1. Domínio

- [ ] 1.1 Incluir `ScheduleItem.ReachedOn`, `Schedule.UpdateProgress` e `Schedule.SetReached`, o progresso opcional em `ChangeItem` e o marco atrasado em `IsLate` e `ScheduleFigures` (`LateMilestones`, fase e fase atual); verificar com os testes "Marco vencido", "Marco atingido ou do dia não fica atrasado", "Marcar marco como atingido", "Desmarcar marco" e "Alterar a tarefa mantém o progresso"
- [ ] 1.2 Incluir `CanUpdateOwnProgress` e `MayUpdateProgress` em `ProjectPermissions`; verificar com a tabela de permissões do domínio

## 2. Persistência, casos de uso e API

- [ ] 2.1 Criar a migração `MilestoneReached`; verificar com o teste de migração do desenvolvimento e o modelo sem mudanças pendentes
- [ ] 2.2 Parametrizar a recusa de `ScheduleEditing` e criar `UpdateScheduleItemProgress` e `SetMilestoneReached`, com o progresso opcional em `ChangeScheduleItem`; verificar com os testes de casos de uso
- [ ] 2.3 Mapear `PUT /schedule/items/{itemId}/progress` e `/reached`, e devolver `reachedOn`, `canUpdateProgress` e `lateMilestones`; verificar com os testes da API gerados da tabela de permissões ("Gerente atualiza qualquer tarefa", "Responsável atualiza a própria tarefa", "Leitor não atualiza o progresso", "Progresso de um marco", "Progresso num projeto encerrado", "Líder técnico não marca o marco", "Atingir uma tarefa") e com a auditoria do marco atingido
- [ ] 2.4 Atualizar o contrato (`npm run api:update`) e conferir o teste do contrato

## 3. Telas

- [ ] 3.1 Mostrar o estado do marco na Lista, os marcos atrasados no resumo e na lista de Projetos; verificar com "Estados do marco pela tela" e "Marco atrasado na lista"
- [ ] 3.2 Criar o bloco Progresso com "Iniciar", "Atualizar progresso" e "Concluir" e o diálogo do percentual, e tirar o progresso do formulário da tarefa; verificar com "Iniciar pela tela", "Concluir pela tela" e "Progresso sem permissão pela tela"
- [ ] 3.3 Criar o bloco Estado do marco com "Marcar como atingido" e "Desmarcar"; verificar com "Marcar marco pela tela" e os textos da auditoria

## 4. Documentação e verificação

- [ ] 4.1 Atualizar o glossário, o `CLAUDE.md` e o roadmap; verificar lendo os trechos alterados
- [ ] 4.2 Rodar `scripts/ci` sem erros nem avisos e conferir as telas nos temas claro e escuro, no computador e no celular, guardando as capturas para o resumo

## Workflow follow-up

- Abrir o PR, atualizar o roadmap para "PR aberto" e avisar Marcelo com as decisões e as capturas.
- Depois da aprovação, arquivar a change, aceitar o ADR-0086, marcar a Cronograma-2 como concluída e esperar o merge.
