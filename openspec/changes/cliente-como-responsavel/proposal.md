## Why

Marcelo perguntou se o cliente pode ser responsável de tarefas do cronograma, como uma fase de testes do cliente. Hoje o responsável é sempre uma pessoa do projeto (ADR-0020, ADR-0043), e as etapas do cliente ficam sem responsável ou em nome de alguém da equipe (Cronograma-2.1, a pedido de Marcelo).

## What Changes

- ADR-0102: o cliente do projeto como opção do responsável de uma tarefa, sem pessoa e sem acesso novo.
- Domínio: a tarefa tem o cliente como responsável (`OwnedByClient`), nunca junto de uma pessoa nem num marco, e só num projeto com cliente; o progresso dela é de quem edita o cronograma; entra no desfazer e na auditoria.
- API: `ownedByClient` na inclusão, na alteração e na leitura das tarefas; a exportação para o MS Project traz o cliente como recurso.
- Tela: "{cliente} (cliente)" na lista do responsável; a Lista, o detalhe e a Linha do tempo mostram o cliente; a ligação de duas barras mantém o cliente.
- Demonstração: "Testes com a equipe do cliente" do ERP-FIN fica com o cliente.

## Capabilities

### New Capabilities

### Modified Capabilities
- `cronograma`: o cliente como responsável de uma tarefa.

## Impact

- `Projects` (domínio do cronograma, histórico, caso de uso, rotas, exportação e a migração `ScheduleItemOwnedByClient`), telas do cronograma, auditoria, demonstração e testes.
