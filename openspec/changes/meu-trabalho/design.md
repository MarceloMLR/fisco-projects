# Design: Meu trabalho (Síntese-1)

## Context

- **ADR-0070:** a fila única por urgência (chamados, tarefas e termos de aceite), os grupos, uma ação por linha, as visões, Meus projetos e Próximos marcos, sem atalhos.
- **ADR-0010:** só itens da pessoa e dos projetos que ela vê; seções vazias com mensagem.
- **ADR-0071:** a faixa de fases nas listas de projetos.
- **ADR-0096 (novo):** a fila montada no servidor pelo módulo `MyWork`; o que entra em cada visão; Próximos 7 dias corridos; prazo depois dos 7 dias fora da fila; o atraso do projeto pelo item mais atrasado.
- **O que já existe:**
  - `TicketAccess` (quem atende, `TeamsOf`, `MayAssign`), `TicketDeadlines.FiguresOf` (o prazo de cada chamado hoje) e as visões da lista de chamados;
  - `ScheduleFigures` (a faixa e os números do cronograma), `ScheduleItem.IsLate` e `DaysLate`, `ScheduleFigures.LateDaysOf` e o `IUpcomingTasks`;
  - `AcceptanceAccess`, o `IPendingAcceptances` e o `IProjectAcceptances`;
  - a lista de projetos com `mine` (a barra lateral usa `mine`, ativos, 30 por página);
  - o `ScheduleStrip`, o `DueDate`, o `TicketDue`, o `TicketStatusBadge`, o `AcceptanceStatusBadge`, o `SegmentedControl`, o `PeopleChangeDialog` e o diálogo de progresso do cronograma.
- **Canvas:** Main (Meu trabalho no computador, 1440×900) e CelularMeuTrabalho.

## Goals / Non-Goals

**Goals:**
- a regra de urgência num lugar só, no servidor, testada com fins de semana e feriados;
- as três visões com a contagem, os grupos e a ação de cada linha;
- Meus projetos e Próximos marcos;
- a tela no computador e no celular.

**Non-Goals:**
- indicadores, atividade recente e personalização (ADR-0070);
- itens com prazo depois dos 7 dias (ADR-0096).

## Decisions

### 1. Módulo `MyWork`

- **Projetos:** `FiscoProjects.MyWork` (Domain, Application, Infrastructure e Endpoints) e `FiscoProjects.MyWork.Contracts`, sem banco; os testes de arquitetura o reconhecem pelo nome.
- **Contrato (`MyWork.Contracts`):**
  - `IMyWorkSource.ItemsAsync(MyWorkQuery, ct)`: a pessoa (id e se é administrador), hoje e o último dia da janela;
  - `MyWorkItem`: o tipo (chamado, tarefa ou aceite), o id, o projeto (id e código), o título, as visões em que entra (`MyWorkViews`, flags), o prazo, se está atrasado e os dias úteis de atraso, se aguarda a pessoa e desde quando, e os dados do tipo (`MyWorkTicket`, `MyWorkTask`, `MyWorkAcceptance`);
  - cada fonte aplica as próprias regras de quem vê e quem atende, e devolve só os itens vencidos, os que aguardam a pessoa e os que vencem até o último dia.
- **Regra (`MyWorkQueue`, Domain):**
  - o grupo: aguarda a pessoa → Aguardando você; atrasado → Atrasado; prazo até o próximo dia útil → Hoje e o próximo dia útil; prazo até hoje + 7 → Próximos 7 dias; o resto fica fora;
  - a ação: chamado aguardando a pessoa → Responder; sem responsável com `CanAssign` → Atribuir; outro chamado → Abrir; tarefa em 0% → Iniciar; tarefa em Atrasado ou Hoje → Concluir; outra tarefa → Atualizar; pedido para a pessoa → Responder; pedido feito por ela → Lembrar;
  - a ordem: pelo prazo, depois o tipo, o título e o id; Aguardando você pela espera mais antiga.
- **Hoje:** o dia no fuso da instalação, pelo `InstallationInfo` do `Administration.Contracts` (um método novo, `DayOf`); o próximo dia útil pelo `IWorkCalendar`.
- **Nomes:** o `MyWork` busca os nomes das pessoas (solicitante, responsável, quem pediu, para quem) uma vez, pelo `IUserDirectory`.
- **API:** `GET /api/my-work` → `{ today, nextBusinessDay, views: [{ view, count, groups: [{ group, items }] }] }`; cada item com `kind`, `id`, `project` (id, código), `title`, `dueOn`, `daysLate`, `waitingSince`, `action` e `ticket`, `task` ou `acceptance`.

### 2. Fontes

- **`Tickets` (`TicketWorkSource`):**
  - os chamados abertos (Aberto, Em triagem, Em andamento, Aguardando solicitante) dos projetos que a pessoa vê, de que ela é responsável, solicitante ou seguidora, ou sem responsável nos times dela (`TicketScope`), numa consulta só do `ITicketStore`;
  - as visões: responsável fora de Aguardando solicitante, solicitante em Aguardando solicitante e sem responsável nos times dela → Para mim; solicitante → Abertos por mim; seguidora → Que acompanho;
  - o prazo pelo `TicketDeadlines.FiguresOf`: a primeira resposta enquanto não foi dada, depois a resolução; atrasado e os dias úteis vêm do próprio `TicketDue`;
  - `MyWorkTicket`: o número, o status, o time, o solicitante, o responsável, `CanAssign` (`TicketAccess.MayAssign` pelo papel no projeto, sem o administrador fora do projeto, que não tem times), as interações e qual prazo.
