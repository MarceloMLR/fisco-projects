# Design: cronograma (Cronograma-1)

## Context

- O módulo `Projects` já tem os projetos e as pessoas de cada um, com as mudanças de pessoas numa transação serializável (`IProjectStore.InSerializableTransactionAsync`) e o que cada pessoa pode fazer em `ProjectPermissions` (ADR-0043).
- O calendário da instalação é do módulo `Calendar`. Os outros módulos usam o contrato `IWorkCalendar`, que devolve um `IBusinessDays` com `IsBusinessDay`, `AddBusinessDays` e `CountBusinessDays`.
- O fuso da instalação vem do `IInstallationSettings` do `Administration`.
- O servidor é a fonte da verdade das datas; o Gantt da Cronograma-3 só vai exibir e enviar alterações (ADR-0051). Por isso, todo o cálculo de datas fica no domínio, no backend.
- O `DataTable` só pagina no servidor e o Data Grid gratuito aceita até 100 linhas por página. A aba Membros já pagina na memória, com 100 por página.

## Goals / Non-Goals

**Goals:**

- Um agregado do cronograma por projeto, com as regras de datas, dependências, progresso e atraso num lugar só, reaproveitado pela aba, pela lista de projetos e pelo cabeçalho.
- Mudanças do cronograma consistentes com pedidos ao mesmo tempo, inclusive com a remoção de uma pessoa do projeto.
- Contrato da API pronto para a Linha do tempo da Cronograma-3: datas calculadas e dependências por id.

**Non-Goals:**

- Ordem manual das tarefas, reordenar fases e arrastar: a ordem segue as datas (decisão 4).
- Recalcular os cronogramas quando o calendário muda (decisão 6).

## Decisions

### 1. O cronograma é um agregado próprio, com uma linha raiz por projeto

- `Schedule` é a raiz, na tabela `projects.schedules`, com `Id` igual ao id do projeto; ele não muda o agregado `Project`.
- Fases, itens (tarefas e marcos) e dependências são coleções dele, nas tabelas `schedule_phases`, `schedule_items` e `schedule_dependencies`, cada uma com o `ProjectId`.
- O Entity Framework acompanha inclusões e remoções pelas coleções, como faz com os membros do projeto.
- A raiz é criada na primeira mudança do cronograma (`IScheduleStore.GetOrAddAsync`), dentro da transação serializável. Por isso, a migração não precisa preencher nada para os projetos que já existem.
- A raiz fica fora da auditoria, porque não tem dados próprios.
- Alternativas:
  - Pôr fases e tarefas dentro de `Project`. Toda leitura do projeto carregaria o cronograma inteiro, e um projeto carregado sem ele apagaria em silêncio as regras que dependem dele.
  - Fases e itens sem raiz. O repositório teria de comparar as listas para saber o que incluir e o que remover.
- No SQL Server, só a raiz apaga em cascata. A fase não apaga os itens, e o item não apaga as dependências, porque o SQL Server recusa mais de um caminho de cascata. Quem exclui fases e itens é o domínio, que remove também os itens e as dependências e deixa cada remoção registrada na auditoria.

### 2. Datas guardadas e calculadas no domínio

- O item guarda o início, a duração e o fim.
- O fim é calculado ao incluir ou alterar o item, pelo `IBusinessDays` lido uma vez por pedido: `AddBusinessDays(início, duração − 1)` para a tarefa; para o marco, o fim é o próprio início.
- Guardar o fim permite filtrar e somar no banco (lista de projetos, períodos) sem o calendário, e a Linha do tempo recebe as datas prontas.
- O início passa para o dia útil seguinte quando cai fora de um dia útil.
- O domínio depende só do contrato `IBusinessDays`, que não é técnico (os testes de arquitetura aceitam contratos de outros módulos).

### 3. Dependências: início mais cedo possível e empurrar em ordem topológica

