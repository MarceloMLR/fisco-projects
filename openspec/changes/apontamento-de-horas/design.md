## Context

O ADR-0128 trouxe as horas do projeto, só em horas: cada pessoa aponta as próprias horas, e o administrador compara as apontadas com as contratadas e as adicionais. Esta change entrega o apontamento (Horas-1). A Horas-2 acrescenta as horas contratadas, as adicionais e a visão gerencial no mesmo módulo.

Os módulos já resolvem os pedaços de que o apontamento precisa. O `Projects.Contracts` diz que projetos a pessoa vê e o papel dela (`IProjectAccess`) e lista as tarefas do cronograma (`IScheduleItems`). O `Credentials` e o `Documents` mostram o padrão: as permissões num lugar só, o 404, o 403 e o 409 de projeto encerrado, a exclusão do projeto (`IProjectDeletion`) e o aquecimento (`IWarmUp`). O dia de hoje no fuso da instalação vem do `InstallationInfo.DayOf` do `Administration.Contracts`, como no `MyWork` e no `Audit`. Falta um contrato para os chamados de um projeto.

## Goals / Non-Goals

**Goals:**

- Apontamento rápido, no computador e no celular, com a tarefa ou o chamado só quando a pessoa quer.
- As regras de quem aponta, vê e corrige num lugar só, com a matriz coberta por inteiro (ADR-0056).
- Um módulo pronto para a Horas-2, sem migrar dados depois.

**Non-Goals:**

- Horas contratadas, adicionais e visão gerencial (Horas-2).
- Valores em reais, aprovação, fechamento do mês, cronômetro (ADR-0128).
- Apontar na página do chamado ou no detalhe da tarefa.

## Decisions

### Módulo novo `TimeTracking`

As horas ficam num módulo novo, `TimeTracking`, com o schema `timetracking`, e não no `Projects`. O `Projects` já é o maior módulo, e toda mudança nele passa pela transação serializável do cronograma (ADR-0085), que o apontamento não precisa. As horas usam o projeto só como referência, como os acessos e os links, e a Horas-2 põe aqui as contratadas e as adicionais, que só o administrador vê. O módulo segue o guia `src/Modules/README.md`: `Domain`, `Application`, `Infrastructure` e `Endpoints`, o projeto `Contracts` (vazio por ora, com a referência de auditoria `TimeTrackingAudit.TimeEntryReference` só se outro módulo precisar) e os testes em `tests/FiscoProjects.TimeTracking.Tests`.

Alternativa descartada: dentro do `Projects`, que teria as permissões e o cronograma à mão, mas cresceria o módulo e prenderia o apontamento à transação do cronograma.

### Dados

Uma tabela, `timetracking.time_entries`: `Id` (GUID v7), `UserId`, `ProjectId`, `Day` (`date`), `Hours` (`decimal(4,2)`), `Description` (até 500), `ScheduleItemId` (opcional), `TicketId` (opcional), `CreatedAt` e `UpdatedAt`, com `rowversion`. Índices em `(UserId, Day)` (a lista da pessoa e a soma do dia) e em `ProjectId` (a exclusão e a Horas-2).

A auditoria é a automática do contexto base (ADR-0039): o rótulo é a descrição; `ProjectId` é referência ao projeto e contexto do item (`IsAuditContext`), `UserId` é referência à pessoa, `ScheduleItemId` à tarefa (`ProjectsAudit.ScheduleItemReference`) e `TicketId` ao chamado (`TicketsAudit.TicketReference`), todos mostrados pelos nomes atuais (ADR-0084). `CreatedAt`, `UpdatedAt` e a versão ficam fora. A tela da Auditoria ganha o tipo "Horas" e os campos no `describeAuditEntry`.

O apontamento guarda só o `UserId`, sem nome nem e-mail: a anonimização não precisa de apagador (`IPersonalDataEraser`), e o nome atual vem do `Users` ao listar.

### Regras no domínio

- `TimeEntry` valida o dia contra o hoje recebido, as horas (de 0,25 a 24, `Hours * 4` inteiro) e a descrição; a soma do dia é conferida no caso de uso, com os outros apontamentos da pessoa naquele dia (sem o que está sendo corrigido).
- `TimeEntryAccess` decide, num lugar só, a partir do `ProjectAccess` e de quem pede: `MayLog` (administrador ou papel diferente de leitor, e o projeto em aberto) e `MayChange(entry)` (dono ou administrador, e `MayLog` no projeto do apontamento). Quem não vê o projeto recebe 404, o leitor 403, e o projeto encerrado 409 (`project-closed`), na ordem do `CredentialAreas.ForChangeAsync`.
- O apontamento de outra pessoa responde 404 a quem não é administrador, para não dizer que ele existe.

### Tarefa e chamado

