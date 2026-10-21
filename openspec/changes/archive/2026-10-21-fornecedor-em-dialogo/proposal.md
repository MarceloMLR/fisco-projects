## Why

A seção "No fornecedor" do painel do chamado mostra tudo de cada registro (link, observação, data de abertura e "Atualizar"), e com dois ou três registros o painel fica longo e difícil de ler. Marcelo pediu que o painel mostre só o essencial e que o resto, com o histórico das mudanças de status, apareça ao escolher o registro (Chamados-6.1, a pedido de Marcelo, ADR-0028).

## What Changes

- No painel, cada registro fica numa linha compacta: o fornecedor, o número e o status (ícone, cor e texto), com o destaque de parado quando houver. A linha inteira é um botão que abre o registro.
- O diálogo do registro mostra o fornecedor e o número, o status, o link para o chamado no fornecedor, a data de abertura no fornecedor, quem registrou e quando, a última atualização, a observação e o histórico: o registro e cada mudança de status, com quem fez, quando e a anotação.
- "Atualizar" sai do painel e fica no diálogo, para quem atende o chamado; "Registrar" continua no painel.
- Nada muda na API: o histórico sai dos eventos do chamado que já vêm com ele.

## Capabilities

### New Capabilities

### Modified Capabilities
- `chamados`: "Fornecedores pela tela" passa a ter o registro compacto no painel e o diálogo do registro.

## Impact

- `web/src/tickets/TicketVendors.tsx` (a seção e o diálogo novo), `web/src/locales/pt-BR.json` e os testes de tela.
- Sem mudança no backend nem no contrato da API.
