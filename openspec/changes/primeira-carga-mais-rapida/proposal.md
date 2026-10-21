## Why

Marcelo notou uma demora de alguns segundos na primeira vez que abre uma tela com dados. A medição mostrou que a primeira visita depois de a API subir leva de 0,4 s a 1,4 s, e a segunda de 0,16 s a 0,31 s: o custo é a primeira execução das consultas, as leituras em cascata da página do projeto e o código da Linha do tempo (Fundação-8, a pedido de Marcelo).

## What Changes

- ADR-0106: primeira abertura das telas mais rápida.
- API: cada módulo com leituras de tela aquece as principais, com uma amostra dos próprios dados, em segundo plano depois de a API subir (`IWarmUp`, `StartupWarmUp`, `WarmUp:Enabled`).
- Tela: a página do projeto pede a aba junto com o projeto; passar o mouse num projeto ou num chamado das listas, ou num projeto da barra lateral, já pede o que a página vai ler; abrir um projeto no computador já carrega o código da Linha do tempo.

## Capabilities

### New Capabilities

### Modified Capabilities
- `plataforma-api`: o aquecimento depois de subir.
- `estrutura-das-telas`: a primeira abertura das telas.

## Impact

- `SharedKernel` (`IWarmUp`), a API (`StartupWarmUp`), a `Infrastructure` dos módulos Users, Administration, Projects, Tickets, Documents, Credentials e Notifications, os testes da API (`WarmUp:Enabled` desligado), `prefetch.ts`, `timelinePreload.ts`, `ProjectLayout`, `DataTable` (`onRowHover`), `ProjectsPage`, `TicketTable` e `Sidebar`.
