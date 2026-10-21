---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0106: Primeira abertura das telas mais rápida

Complementa o ADR-0047 (monolito modular, processos em segundo plano) e o ADR-0080 (leituras da API pelo TanStack Query).

## Contexto e problema

Marcelo notou uma demora de alguns segundos na primeira vez que abre uma tela com dados, mesmo com o `vite preview`. Medimos com os dados de demonstração, em Debug e em Release, logo depois de a API subir e em seguida:

| Tela | 1ª vez | 2ª vez |
| --- | --- | --- |
| Projeto (Visão geral) | 1,4 s | 0,18 s |
| Cronograma | 1,2 s | 0,25 s |
| Projetos | 0,6 s | 0,23 s |
| Chamados | 0,5 s | 0,31 s |
| Chamado | 0,44 s | 0,16 s |

A diferença vem de três lugares: a primeira execução de cada consulta da API (a visão geral leva 0,82 s na primeira vez e 0,10 s depois, por compilar a consulta do EF e o código); as leituras em cascata na página do projeto (a aba só pede os dados depois que o projeto chega); e o código da Linha do tempo (6,6 MB do Syncfusion), carregado na primeira vez que o Cronograma abre. Release ajuda pouco, porque o custo é o do primeiro uso. O que fazer?

## Fatores de decisão

- A primeira visita igual à segunda, também logo depois de a API subir.
- Nada muda nas regras de cada módulo: só leituras, e o mesmo cache das telas (ADR-0080).
- Nenhuma leitura a mais quando os dados já estão na tela.

## Opções consideradas

1. Aquecer a API ao subir, buscar os dados antes na tela e carregar antes o código da Linha do tempo.
2. Modelos e consultas compilados do EF Core (`dbcontext optimize`, consultas pré-compiladas).
3. Só a configuração da produção (Release com ReadyToRun, "Always On", HTTP/2, compressão).

## Resultado da decisão

Opção 1, agora; a 3 entra no deploy de produção (Fundação-7); a 2 fica de fora enquanto as consultas pré-compiladas do EF são experimentais, e o aquecimento já cobre o custo.

- **Aquecimento**: cada módulo com leituras de tela implementa `IWarmUp` (SharedKernel) e roda, com uma amostra dos próprios dados (o gerente do primeiro projeto aberto, quem abriu o último chamado), os casos de uso das telas principais. O `StartupWarmUp` da API roda todos, um depois do outro, cada um num escopo próprio, em segundo plano, depois de a API subir; a API responde enquanto isso, e uma falha só vai para o log. Só leituras: os Documentos usam um projeto que já tem as pastas, para não criá-las. `WarmUp:Enabled` (ligado por padrão) desliga, como nos testes.
- **Leituras antes, na tela**: a página do projeto pede a aba (visão geral, cronograma ou membros) junto com o projeto (`prefetchProject`); passar o mouse num projeto da lista ou da barra lateral, ou num chamado da lista, já pede o que a página vai ler. Só o que o cache não tem é pedido (`queryClient.query` com `staleTime: 'static'`); o que já está nele aparece na hora e é pedido de novo em segundo plano, como antes.
- **Linha do tempo**: abrir um projeto no computador já começa a carregar o código do Gantt (`preloadTimeline`), o mesmo import do `ScheduleTimeline`.

### Consequências

- Boa, porque a primeira visita depois de a API subir fica como a segunda (números no PR).
- Boa, porque a página do projeto não espera o projeto para pedir a aba.
- Neutra, porque a API faz algumas leituras a mais ao subir, uma vez.
- Ruim, porque um caso de uso novo de tela entra no aquecimento à mão; sem isso, ele só fica mais lento na primeira vez.
