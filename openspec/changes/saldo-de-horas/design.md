## Context

A Horas-1 criou o módulo `TimeTracking` (schema `timetracking`) com os apontamentos (`time_entries`), as regras de quem aponta (`TimeEntryAccess`) e a tela Minhas horas. O ADR-0128 já decidiu o resto: só o administrador cadastra as horas contratadas e as adicionais e vê o saldo, só em horas, na tela Horas dos projetos, na seção nova Acompanhamento da Administração, com o detalhe numa gaveta e a planilha, como no canvas "Horas do projeto" aprovado em 29/10/2026 (quadros AdminHoras, AdminProjeto e AdminAdicional).

O `TimeTracking` conhece os projetos pelo `IProjectAccess` (código, nome, encerrado e o papel), que não traz a situação nem o cliente que a lista mostra. Os nomes das pessoas vêm do `IUserDirectory` do `Users.Contracts`. A Administração tem as rotas sob `/api/admin`, com `SystemPolicies.Administrator`, e as telas sob `/admin`, no `AdminLayout`.

## Goals / Non-Goals

**Goals:**

- O saldo e o estado de cada projeto calculados num lugar só, no domínio, usados pela lista, pelo detalhe e pela planilha.
- As telas do canvas, com os componentes que já existem (`DataTable`, `DetailsDrawer`, `Dialog`, `StatusBadge`, `ProgressBar`, `SegmentedControl`).
- O administrador corrigindo os apontamentos de qualquer pessoa pela tela, o que a Horas-1 deixou só na API.

**Non-Goals:**

- Valores em reais, avisos de estouro, aprovação das horas e o gerente vendo o saldo (ADR-0128).
- A Administração no celular, que o canvas não desenha.

## Decisions

### Dados

Duas tabelas no schema `timetracking`, na mesma migração:

- `contracted_hours`: `Id` (GUID v7), `ProjectId` (único), `Hours` (`decimal(7,2)`) e `UpdatedAt`. Tirar as horas contratadas apaga a linha. Uma linha por projeto, e não uma coluna no projeto, porque o `Projects` não conhece as horas (ADR-0047) e a auditoria registra a inclusão, a troca e a retirada como de qualquer item.
- `additional_hours`: `Id`, `ProjectId`, `Day` (`date`), `Hours` (`decimal(7,2)`), `Reason` (até 500), `CreatedBy` (o id de quem incluiu), `CreatedAt` e `UpdatedAt`, com índice em `ProjectId`.

Na auditoria (ADR-0039), as duas têm o projeto como referência e contexto (`IsAuditContext`); o rótulo da adicional é o motivo, o das contratadas é o projeto; `CreatedBy` é referência à pessoa; `CreatedAt` e `UpdatedAt` ficam fora. As duas guardam só ids de pessoa, sem nome nem e-mail: a anonimização não precisa de apagador.

Alternativa descartada: as horas contratadas como um campo do projeto no `Projects`. Daria o número junto com o projeto, mas poria no `Projects` uma informação que só o administrador vê e que o ADR-0128 deixa no módulo das horas.

### Regras no domínio

- `ContractedHours` e `AdditionalHours`, com as validações: contratadas maiores que zero e até 99.999, adicionais de 0,25 a 9.999, ambas em quartos de hora (a mesma regra do `TimeEntryForm`, num método só), o dia da adicional até hoje e o motivo de 1 a 500 caracteres.
- `HoursBalance.Of(contracted, additional, logged, isClosed)` devolve o total, o saldo, o consumo (porcentagem arredondada, sem casas) e o estado (`HoursBalanceState`: `Over`, `NearLimit`, `Within`, `NoContract`). `Over` quando as apontadas passam do total; `NearLimit` quando o consumo chega a 90% e o projeto está em aberto; sem horas contratadas, `NoContract`, mesmo com adicionais. A ordem da lista também sai daqui (`HoursBalance.Order`).

### Contrato dos projetos

`IProjectCatalog` no `Projects.Contracts`, com `AllAsync` e `FindAsync`: `ProjectCard(Id, Code, Name, Status, ClientName)`, com a situação num enum próprio do contrato (`ProjectCardStatus`, com os mesmos nomes do `ProjectStatus` do `Projects`, para a tela usar o mesmo selo). O `Projects` o implementa lendo o projeto e o nome do cliente. A alternativa de ampliar o `ProjectAccess` mexeria em todos os módulos que o usam.

### API

Grupo `/api/admin/project-hours`, com `SystemPolicies.Administrator` e a tag "TimeTracking":