- **`Projects` (`ScheduleWorkSource`):**
  - as tarefas da pessoa, com datas próprias (sem subtarefas), abaixo de 100%, que terminam até o último dia, em projetos não encerrados de que ela participa (ou qualquer um, para o administrador);
  - o atraso pelo `ScheduleItem.IsLate` e `DaysLate`, com o calendário da instalação;
  - `MyWorkTask`: a fase e o progresso; sempre Para mim.
- **`Documents` (`AcceptanceWorkSource`):**
  - os pedidos pendentes para a pessoa (Para mim, aguardando, desde o pedido) e os que ela fez e ainda gerencia (`AcceptanceAccess.Manages`; Para mim e Abertos por mim, pelo dia do marco do `IScheduleItems`), nos projetos abertos que ela vê;
  - `MyWorkAcceptance`: para quem, quem pediu, quando e o marco.

### 3. Projetos

- **Atraso na lista:** `ProjectScheduleSummaryResponse.lateDays`, o maior valor de `ScheduleFigures.LateDaysOf`, com o calendário da instalação; zero sem atraso.
- **Próximos marcos (`ListUpcomingMilestones`):** `GET /api/projects/upcoming-milestones` → `{ today, items: [{ id, name, day, businessDaysLeft, acceptanceRequested, project: { id, code, name } }] }`:
  - os projetos de que a pessoa participa, não encerrados, também para o administrador;
  - os marcos não atingidos de hoje em diante, os 10 primeiros pelo dia e pelo código;
  - os dias úteis que faltam: os dias úteis depois de hoje até o dia do marco (zero no próprio dia);
  - o aceite pedido por um método novo do `IProjectAcceptances` (`RequestedMilestonesAsync`), que o `Documents` implementa.
- **Endereço do detalhe:** a aba Cronograma lê `?item=` para abrir a gaveta e o tira do endereço ao fechar.

### 4. Tela

- **Rota raiz (`MyWorkPage`, em `web/src/my-work`):** o `Page` ganha `subtitle` (a data de hoje por extenso, do `today` da API) e `actions` (o `SegmentedControl` das visões, com a contagem).
- **Fila (`MyWorkQueue`):** os grupos com o ícone (atraso, espera, hoje, calendário), o rótulo ("Hoje e amanhã" ou "Hoje e segunda", pelo próximo dia útil) e a contagem; as linhas como o canvas (fp-qrow): o ícone do tipo (tarefa, chamado, chamado sem responsável, termo), o título como link, o código do projeto e o detalhe, a situação (`TicketStatusBadge` e `AcceptanceStatusBadge`), o prazo (`DueDate`) e a ação.
- **Textos (`myWork.ts`):** o detalhe ("Tarefa · Levantamento · 80%", "Você é responsável · 6 interações", "Diego Torres aguarda sua resposta há 2 h", "Desenvolvimento · ninguém assumiu ainda", "Termo de aceite pedido por Beatriz Lima", "Você pediu a Carla Mendes em 15/10") e o prazo ("Venceu há 1 dia útil", "Vence hoje", "Vence segunda, 26/10", "1ª resposta até segunda", "Prazo pausado", "Pedido em 15/10", "Marco em 03/11").
- **Ações:** Concluir chama `PUT .../progress` com 100; Iniciar e Atualizar abrem o diálogo de progresso do cronograma (o `ProgressDialog`, exportado do `ScheduleItemProgress` com o projeto e a tarefa mínimos); Atribuir abre o `PeopleChangeDialog`; Abrir e Responder levam ao chamado; Responder e Lembrar do termo levam à gaveta do termo (`?term=`). Depois de cada ação, a fila, os projetos e os chamados são invalidados, e a mensagem aparece num `Alert` acima da fila.
- **Links do título:** o chamado (`/tickets/{número}`), a tarefa (`/projects/{id}/schedule?item={id}`) e o termo (`/projects/{id}/acceptances?term={id}`).
- **Meus projetos:** a mesma consulta da barra lateral (`mine`, ativos, 30), com a faixa, "{n}% concluído" e o `DueDate`: "{n} dias úteis de atraso" (vencido), "Começa {dia}" antes do início, "Concluído em {dia}" com tudo a 100% e "No prazo" no resto; "Sem cronograma" sem itens.
- **Próximos marcos:** a caixa da data (fp-datebox), o nome, "Hoje", "Em 1 dia útil" ou "Em {n} dias úteis", " · aceite pedido" e o código do projeto.
- **Altura:** a página não rola; a fila e o cartão Próximos marcos rolam por dentro (ADR-0066).
- **Celular:** as visões, a fila em duas camadas (fp-mrow: o título; o código, o tipo e o prazo) e, embaixo, Meus projetos e Próximos marcos, com a página rolando.

### 5. Demonstração

- O plano ganha tarefas com responsáveis e datas relativas ao dia da carga (atrasada, de hoje, da semana), um chamado aguardando o solicitante e um sem responsável, e usa o pedido de aceite pendente que já existe.

## Risks / Trade-offs

- **[Consultas por visão]** A fila consulta três módulos a cada abertura do Início. Mitigação: cada fonte faz uma consulta, com os filtros no banco, e a janela de 7 dias limita as tarefas.
- **[Prazo longo fora da fila]** Um chamado de prioridade baixa só aparece quando faltam 7 dias. Mitigação: a página Chamados continua com as listas completas (ADR-0096).
- **[Pessoa que saiu do projeto]** A tarefa de quem saiu do projeto não aparece mais na fila dele. Mitigação: o gerente vê a tarefa no cronograma e troca o responsável.