- O primeiro dia possível depois de uma anterior é `AddBusinessDays(fim da anterior, 1)` para uma tarefa, e o fim da anterior para um marco: o marco acontece no fim do dia.
- Ao alterar um item, o início pedido passa para o primeiro dia possível, se for mais cedo, e o fim é recalculado.
- Depois, os itens alcançáveis pelas dependências são percorridos em ordem topológica (Kahn, só dentro do conjunto alcançável). Cada um começa no maior dos primeiros dias possíveis das suas anteriores, se o início dele for mais cedo; senão, fica como está. Assim, antecipar não puxa ninguém.
- A ordem topológica processa cada item uma vez, mesmo com caminhos que se cruzam, e a contagem de "empurrados" é o número de itens cujo início mudou.
- O ciclo é recusado antes de qualquer mudança: uma anterior nova não pode ser alcançável a partir do próprio item.
- Alternativa: recusar com 400 um início antes do permitido. Foi rejeitada, porque o início mais cedo segue a mesma regra das seguintes (ADR-0019), e a Linha do tempo da Cronograma-3, ao arrastar, também precisa encaixar.

### 4. Ordem na Lista

- As fases vêm pela data de início, e as fases sem itens vêm depois, na ordem de criação (`Position`).
- Os itens de cada fase vêm por início, fim e tipo (tarefa antes do marco no mesmo dia), depois por nome.
- Sem ordem manual não há o que reordenar, e a Lista fica igual à Linha do tempo. Uma ordem manual, se for pedida, entra com um campo novo e uma change própria.

### 5. Números do cronograma num lugar só

- `ScheduleFigures` (domínio) calcula, a partir de fases, itens e o dia de hoje:
  - as datas e o progresso de cada fase e do projeto, ponderados pela duração das tarefas, sem os marcos, arredondados ao inteiro mais próximo;
  - as tarefas e as fases atrasadas, e quantas tarefas estão atrasadas;
  - a fase atual (a primeira atrasada; senão, a última que já começou);
  - a data de conclusão, quando todas as tarefas estão a 100%;
  - os marcos.
- A aba, a lista de projetos e o cabeçalho usam o mesmo cálculo. A lista lê só as fases e os itens dos projetos da página, sem acompanhamento, numa consulta por tabela.
- "Hoje" é o dia no fuso da instalação (`InstallationClock`, com `TimeProvider` e `IInstallationSettings`). A API devolve esse dia, para a tela posicionar a faixa sem usar o fuso do navegador.

### 6. O calendário de agora vale para a próxima mudança

- As datas guardadas não são recalculadas quando o administrador muda os dias de trabalho ou os feriados.
- A próxima alteração de um item usa o calendário do momento, e o empurrão segue dali.
- Recalcular todos os cronogramas abertos a cada mudança do calendário pede um evento entre os módulos, a auditoria como "sistema" e um aviso às pessoas. Isso fica para uma change própria, se Marcelo quiser.

### 7. Concorrência e a remoção de pessoas

- Toda mudança do cronograma roda em `InSerializableTransactionAsync`: lê o projeto (permissão, aberto, responsáveis válidos) e o cronograma, aplica e salva. Assim, duas edições ao mesmo tempo não quebram uma cadeia de dependências, e uma remoção de pessoa não deixa um responsável inválido.
- Na disputa, o banco escolhe uma vítima e a execução é repetida do zero.
- `RemoveProjectMember` e `ChangeProjectMemberRole` (para leitor) chamam `Schedule.Unassign(userId)` na mesma transação. As tarefas alteradas entram na auditoria pelo ponto único do contexto.

### 8. Permissões

- `ProjectPermissions` ganha `CanEditSchedule`: o administrador e o gerente (ADR-0043).
- A API devolve `canEditSchedule` com o projeto, já falso com o projeto encerrado, como as outras permissões.
- Na Cronograma-2, o responsável vai atualizar o progresso das próprias tarefas por outra ação; aqui, o progresso é parte da edição do item.

### 9. API

Rotas sob `/api/projects/{id}/schedule`:

- `GET /schedule` devolve: hoje, início, fim, progresso, tarefas atrasadas e as fases. Cada fase traz datas, progresso, atraso e itens; cada item traz tipo, nome, início, fim, duração, progresso, conclusão, atraso, responsável e ids das anteriores.
- `POST /schedule/phases`, `PUT /schedule/phases/{phaseId}` e `DELETE /schedule/phases/{phaseId}`.
- `POST /schedule/items` devolve 201 com o id.
- `PUT /schedule/items/{itemId}` devolve 200 com `pushed`. O tipo não muda.
- `DELETE /schedule/items/{itemId}`.
- Erros:
  - 400 com os campos: `name`, `phaseId`, `start`, `duration`, `progress`, `ownerId`, `predecessorIds`;
  - 400 do tipo `urn:fiscoprojects:problem:schedule-cycle` para o ciclo, que a tela explica no campo "Depende de";
  - 403 e 404 como nas pessoas;
  - 409 `project-closed`.
