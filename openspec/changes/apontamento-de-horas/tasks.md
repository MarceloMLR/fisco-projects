## 1. Módulo

- [x] 1.1 Projetos `FiscoProjects.TimeTracking` e `tests/FiscoProjects.TimeTracking.Tests`, na solução, referenciados e registrados no host (`AddTimeTrackingModule`, `MapTimeTrackingEndpoints`)
- [x] 1.2 `TimeEntry` e `TimeEntryAccess` no domínio, com os testes: "Dia depois de hoje", "Horas fora dos quartos de hora", "Apontamento sem descrição", "Tarefa e chamado juntos" e a matriz "Permissões nas horas por papel" (`[Theory]` por papel, mais o administrador fora do projeto e o projeto encerrado)
- [x] 1.3 `TimeTrackingDbContext` (schema `timetracking`), a configuração com a auditoria (rótulo, referências e contexto), a migração e a fábrica do `dotnet ef`

## 2. Contratos

- [x] 2.1 `IProjectTicketList` no `Tickets.Contracts`, implementado no `Tickets`, coberto pelos cenários da API com chamados
- [x] 2.2 `ProjectItemKind.TimeEntries` e o `TimeEntryProjectDeletion` (contagem e exclusão na transação recebida)

## 3. Casos de uso e API

- [x] 3.1 `ListTimeEntries`, `AddTimeEntry`, `ChangeTimeEntry`, `DeleteTimeEntry`, `ListTimeEntryProjects` e `ListTimeEntryWork`, com o hoje no fuso da instalação, a soma do dia e as opções de tarefa e chamado
- [x] 3.2 Endpoints em `/api/time-entries`, com os Problem Details nos campos e o contrato OpenAPI
- [x] 3.3 `TimeTrackingWarmUp` (lista e opções, só leituras)
- [x] 3.4 Testes da API com o nome de cada cenário: "Apontar horas no projeto", "Mais de 24 horas no dia", "Apontar numa tarefa", "Apontar num chamado", "Tarefa de outro projeto", "Chamado de outro projeto", "Tarefa excluída depois", "Só as próprias horas", "Horas de outra pessoa", "Período longo demais", "Administrador corrige as horas de outra pessoa", "Quem saiu do projeto", "Horas na auditoria", "Permissões nas horas por papel", "Administrador fora do projeto", "Horas de um projeto de fora", "Horas de projeto encerrado", "Projetos para apontar" e "Tarefas e chamados do projeto"
- [x] 3.5 Exclusão do projeto: "Excluir projeto" e "Resumo da exclusão" com as horas

## 4. Tela

- [x] 4.1 Item Minhas horas no `mainNavigation`, rota `/hours`, título e textos em `pt-BR.json`; "Navegação principal" e "Menu no celular" atualizados
- [x] 4.2 `npm run api:update`, as consultas (`timeEntriesQuery`, `timeEntryProjectsQuery`, `timeEntryWorkQuery`) e os simuladores do MSW (`web/src/test/demoTimeEntries.ts`)
- [x] 4.3 `hours.ts` (`parseHours`, formatação das horas e a semana) com os testes
- [x] 4.4 `MyHoursPage` (semana, linhas de grupo por dia, totais, troca de semana, vazio, "⋮") e `TimeEntryDialog`
- [x] 4.5 Testes de tela: "Semana pela tela", "Apontar pela tela", "Erro no campo", "Corrigir e apagar pela tela", "Trocar a semana", "Semana sem horas" e "Minhas horas no celular"
- [x] 4.6 "Horas" na Auditoria (`describeAuditEntry`) e as horas no `DeleteProjectDialog`

## 5. Demonstração

- [x] 5.1 `hours` no plano (`plan.ts`) e a carga (`load.ts`), com os testes "Horas na demonstração"

## 6. Documentação e conferência

- [x] 6.1 `CLAUDE.md` (o módulo `TimeTracking` na seção 3 e a tela na seção 6), glossário e roadmap
- [ ] 6.2 Capturas de Minhas horas no computador e no celular, claro e escuro, para o PR
- [ ] 6.3 Build sem avisos, testes, `dotnet format`, lint, typecheck e `openspec validate --all --strict`

## Workflow follow-up

- PR, aprovação de Marcelo, archive na mesma branch e merge por Marcelo.
