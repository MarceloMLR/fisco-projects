# Design: trocas e transferências (Chamados-5)

## Context

- **O que já existe:**
  - o responsável nasce da primeira resposta (`TicketAnswers`, Chamados-3);
  - quem faz cada ação sai de `TicketAccess`;
  - os eventos da conversa (`TicketEvent`) já têm `AssigneeSet` e `PriorityChanged`;
  - a aba Membros (Projects) remove e troca papéis numa transação serializável e já deixa as tarefas sem responsável.
- **ADRs:** ADR-0068 (quem troca o responsável e quem pode ser), ADR-0027 (solicitante e lote), ADR-0044 (transferência e lote) e ADR-0042 (pendências de quem sai).

## Goals / Non-Goals

**Goals:**
- trocar o responsável e o solicitante, um a um e em lote;
- transferir de time;
- chamados sem responsável quando a pessoa não pode mais atender;
- pendências no detalhe da pessoa.

**Non-Goals:**
- avisos por e-mail;
- ausência com substituto;
- passar as tarefas do cronograma, que já ficam sem responsável pela Projetos-4.

## Decisions

### 1. Regras em `TicketAccess`

- **Trocar o responsável:** `MayAssign`, para o administrador e os gestores (ADR-0068, que substitui a coluna "quem atende" do ADR-0044).
- **Quem pode ser responsável:** `CanBeAssignee(team, role)`: quem atende o time (`Serves`) ou o gerente.
- **Trocar o solicitante:**
  - `MayChangeRequester`: o administrador, os gestores e o próprio solicitante;
  - em lote, só o administrador e os gestores (`MayChangeInBulk`).
- **Quem pode ser solicitante:** `CanBeRequester(role)`: quem abre chamados, ou seja, todos menos o leitor.
- **Transferir:** `MayTransfer`, para quem atende o chamado (`TicketActors.Attendant`: o administrador, os gestores e quem atende o time).

### 2. API em lote

- **Endpoints:**
  - `PUT /api/tickets/assignee` recebe `{ numbers, assigneeId }`, e `PUT /api/tickets/requester`, `{ numbers, requesterId }`.
  - Um chamado só vai com um número, e a página do chamado usa as mesmas rotas.
- **Tudo ou nada:**
  - os chamados são conferidos antes de gravar, numa gravação só;
  - um chamado que a pessoa não vê dá 404; sem permissão, 403; um responsável ou solicitante que não serve para algum deles, 400 com os números; um chamado Resolvido ou Fechado, 409.
  - O token de concorrência da Chamados-4 protege a gravação.
- **Opções:** `GET /api/tickets/assignee-choices?number=…` e `requester-choices` devolvem as pessoas que servem para todos os números. A regra fica no servidor, e a tela só mostra.
- **Limite:** até 100 chamados por vez, como o tamanho máximo da página da lista.

### 3. Transferir (`PUT /api/tickets/{number}/team`)

- **Regra:** o time novo precisa ter alguém para atender (`TeamsServedBy`), e o chamado fica sem responsável.
- **Prazos e status:** não mudam. A primeira resposta já cumprida continua cumprida.
- **Evento:** `TeamChanged`, com o time anterior e o novo.

### 4. Eventos

- **Novos tipos:**
  - `AssigneeSet` por outra pessoa, que já existe: "definiu o responsável · X";
  - `AssigneeCleared`: "deixou sem responsável · X saiu do projeto ou do time";
  - `RequesterChanged`: "trocou o solicitante · A → B";
  - `TeamChanged`: "transferiu · Desenvolvimento → Funcional".
- **Campos novos em `TicketEvent`:** `FromTeam`, `ToTeam`, `FromPersonId` e `ToPersonId`. O `AssigneeId` continua para o `AssigneeSet`.

### 5. Quem sai ou muda de papel: `IProjectMemberChanges`

- **O contrato:** fica no `Projects.Contracts`, com `MemberChangedAsync(projectId, userId, newRole?, actorId)`. O `Projects` chama as implementações registradas depois de gravar a remoção ou a troca de papel.
- **A implementação do Tickets:** deixa sem responsável os chamados em aberto do projeto em que a pessoa é responsável e que o novo papel não atende (`CanBeAssignee`), com o evento `AssigneeCleared`.
- **Por que depois de gravar:** cada módulo tem o próprio contexto e o próprio banco lógico, e uma transação entre os dois exigiria transação distribuída. O passo do Tickets é idempotente.
- **Se ele falhar:**
  - a falha vai para o registro da aplicação e a remoção continua valendo;
  - o chamado mostra o antigo responsável até alguém trocá-lo pela tela.

### 6. Tela

- **Lista (`TicketTable`):**
  - coluna de seleção, só nas linhas em que a pessoa pode trocar algo (`canChangeAssignee` ou `canChangeRequester` em cada item);
  - barra "N selecionados" acima da tabela, como no canvas.
  - "Atribuir a…" fica habilitado só se todos os marcados permitem; idem "Trocar solicitante".
  - O link "Atribuir" embaixo de "Sem responsável" abre o mesmo diálogo para um chamado.
- **Diálogo de pessoa:** um `Select` com as opções da API, porque a lista de pessoas de um projeto é curta.
- **Página do chamado:** "Trocar responsável", "Trocar solicitante" e "Transferir para Funcional ou Desenvolvimento" no "⋮", pelas permissões do chamado (`canChangeAssignee`, `canChangeRequester`, `canTransfer`).
- **Aba Membros:**
  - o detalhe da pessoa ganha a seção "Chamados" (lista com `personId`), com o selo da parte de cada um;
  - ganha também "Passar pendências", com um diálogo para o novo responsável e o novo solicitante.
- **Filtro por pessoa:** não entra na barra da lista, porque o canvas não tem esse campo. A API o tem, e a aba Membros o usa.

## Risks / Trade-offs

- **[Falha depois da remoção]** O chamado pode manter um responsável que saiu. Mitigação:
  - o passo é idempotente e simples;
  - a falha fica no registro;
  - a pessoa que saiu não vê mais o projeto.
- **[Lote entre projetos]** As opções podem vir vazias se não houver ninguém comum. Mitigação: a tela diz que não há uma pessoa que sirva para todos.