A tarefa vem do `IScheduleItems.ItemsAsync` do projeto, só as do tipo `Task`. O chamado vem de um contrato novo do `Tickets.Contracts`, `IProjectTicketList`, implementado pelo `Tickets`: `ListAsync(projectId)` devolve `ProjectTicketInfo(Id, Number, Title, IsClosed)` dos chamados do projeto, e `FindAsync(ticketId)` devolve o chamado com o projeto. Pelo ADR-0044, todas as pessoas do projeto veem todos os chamados dele; então basta o chamado ser do projeto em que a pessoa aponta. Uma tarefa excluída depois deixa o `ScheduleItemId` sem item: a lista mostra "Tarefa excluída", e a auditoria segue a regra de referência apagada (ADR-0084).

### API

Grupo `/api/time-entries`, com a tag "Horas":

| Rota | Nome | O quê |
| --- | --- | --- |
| `GET /api/time-entries?from&to&personId` | `ListTimeEntries` | Os apontamentos do período (até 93 dias), da pessoa na sessão ou, só para o administrador, de `personId`; cada um com o projeto (código e nome), a tarefa ou o chamado e `canChange`; e o total |
| `POST /api/time-entries` | `AddTimeEntry` | Inclui; 201 com o apontamento |
| `PUT /api/time-entries/{id}` | `ChangeTimeEntry` | Corrige todos os campos, inclusive o projeto |
| `DELETE /api/time-entries/{id}` | `DeleteTimeEntry` | Apaga |
| `GET /api/time-entries/projects` | `ListTimeEntryProjects` | Os projetos em que a pessoa aponta |
| `GET /api/time-entries/projects/{projectId}/work` | `ListTimeEntryWork` | As tarefas e os chamados do projeto |

As horas vão e voltam como número decimal (`1.5`). Os erros de campo usam os nomes `day`, `hours`, `description`, `projectId`, `scheduleItemId` e `ticketId`.

### Tela Minhas horas

Rota `/hours`, no `mainNavigation` entre Chamados e Administração, com o ícone `ScheduleOutlined`, título "Minhas horas" e o `Page` de sempre. A tela segue o padrão das listas (ADR-0066): o `DataTable` com uma linha de grupo por dia (`isGroupRow`, como os times da aba Membros), o dia por extenso e o total do dia na linha de grupo; as colunas Projeto (`CodeChip` e nome), Tarefa ou chamado, Descrição, Horas (à direita) e o "⋮". Acima, a semana ("26 de out. a 1º de nov.") com os botões de semana anterior e seguinte, "Esta semana" e, à direita, "Apontar horas"; o total da semana no `subtitle` do `Page`. A semana começa na segunda, e os dias saem do `useDates()` no fuso da instalação, como `DayValue`.

O diálogo (`TimeEntryDialog`, React Hook Form com Zod) tem o dia (`TextField` de data, hoje de início), o projeto (`Select`), a tarefa ou o chamado (`Select` com os grupos Tarefas e Chamados, opcional, recarregado ao trocar o projeto), as horas (texto que aceita "1,5", "1.5" e "1:30", convertido por `parseHours` em `hours.ts`, que também formata "1,5 h") e a descrição (`TextArea`). Apagar pede confirmação pelo `Dialog`. Depois de cada mudança, a tela invalida `queryKeys.timeEntries`.

A tela segue o canvas "Horas do projeto" (https://claude.ai/artifact/4w44gN26rJ2XMec6pUga3y), aprovado por Marcelo em 29/10/2026: os dias do mais recente para o mais antigo, "Só o projeto" quando o apontamento não tem tarefa nem chamado, a próxima semana desabilitada na semana atual e, no celular, um cartão por dia. As capturas de computador e celular, claro e escuro, vão no PR.

### Demonstração

O plano ganha `hours?: DemoTimeEntry[]` por projeto (`by`, dias atrás em dias úteis, horas, descrição e, opcionalmente, a tarefa pelo nome ou o chamado pelo título), carregadas pela API por quem apontou, nas duas últimas semanas, sem passar de 24 horas num dia.

## Risks / Trade-offs

- [O módulo novo repete a busca de "hoje" no fuso da instalação] → São duas linhas sobre o `InstallationInfo.DayOf`, como no `MyWork` e no `Audit`; a regra continua no `Administration.Contracts`.
- [Uma tarefa excluída deixa o apontamento apontando para nada] → A lista e a auditoria mostram a tarefa como excluída, e as horas continuam no projeto, que é o que conta no saldo.
- [Sem aprovação, uma hora errada fica até alguém corrigir] → A pessoa e o administrador corrigem, e a auditoria mostra quem mudou o quê; a aprovação fica para depois (ADR-0128).
- [A barra lateral ganha um item que o canvas não tem] → É o único item novo da estrutura, no mesmo desenho dos outros; vai nas capturas do PR para Marcelo aprovar.

## Migration Plan

A migração cria o schema `timetracking` e a tabela, sem mexer nos outros módulos. Voltar a versão deixa a tabela sem uso (as migrações só acrescentam, ADR-0125).
