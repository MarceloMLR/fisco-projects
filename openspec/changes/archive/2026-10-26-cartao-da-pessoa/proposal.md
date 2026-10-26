## Why

Com duas pessoas de mesmo nome, como as duas Marina Costa que Marcelo testou, as telas não mostram qual é qual: o avatar só traz as iniciais e o nome. Marcelo pediu que o clique na foto da pessoa abra um cartão com o nome e o e-mail, em todo lugar, antes da M365-2 (Fundação-5.4, a pedido de Marcelo).

## What Changes

- ADR-0118: o cartão da pessoa ao clicar no avatar.
- API: `GET /api/people/{id}` traz o nome e o e-mail de um usuário, para qualquer pessoa com sessão; do anonimizado, sem e-mail.
- Tela: o avatar de outra pessoa abre o cartão, com o avatar, o nome, o papel naquela tela quando ela o conhece, o e-mail e "Copiar e-mail", na conversa e no painel do chamado, na lista de chamados, em Membros, na Visão geral, no cronograma, em Projetos, em Usuários e na Auditoria.

## Capabilities

### New Capabilities

### Modified Capabilities
- `usuarios-e-papeis`: os dados de uma pessoa pelo identificador.
- `estrutura-das-telas`: o cartão da pessoa.

## Impact

- `Users` (caso de uso, rota e testes), contrato e tipos do frontend, `web/src/components` (o cartão) e as telas com avatar, com os testes.
