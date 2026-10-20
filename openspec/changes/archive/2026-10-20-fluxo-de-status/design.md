# Design: fluxo de status (Chamados-3)

## Context

- **ADR-0024:** o fluxo vem em tabela, com o do MVP como padrão. Não há editor na v1, e o servidor confere a integridade.
- **ADR-0068:** tira o "Assumir". O responsável sai da primeira resposta de quem atende.
- **ADR-0044:** define quem faz cada ação.
- **O que já existe:** a Chamados-2 deixou a conversa pronta para receber eventos.

## Goals / Non-Goals

**Goals:** chamados que andam até Fechado, com o responsável definido sozinho, e o histórico na conversa.

**Non-Goals:** editor de fluxo, prazos e pausa (Chamados-4), troca do responsável e transferência (Chamados-5).

## Decisions

### 1. A tabela `ticket_transitions`

- **Colunas:** status de origem e de destino, ação (código em inglês), quem pode (lista de papéis) e se exige mensagem.
- **Automática:** marca as transições que o sistema faz sozinho, que não viram botão.
- **Fluxo inicial:** nasce pela migração (`HasData`), com ids fixos:

| De | Para | Ação | Quem pode | Mensagem | Automática |
| --- | --- | --- | --- | --- | --- |
| Aberto | Em triagem | `StartTriage` (Iniciar triagem) | quem atende | não | não |
| Aberto | Em andamento | `FirstResponse` (primeira resposta) | quem atende | sim | sim |
| Em triagem | Em andamento | `StartWork` (Iniciar atendimento) | quem atende | não | não |
| Em andamento | Aguardando solicitante | `AskForInformation` (Pedir informações) | quem atende | sim | não |
| Aguardando solicitante | Em andamento | `Reply` (Enviar resposta) | solicitante, quem atende | sim | sim |
| Em andamento | Resolvido | `Resolve` (Marcar como resolvido) | quem atende | não | não |
| Resolvido | Fechado | `ConfirmAndClose` (Confirmar e fechar) | solicitante, gestor, administrador | não | não |
| Resolvido | Fechado | `AutoClose` (fechamento automático) | sistema | não | sim |
| Resolvido | Em andamento | `Reopen` (Reabrir) | solicitante | sim | não |
| Aberto | Fechado | `Cancel` (Cancelar chamado) | solicitante, gestor, administrador | sim | não |
| Em triagem | Fechado | `Cancel` (Cancelar chamado) | solicitante, gestor, administrador | sim | não |

- **Integridade (`TicketFlow.From`):**
  - todo status, menos Fechado, tem saída;
  - nenhuma ação se repete no mesmo status de origem;
  - nada sai de Fechado.
  - Um fluxo quebrado lança uma exceção, que aparece no registro e responde 500.

### 2. Quem pode (`TicketAccess.ActorsOf`)

| Papel no fluxo | Quem |
| --- | --- |
| quem atende | o administrador, os gestores (gerente e líder técnico) e quem atende o time do chamado (ADR-0044: a coluna "Quem atende" da matriz inclui administrador e gestores) |
| solicitante | quem abriu o chamado |
| gestor | o gerente e o líder técnico |
| administrador | o administrador do sistema |
| sistema | o processo em segundo plano |

### 3. Primeira resposta e resposta do solicitante (`SendTicketMessage`)

- **Primeira resposta:**
  - a mensagem de quem atende o time (`Serves`, sem contar os gestores que não estão no time), num chamado sem responsável, torna a pessoa responsável e a faz acompanhar o chamado;
  - num chamado Aberto, aplica a transição `FirstResponse`.
  - O líder técnico conta como quem atende Desenvolvimento, como no ADR-0068.
- **Resposta em Aguardando solicitante:** a mensagem do solicitante ou de quem atende aplica `Reply`.
- **Chamado Fechado:** não recebe mensagens (409).

### 4. Eventos

- **`TicketEvent`:** o chamado, quem fez (nulo para o sistema), o tipo (`StatusChanged` ou `AssigneeSet`), os status de origem e de destino, o responsável e quando. Fica fora da auditoria, como as mensagens.
- **Auditoria:** a mudança do chamado (status e responsável) continua na auditoria, pelo próprio `Ticket`.
- **API:** o `GET /api/tickets/{number}` traz os eventos e as ações que a pessoa pode fazer agora (`actions`).
- **Interações:** a lista conta mensagens e eventos, como o canvas ("23 mensagens e 14 eventos").

### 5. Fechamento automático

- **Caso de uso:** `CloseResolvedTickets` fecha os chamados Resolvidos cujo `ResolvedAt`, mais `Tickets:AutoCloseBusinessDays` (5) dias úteis do calendário, já passou no dia da instalação.
- **Processo em segundo plano:** `TicketAutoClose` (`BackgroundService`, ADR-0047) roda ao subir e a cada hora, com o `TimeProvider` do host. Nos testes, o relógio falso o dispara.

### 6. Tela

- **Cabeçalho:**
  - a ação principal é a primeira, nesta ordem: Marcar como resolvido, Iniciar atendimento, Iniciar triagem, Confirmar e fechar, Reabrir, Pedir informações, Cancelar chamado;
  - as outras ficam no "⋮";
  - a ação que exige mensagem abre um diálogo com o editor.
- **Conversa:** os eventos ficam numa linha centralizada e discreta (`Message.Event`), e o filtro "Tudo / Mensagens / Histórico" fica na faixa "Conversa".
- **Página "Fluxo de chamados":**
  - em `/tickets/flow`, aberta por um link na página Chamados;
  - mostra os status em ordem e a tabela das transições (de, para, ação, quem pode, mensagem e se é automática);
  - não há diagrama, porque a tela não foi desenhada.

## Risks / Trade-offs

- **[Processo em segundo plano em várias instâncias]** Duas instâncias da aplicação fechariam o mesmo chamado. Mitigação: a mudança confere o status na gravação, e uma instância só (ADR-0052).
