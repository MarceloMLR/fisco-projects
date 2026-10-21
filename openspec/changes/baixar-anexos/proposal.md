# Proposta: baixar anexos (Chamados-2.1)

## Why

Na conversa do chamado, o anexo abre numa aba nova; imagens, PDF e texto abrem no navegador, e não há como baixá-los direto, nem levar todos os anexos de uma vez. Marcelo pediu um botão para baixar cada anexo e outro para baixar todos.

## What Changes

- **Baixar um anexo**: o endereço do anexo aceita `download=true`, que manda o arquivo como download, com o nome original, mesmo para os tipos que abrem no navegador.
- **Baixar todos**: `GET /api/tickets/{number}/attachments/archive` entrega um `.zip` com todos os anexos do chamado (da descrição e das mensagens), na ordem em que foram enviados, com os nomes repetidos numerados; sem anexos, 404. Só quem vê o chamado baixa (404 para os outros).
- **Na tela**:
  - cada anexo, na conversa e em "Anexos da conversa", ganha o ícone "Baixar", ao lado do nome que continua abrindo o arquivo;
  - "Anexos da conversa" ganha "Baixar todos", quando há anexos.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `chamados`: baixar um anexo e todos de uma vez, pela API e pela tela.

## Impact

- `Tickets`: o caso de uso dos anexos do chamado para o `.zip` e os endpoints, com testes da API.
- Telas: `AttachmentChip` (`TicketConversation.tsx`) e a seção "Anexos da conversa" (`TicketPage.tsx`); textos em `pt-BR.json`.
