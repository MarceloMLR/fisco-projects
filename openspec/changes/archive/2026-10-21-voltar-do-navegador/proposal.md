## Why

Marcelo notou que às vezes precisa clicar duas vezes em Voltar para sair de uma página. O clique no título de um chamado ou no nome de um projeto, numa tabela, abria a página duas vezes (o link e a linha), a gaveta de um termo deixava duas entradas ao abrir e fechar, e o formulário salvo continuava no histórico (Fundação-5.2, a pedido de Marcelo).

## What Changes

- ADR-0105: o que entra no histórico do navegador.
- Tabela padrão: o clique ou o Enter num link ou botão dentro de uma linha não escolhem a linha também.
- Gaveta do termo de aceite: abrir e fechar trocam o endereço, sem entrar no histórico.
- Novo projeto e Abrir chamado: a página salva ou cancelada troca o formulário no histórico; sair do próprio projeto troca o projeto pela lista.

## Capabilities

### New Capabilities

### Modified Capabilities
- `estrutura-das-telas`: o Voltar do navegador.

## Impact

- `DataTable`, `ProjectAcceptancesPage`, `NewProjectPage`, `OpenTicketPage`, `ProjectMembersPage` e testes.
