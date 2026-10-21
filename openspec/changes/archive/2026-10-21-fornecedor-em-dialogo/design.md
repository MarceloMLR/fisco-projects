## Context

O `VendorSection` (`web/src/tickets/TicketVendors.tsx`) lista os registros em fornecedores no painel do chamado com o fornecedor, o número (link quando há), o status, o destaque de parado, a observação em até 3 linhas, a data de abertura e "Atualizar". O chamado já traz os registros (`vendorTickets`) e os eventos (`events`), entre eles `VendorTicketRegistered` e `VendorStatusChanged`, com o registro (`vendorTicketId`), os status de antes e de depois, a anotação, quem fez e quando.

## Goals / Non-Goals

**Goals:**
- Painel enxuto: uma linha por registro.
- Tudo o que se sabe do registro num diálogo, com o histórico.

**Non-Goals:**
- Editar o número, o link ou a observação do registro (continua como na Chamados-6).
- Mudar a API.

## Decisions

- **Linha do painel**: o fornecedor e o status na primeira linha, o número embaixo, em fonte mono, e o destaque de parado quando houver. O status fica porque é o que se acompanha, e o parado porque pede ação (ADR-0028). A linha é um botão com o nome "{número} no {fornecedor}", que abre o diálogo; o link para o fornecedor sai do painel, para o clique não ter dois destinos.
- **Diálogo** (`VendorTicketDialog`, o `Dialog` padrão): título com o fornecedor, subtítulo com o número; os dados numa lista de rótulo e valor (status, "Abrir no fornecedor" quando há link, aberto no fornecedor em, registrado por, última atualização e observação); o histórico em ordem de acontecimento, cada item com a data e a hora, quem fez, a mudança ("Registrado como Em análise", "Em análise → Aguardando nosso retorno" ou "Anotação, sem mudar o status") e a anotação. O registro entra no histórico pelo evento `VendorTicketRegistered`; se ele faltar, pela data do registro.
- **Atualizar**: no rodapé do diálogo, para quem pode registrar (`canRegisterVendorTicket`); abre o diálogo de atualização que já existe, no lugar do diálogo do registro. Depois de salvar, o painel mostra o novo status e a mensagem de sempre.
- **Nomes das pessoas**: pelos participantes do chamado, como a conversa.

## Risks / Trade-offs

- [Um clique a mais para ver a observação] → é o pedido; o status e o parado continuam à vista.
