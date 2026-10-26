## Why

Com a entrada pelo Entra, o ambiente de demonstração passou a ter duas Marina Costa: a da demonstração e a do tenant. Na lista do "@", as duas aparecem só com o nome, e não dá para saber qual delas será mencionada. Marcelo pediu o e-mail embaixo do nome.

## What Changes

- Nas duas listas de menção do editor dos chamados (ao digitar "@" e no botão "@"), cada pessoa aparece com o nome e, embaixo, o e-mail, menor e em cinza.
- A API do chamado passa o e-mail de cada pessoa do chamado (`participants[].email`), vazio para quem foi anonimizado.

## Non-goals

- Procurar pelo e-mail na lista: a busca continua pelo começo do nome ou de uma palavra dele (ADR-0107).
- O cartão da pessoa ao clicar no avatar, que vem na Fundação-5.4.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `chamados`: a lista de menção mostra o e-mail de cada pessoa.

## Impact

- `src/Modules/Tickets` (as pessoas do chamado com o e-mail, pelo `IUserDirectory.PeopleAsync`), contrato e tipos da API.
- `web/src/components/RichTextEditor.tsx`, `typedMention.ts`, `web/src/tickets/TicketReply.tsx` e os testes.
- Roadmap: Chamados-2.5, a pedido de Marcelo (ADR-0026, ADR-0107).
