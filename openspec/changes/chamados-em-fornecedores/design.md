# Design: chamados em fornecedores (Chamados-6)

## Context

- **ADR-0028:** registro manual, com destaque para o que está parado.
- **ADR-0044:** registrar e atualizar no fornecedor são ações de quem atende.
- **ADR-0046:** os cadastros da Administração são só do administrador.
- **O que já existe:**
  - `TicketActors.Attendant`;
  - os eventos da conversa;
  - o calendário da instalação (`TicketDay`);
  - as telas de cadastro de Clientes e de Modelos de projeto.
- **Canvas:**
  - a seção "No fornecedor" do painel do Chamado (fornecedor, número, `StatusBadge` do tipo vendor, `DueDate` "Sem retorno do fornecedor há 6 dias úteis" e "Atualizar");
  - a visão "Com fornecedor";
  - o evento "registrou o chamado FX-20931 no fornecedor Emissor Exemplo".

## Goals / Non-Goals

**Goals:**
- cadastro de fornecedores;
- registros no chamado, com status, eventos e destaque do que está parado;
- a visão "Com fornecedor".

**Non-Goals:**
- pausa do nosso prazo pelo fornecedor;
- integração automática;
- a trilha de etapas.

## Decisions

### 1. Tabelas no módulo Tickets

- **`vendors`:**
  - campos: nome (único, até 150), portal (até 500, http ou https) e situação;
  - fica na auditoria, com o nome como rótulo;
  - não se apaga, só se desativa, porque os registros guardam o fornecedor.
- **`vendor_tickets`:**
  - campos: o chamado, o fornecedor, o número (até 60), a data de abertura, o status, o link (até 500, http ou https), a observação (até 1.000), quem registrou, quando e a última atualização;
  - fica fora da auditoria, como a conversa, porque cada registro e cada atualização já viram eventos.
- **Por que no Tickets:** os fornecedores só servem aos chamados.

### 2. Status do lado do fornecedor

- **Valores:** `VendorTicketStatus` tem `Open`, `InAnalysis`, `WaitingForUs`, `Resolved` e `Closed`, com os textos e ícones do design system (vendor: aberto, em_analise, aguardando_retorno, resolvido, fechado).
- **Ciclo:** não há fluxo, porque o fornecedor decide o próprio ciclo. Qualquer status vai para qualquer outro.

### 3. Eventos

- **`VendorTicketRegistered`:** "registrou o chamado X no fornecedor Y".
- **`VendorStatusChanged`:** "atualizou o chamado X no Y · Em análise → Aguardando nosso retorno". A anotação aparece na linha de baixo.
- **Campos novos em `TicketEvent`:** `VendorTicketId`, `FromVendorStatus`, `ToVendorStatus` e `Note`. O nome do fornecedor e o número saem do registro, lido junto com o chamado.

### 4. Parado

- **Regra:** um registro em `Open`, `InAnalysis` ou `WaitingForUs` cujo dia da última atualização tem `Tickets:VendorStaleBusinessDays` (5) dias úteis ou mais até hoje fica parado, com `staleDays`. Ou seja, `CountBusinessDays(dia + 1, hoje) >= N`.
- **Onde:** a regra fica num lugar só (`VendorTickets.StaleDaysOf`), com o `TicketDay` dos prazos.

### 5. API

- **Administração:**
  - `GET /api/admin/vendors` traz todos;
  - `POST` inclui, e `PUT /{id}` muda nome, portal e situação;
  - nome repetido dá 409 (`vendor-name-in-use`).
- **Opções:** `GET /api/tickets/vendor-choices` traz os ativos, para qualquer pessoa com sessão.
- **Chamado:**
  - `POST /api/tickets/{number}/vendor-tickets` registra, e `PUT .../{id}` atualiza o status com a anotação;
  - quem atende pode (`TicketActors.Attendant`), com o chamado não fechado.
- **Leitura:**
  - o `GET /api/tickets/{number}` traz `vendorTickets` e `permissions.canRegisterVendorTicket`;
  - a lista traz `vendor` (o registro em aberto mais recente, ou o último) e a visão `WithVendor`, com a contagem.

### 6. Tela

- **Painel do chamado:**
  - a seção "No fornecedor", depois de Prazos, com cada registro;
  - "Registrar" no cabeçalho da seção e "Atualizar" em cada registro, só para quem atende;
  - os diálogos são formulários RHF com Zod.
- **Lista:** "· no {fornecedor}" na linha de baixo do título.
- **Administração:** "Fornecedores", em Cadastros, desenhada como "Modelos de projeto" (tabela com nome, portal, situação e "⋮" com editar, desativar e reativar), com "Novo fornecedor".

## Risks / Trade-offs

- **[Status desatualizado]** O registro é manual. Mitigação: o destaque do que está parado (ADR-0028).