- A lista de projetos ganha `schedule` em cada item (nulo sem itens) e `today` na página. O projeto ganha `scheduleStart`, `scheduleEnd` e `permissions.canEditSchedule`.
- Limites técnicos: duração de 1 a 999 dias úteis e datas de 2000 a 2099, para o cálculo nunca andar sem fim.

### 10. Auditoria

| Tipo | Rótulo | Referências | Contexto |
| --- | --- | --- | --- |
| `SchedulePhase` | nome | — | `ProjectId` |
| `ScheduleItem` | nome | `PhaseId` (fase) e `OwnerId` (`User`) | `ProjectId` e `Kind`: o título diz "tarefa" ou "marco" em toda mudança |
| `ScheduleDependency` | a tarefa seguinte, por referência | `PredecessorId` e `SuccessorId` | `ProjectId` |

- `Position` fica fora.
- As fontes de nomes `SchedulePhase` e `ScheduleItem` ficam no `Projects` (ADR-0084).
- O filtro da Auditoria ganha um tipo para cada um.
- Uma referência a um item excluído fica sem nome, como as outras.

### 11. Frontend

- Rota `projects/:projectId/schedule` (`ProjectSchedulePage`), filha do `ProjectLayout`; a aba "Cronograma" vem entre Visão geral e Membros, sem contador.
- Lista no `DataTable`, paginada na memória com 100 linhas, como Membros:
  - as fases são linhas de grupo (fp-group-row), com as próprias células (sem `colSpan`), e podem ser escolhidas: o `DataTable` ganha `groupRowsChoosable`;
  - os itens ficam recuados abaixo da fase;
  - o marco tem a bandeira (`flag`), como no Gantt do design system;
  - o atraso fica junto do fim, com o ícone de alerta, a cor de perigo e o texto "Atrasada", no padrão do DueDate (fp-due--overdue);
  - o progresso usa o fp-progress.
- Componentes novos em `web/src/components`:
  - `ProgressBar` (fp-progress), com o percentual sempre visível;
  - `ScheduleStrip` (fp-strip), que posiciona as fases e os marcos pela data, em dias corridos do período, e hoje pelo `today` da API;
  - `MultiSelect`, para "Depende de", com o Autocomplete do MUI em modo múltiplo, no visual dos campos fp-field.
- Formulário único do item (`ScheduleItemForm`, React Hook Form com Zod):
  - fica no diálogo "Nova tarefa" e na gaveta;
  - o início é digitado como dd/mm/aaaa (`maskTypedDay` e `parseTypedDay`);
  - o fim não é calculado na tela, para não repetir a regra do calendário: a gaveta mostra o fim salvo e a lista atualiza depois de salvar;
  - o responsável é escolhido entre as pessoas do projeto que não são leitoras, num `Select`.
- Depois de cada mudança, as chaves do projeto (`queryKeys.projects`) são invalidadas: aba, cabeçalho e lista. A mensagem diz "Tarefa salva", com "N itens foram empurrados" quando houver.
- Datas do dia (sem fuso) pelo `formatDayText`; o período do cabeçalho em "05/10 – 13/11/2026", ou com os dois anos quando forem diferentes.

## Risks / Trade-offs

- [Cadeias longas recalculadas a cada mudança] → O cálculo é em memória, sobre um cronograma de centenas de itens, com o calendário lido uma vez. A ordem topológica visita cada item uma vez.
- [Deadlock entre duas edições do mesmo cronograma na transação serializável] → A estratégia de execução repete a vítima, como nas pessoas do projeto. Os testes de concorrência cobrem duas edições ao mesmo tempo.
- [Mudança do calendário não reflete nas datas guardadas] → Fica dito na proposta e no resumo para Marcelo. A próxima mudança de cada item já usa o calendário novo.
- [Paginação de 100 linhas pode separar uma fase dos itens dela] → É raro na v1. A Linha do tempo da Cronograma-3 não pagina.
- [Fim mostrado só depois de salvar] → É aceitável na Lista. A Linha do tempo mostra a barra com o fim calculado pelo servidor.

## Migration Plan

- Migração `Schedule` no schema `projects`, com as quatro tabelas.
- Projetos existentes ficam sem cronograma até a primeira mudança. Não há dados para migrar.
- Reverter é remover a migração, porque nenhuma tabela existente muda.
