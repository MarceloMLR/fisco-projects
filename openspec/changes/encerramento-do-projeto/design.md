# Design: encerramento do projeto (Chamados-7)

## Context

- **ADR-0043:** as regras de encerramento (concluir só com chamados fechados, cancelar fecha os abertos, aviso das tarefas incompletas).
- **ADR-0047:** o `Projects` não lê as tabelas do `Tickets`; os módulos conversam pelos contratos.
- **O que já existe:**
  - o `ChangeProjectStatus`, que muda o status sem olhar os chamados;
  - o `IProjectMemberChanges`, contrato do `Projects.Contracts` que o `Tickets` implementa;
  - o `TicketDeadlines`, por onde passa toda mudança de status dos chamados;
  - os diálogos "Concluir projeto" e "Cancelar projeto" do `ProjectLayout`.

## Goals / Non-Goals

**Goals:**
- conclusão impedida com chamado não fechado;
- cancelamento que fecha os chamados abertos, com o motivo na conversa;
- resumo do encerramento para os diálogos.

**Non-Goals:**
- termos de aceite pendentes no aviso (chegam com Documentos-4);
- fechar os chamados pelo fluxo de status da tabela: o fechamento pelo cancelamento é do sistema, de qualquer status.

## Decisions

### 1. Contrato `IProjectClosing`

- **Onde:** no `Projects.Contracts`, implementado pelo `Tickets` (`TicketProjectClosing`), como o `IProjectMemberChanges`.
- **Métodos:**
  - `OpenTicketsAsync(projectId)`: quantos chamados não estão fechados;
  - `CloseOpenTicketsAsync(projectId, actorId)`: fecha os que não estão fechados, numa gravação só, e devolve falso quando alguém mudou um chamado ao mesmo tempo (nada é gravado).
- **Sem o `Tickets`:** o `Projects` recebe a lista dos módulos registrados; sozinho, nos testes dele, não há chamados.

### 2. Ordem no cancelamento

- **Primeiro os chamados, depois o projeto.** Se o projeto não gravar, o gerente repete o cancelamento: os chamados já fechados não mudam, e o projeto fica cancelado.
- **Por que não ao contrário:** um projeto cancelado com a falha dos chamados ficaria com chamados abertos e sem "Cancelar projeto" no menu para repetir.
- **Os dois módulos não gravam numa transação só:** cada módulo tem o próprio contexto (ADR-0047). A repetição segura cobre a falha entre as duas gravações.

### 3. Conclusão

- O `ChangeProjectStatus` pergunta o número de chamados não fechados antes de gravar. Com algum, devolve 409 (`project-has-open-tickets`, com `openTickets`).
- **Janela:** um chamado aberto entre a pergunta e a gravação passa. É raro, e o gerente reabre o projeto para tratá-lo.

### 4. Fechamento de um chamado pelo cancelamento

- **Domínio:** `Ticket.CloseWithProject(now)` fecha de qualquer status, sem passar pela tabela do fluxo; o resolvido guarda quando foi resolvido.
- **Prazos:** `TicketDeadlines.CloseWithProjectAsync` encerra a pausa de Aguardando solicitante antes de fechar, para os dias pausados não crescerem depois.
- **Evento:** `TicketEventKind.ClosedWithProject`, com `FromStatus` e `ToStatus` (Fechado), e quem cancelou. A tela diz "fechou o chamado · projeto cancelado · {de} → Fechado".

### 5. Resumo do encerramento

- `GET /api/projects/{id}/closing` traz `openTickets` e `incompleteTasks` (tarefas sem subtarefas abaixo de 100%).
- Só quem muda o status (`CanChangeStatus`); quem não está no projeto recebe 404.

### 6. Tela

- **Concluir:** o diálogo lê o resumo ao abrir. Com chamados não fechados, mostra um alerta com o número e "Ver chamados" (a aba Chamados) e desativa "Concluir projeto". Com tarefas incompletas, mostra um aviso e deixa concluir.
- **Cancelar:** com chamados abertos, o texto diz quantos serão fechados.
- **Recusa da API** (a janela da decisão 3): o diálogo mostra o alerta com o número que a API devolveu.

## Risks / Trade-offs

- **[Gravações em dois módulos]** Mitigação: a ordem da decisão 2 e o fechamento que se pode repetir.
- **[Chamado aberto durante a conclusão]** Mitigação: a janela é curta, e o projeto pode ser reaberto.