| Rota | Nome | O quê |
| --- | --- | --- |
| `GET /api/admin/project-hours?view&search` | `ListProjectHours` | Os projetos da visão (`open`, padrão, `closed` ou `all`) e da busca, com os números do saldo e o estado, na ordem do `HoursBalance` |
| `GET /api/admin/project-hours/{projectId}` | `GetProjectHours` | O detalhe: os números, o primeiro dia apontado, as adicionais com quem incluiu e as pessoas com o papel e a parte |
| `PUT /api/admin/project-hours/{projectId}/contracted` | `ChangeContractedHours` | Cadastra, troca ou, com `hours: null`, tira as contratadas |
| `POST /api/admin/project-hours/{projectId}/additional` | `AddAdditionalHours` | Inclui uma adicional; 201 com o id |
| `PUT /api/admin/project-hours/{projectId}/additional/{id}` | `ChangeAdditionalHours` | Corrige |
| `DELETE /api/admin/project-hours/{projectId}/additional/{id}` | `DeleteAdditionalHours` | Apaga |

Projeto inexistente, ou a adicional de outro projeto, responde 404. A lista não pagina no servidor: uma instalação tem dezenas de projetos, e a planilha usa todas as linhas da visão. As apontadas de cada projeto saem de uma soma agrupada em `time_entries`.

`GET /api/time-entries` ganha `projectId`, só do administrador (403 para os outros, 404 para projeto inexistente): os apontamentos de todas as pessoas do projeto no período, que pode juntar com `personId`. Cada apontamento passa a trazer `person` (id e nome atual, pelo `IUserDirectory`), também na lista de Minhas horas.

### Telas

- `AdminLayout`: a seção "Acompanhamento", a primeira, com "Horas dos projetos" (`ScheduleOutlined`), em `/admin/project-hours`.
- `ProjectHoursPage` (`web/src/admin`): o `Page` com a meta ("5 projetos em aberto · 1 acima das horas contratadas") e "Baixar planilha"; a busca e o `SegmentedControl` na barra da tabela; o `DataTable` com as colunas do canvas, o saldo pelo `HoursBalanceBadge` e o consumo pelo `ProgressBar` com o tom do estado. A linha escolhida abre a gaveta pelo `?project=` do endereço, com `replace` (ADR-0105).
- `ProjectHoursDrawer`: o `DetailsDrawer` com o cabeçalho do projeto, os quatro números (fp-stat), a barra do consumo com o rótulo, "Horas adicionais" e "Por pessoa" em tabelas simples. "Alterar" abre o `ContractedHoursDialog`; "Incluir horas adicionais" e o "Corrigir" do "⋮" abrem o `AdditionalHoursDialog`; "Apagar" pede confirmação.
- `ProjectEntriesDialog`: o `Dialog` com `size="full"`, aberto por "Ver apontamentos", com o mês (anterior, seguinte e "Este mês"), os apontamentos por dia nas linhas de grupo do `DataTable` (as mesmas do `hoursRows` de Minhas horas), a pessoa em cada linha e o "⋮" de corrigir (o `TimeEntryDialog` da Horas-1) e apagar. Esta tela não está no canvas: o botão "Ver apontamentos" está, e o desenho repete Minhas horas, com a pessoa e o mês.
- As regras de tela (texto do saldo, tom, ícone, meta) ficam em `projectHours.ts`; a planilha, em `projectHoursReport.ts`, pelo gerador de `.xlsx` da auditoria (`web/src/format/xlsx.ts`, ADR-0099).
- O `ProgressBar` ganha `tone` (`success`, padrão, `info`, `warning` e `danger`, como o fp-progress do design system: o `info` usa `--control-selected`) e o rótulo em cima (`caption` e `detail`, como o fp-progress__label).
- A auditoria mostra os tipos novos ("Horas contratadas" e "Horas adicionais") pelo `describeAuditEntry`.

Depois de cada mudança, a tela invalida as horas dos projetos, o detalhe, os apontamentos e a auditoria.

### Exclusão do projeto e aquecimento

O `TimeEntryProjectDeletion` apaga também as contratadas e as adicionais do projeto, na mesma transação. O resumo da exclusão continua contando só as horas apontadas. O `TimeTrackingWarmUp` chama a lista das horas dos projetos.

### Demonstração

O plano ganha, por projeto, `contractedHours` e `additionalHours` (dias úteis atrás, horas e motivo), cadastrados pelo administrador depois das horas apontadas, com números escolhidos sobre as horas do plano para dar os quatro estados nos projetos em aberto.

## Risks / Trade-offs

- [A lista calcula o saldo de todos os projetos a cada pedido] → Uma soma agrupada por projeto e duas leituras pequenas; uma instalação tem dezenas de projetos.
- [Cada apontamento passa a trazer o nome da pessoa] → Uma leitura dos nomes por lista, pelo `IUserDirectory`, como nos outros módulos.
- [O tom `info` do consumo usa `--control-selected`, que não é um token de status] → É o que o design system usa no fp-progress--info; a cor nunca é a única informação, porque o estado vem no selo do saldo.
- [A tela dos apontamentos do projeto não está no canvas] → Repete Minhas horas, já aprovada, e vai nas capturas do PR para Marcelo conferir.

## Migration Plan

A migração cria as duas tabelas no schema `timetracking`, sem mexer nas outras. Voltar a versão deixa as tabelas sem uso (ADR-0125).
