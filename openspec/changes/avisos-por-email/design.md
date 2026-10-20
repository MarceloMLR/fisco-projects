# Design: avisos por e-mail (Avisos-2)

## Context

- **ADR-0037:** e-mail na hora só para o que pede ação; o restante num resumo diário, no horário da instalação; quem fez a ação não recebe; todo e-mail leva ao item; o resumo se desliga, os imediatos não; resumo sem novidades não sai.
- **ADR-0038:** as regras de quem recebe não conhecem os canais (e-mail agora, sino na Avisos-3, Teams depois).
- **ADR-0036 e Avisos-1:** a fila de e-mails (`EmailQueue`), o layout (`EmailRenderer`) e o envio em segundo plano.
- **ADR-0025 e Chamados-4:** os prazos dos chamados (`TicketDeadlines.FiguresOf`, com o estado Vence em breve, hoje ou no próximo dia útil, e Vencido).
- **ADR-0042 e Chamados-5:** o padrão de avisar outro módulo depois de gravar (`IProjectMemberChanges`), com a falha registrada no log sem desfazer a ação.
- **Canvas Perfil:** o cartão "Avisos por e-mail", com "Resumo diário" ("Receber às 07:30, com o que vence no dia") e "Na hora". **Canvas AdminConfiguracoes:** "Horário do resumo diário" nas Regras gerais.

## Goals / Non-Goals

**Goals:**
- contrato dos avisos e regras de quem recebe num lugar só;
- os eventos dos chamados e dos projetos da tabela do ADR-0037;
- alertas de prazo dos chamados;
- resumo diário, com as tarefas que vencem;
- o horário na instalação e a preferência no perfil.

**Non-Goals:**
- o sino (Avisos-3);
- pedidos de aceite e aceites pendentes (Documentos-5, quando os termos existirem);
- juntar vários eventos do mesmo chamado num e-mail e escolher evento por evento (ADR-0037, depois).

## Decisions

### 1. Contrato dos avisos

- `Notifications.Contracts`: `INoticeBoard.PostAsync(Notice)`, com `Notice(Kind, Recipients, ActorId, ProjectId, Path, Values, Key)`.
- **Tipo** (`NoticeKind`): um por evento (`TicketOpened`, `TicketMessage`, `TicketMentioned`, `TicketInformationRequested`, `TicketResolved`, `TicketAssigned`, `TicketRequesterChanged`, `TicketTransferred`, `TicketDueSoon`, `TicketOverdue`, `TicketStatusChanged`, `VendorTicketUpdated`, `ProjectMemberAdded`, `ProjectRoleChanged`, `ProjectStatusChanged`, `ScheduleDatesChanged`).
- **Valores:** só dados do item (número, título, código e nome do projeto, time, status, datas); nomes de pessoas nunca vão no aviso: o canal busca o nome atual de quem fez a ação pelo `ActorId` (ADR-0058, ADR-0084).
- **Quando:** os módulos avisam depois de gravar a ação, como o `IProjectMemberChanges`. A implementação nunca lança: uma falha vai para o log e a ação fica (ADR-0036: a ação não depende do aviso).
- **Por quê, e não gravar o aviso na mesma transação:** cada módulo tem o próprio contexto e o aviso é de outro módulo; perder um aviso por uma falha logo depois de gravar é raro e aparece no log, e a alternativa (uma transação entre módulos) acoplaria os módulos.
- Os módulos recebem `IEnumerable<INoticeBoard>`, para os testes de um módulo rodarem sem o `Notifications`.

### 2. Regras de quem recebe

- No `Notifications` (`PostNotice`), antes dos canais: tira quem fez a ação, quem está desativado e, num aviso com projeto, quem não vê mais o projeto (não é do projeto nem administrador, pelos contratos `IProjectAccess` e `IUserAccess`).
- **Um aviso por pessoa por ação:** o módulo que avisa monta os avisos da ação já sem repetir pessoas, do mais específico para o mais geral (no chamado: menção, pedido de informação ou resolução, mensagem e, por último, status no resumo).

### 3. Canais

- `INoticeChannel` no `Notifications`; nesta change, só o `EmailChannel`. O sino entra na Avisos-3 como outro canal, com os mesmos avisos.
- **E-mail na hora ou no resumo:** decisão do canal de e-mail (o ADR-0037 é sobre e-mail), pelo tipo: os da lista do ADR vão para a fila na hora; os outros viram itens do resumo.

### 4. Avisos dos chamados

| Ação | Na hora | No resumo |
| --- | --- | --- |
| Abrir | quem atende o time | |
| Mensagem | mencionados (menção); solicitante no pedido de informação e na resolução; quem acompanha (mensagem) | |
| Resolver sem mensagem | solicitante | quem acompanha |
| Outra mudança de status, fechamento automático, fechamento pelo projeto cancelado | | quem acompanha |
| Trocar responsável | o novo responsável | |
| Trocar solicitante | o novo solicitante | |
| Transferir | quem atende o novo time | |
| Fornecedor registrado ou atualizado | | quem acompanha |

- **Quem acompanha:** os seguidores do chamado (ADR-0026): o solicitante, o responsável e os mencionados já seguem.
- **Quem atende o time:** `TicketAccess.Serves` com os papéis do projeto (ADR-0068).
- A troca de prioridade não avisa (fora da tabela do ADR-0037).

### 5. Alertas de prazo

- **Processo:** `TicketDeadlineAlerts`, a cada hora, como o `TicketAutoClose`; usa o `TicketDeadlines.FiguresOf`, a única conta dos prazos.
- **Uma vez por estado:** o chamado guarda, para cada prazo, o último estado avisado e o dia de vencimento de então (`FirstResponseAlert`, `FirstResponseAlertDue`, `ResolutionAlert`, `ResolutionAlertDue`, fora da auditoria). Avisa quando o estado (Vence em breve ou Vencido) passa do avisado, ou quando o dia de vencimento mudou (nova prioridade, retomada depois da pausa).
- **Para quem:** o responsável; sem responsável, quem atende o time.

### 6. Avisos dos projetos

- **Inclusão e troca de papel:** depois de gravar, no `AddProjectMember`, no `ChangeProjectMemberRole` e no `CreateProject` (o gerente e o líder técnico escolhidos, ADR-0072).
- **Status do projeto:** para as pessoas do projeto.
- **Cronograma:** o `ScheduleEditing.RunAsync` já tira a foto antes e depois; os casos de uso que mudam datas (incluir, alterar, reagendar e remover itens, desfazer e refazer) avisam, depois de gravar, os responsáveis das tarefas cujo início ou término mudou. A chave do item (`Key`) faz o resumo guardar só as datas mais recentes de cada tarefa.

### 7. Resumo diário

- **Itens:** `notifications.digest_items` (pessoa, tipo, quem fez, caminho, valores em JSON, chave e data), substituídos pela chave quando ela se repete para a mesma pessoa.
- **Horário:** `DailySummaryAt` das Configurações gerais (padrão 07:30), no fuso da instalação.
- **Processo:** `DailySummaryScheduler`, a cada minuto: passado o horário e sem resumo do dia, grava o dia em `notifications.daily_summaries` (chave primária: só uma instância consegue) e manda. Com a aplicação parada no horário, manda quando ela voltar, no mesmo dia.
- **Conteúdo:** as tarefas da pessoa que vencem de hoje até o segundo dia útil seguinte (contrato `IUpcomingTasks` do `Projects`, tarefas com responsável, sem subtarefas-resumo, abaixo de 100%, em projetos não encerrados) e os itens agrupados em Chamados, Cronograma e Projetos, cada um com link.
- Depois do envio, os itens do resumo são apagados, inclusive os de quem desligou o resumo e de quem não está mais ativo: não se acumulam.

### 8. Horário e preferência

- **Instalação:** `DailySummaryAt` (`TimeOnly`, migração com 07:30) no `Administration`, em `PUT /api/installation` (obrigatório), em `GET /api/installation/rules` e no `InstallationInfo` do contrato; na tela, um campo de hora nas Regras gerais.
- **Pessoa:** `ReceivesDailySummary` (padrão ligado) no `Users`, gravada por `PUT /api/profile/daily-summary`, lida em `GET /api/profile` e no `PersonInfo` do contrato.
- **Meu perfil:** o cartão "Avisos por e-mail" do canvas. O resumo é uma caixa que grava na hora, como o tema. Os avisos na hora aparecem marcados e desabilitados, com o texto "Não podem ser desligados", porque o ADR-0037 diz que não se desligam (no canvas, duas das três caixas estavam habilitadas); os rótulos descrevem o que a v1 manda: menções, respostas e pedidos de informação; chamados novos, transferidos e atribuídos a mim; prazos dos meus chamados vencendo ou vencidos.

### 9. Textos

- Cada tipo tem assunto, parágrafo e botão no `pt-BR.json` dos e-mails; time, status, papel e prazo são traduzidos pelo mesmo arquivo. O resumo tem uma linha por item.

## Risks / Trade-offs

- **Aviso perdido numa falha logo depois de gravar a ação** → raro, fica no log; a ação vale.
- **Primeiro alerta de prazo ao subir:** os chamados já vencidos avisam na primeira rodada → é o comportamento esperado (o prazo venceu).
- **Itens do resumo de quem desligou:** são apagados no horário do resumo, não ficam guardados.
