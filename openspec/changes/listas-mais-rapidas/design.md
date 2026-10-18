# Design: listas mais rápidas (Acesso-2.1)

## Context

A motivação está em proposal.md, e a escolha da biblioteca no ADR-0080. Hoje cada tela lê a API no próprio `useEffect`, com o resultado num `useState` e uma variável `active` para ignorar respostas antigas. É o que fazem `UsersPage`, `AuditPage`, `AdminLayout` (a contagem) e `PersonFilter`. Nada é guardado entre montagens. O `loading` só vale `true` na primeira carga, então uma troca de filtro não dá nenhum sinal enquanto espera.

Medições feitas no build de produção:

- **Redesenho inútil.** O `DataTable` passa ao Data Grid um `pageSizeOptions={[pageSize]}` novo a cada desenho, além do `sx`, do `paginationModel` e de callbacks recriados. Por isso o Data Grid, que é memoizado, redesenha todas as células a cada mudança da tela. Estabilizar essas propriedades levou o trabalho logo após o clique no filtro de cerca de 60 para 27 ms.
- **Menu lento para sair.** O menu do MUI (`Popover` com `Grow` e `transitionDuration: 'auto'`) leva cerca de 250 ms para sumir, por cima da tabela já atualizada.
- **Esqueleto que pisca.** Ele aparece no primeiro desenho da tabela (`loading` começa `true`) e some 30 a 130 ms depois.

## Goals / Non-Goals

**Goals:**

- Um jeito único de ler a API nas telas, que as próximas changes reaproveitam.
- Nenhum indicador de carregamento para esperas curtas, e um indicador discreto para as longas.
- Nenhum dado de uma sessão visível na seguinte.

**Non-Goals:**

- Escrever na API pelo TanStack Query (`useMutation`). As ações continuam chamando o cliente `api` e, quando dão certo, invalidam as consultas afetadas. `useMutation` pode entrar quando uma tela precisar do estado da ação.
- Pré-carregar telas (`prefetchQuery`) ou dividir o pacote.

## Decisions

### 1. TanStack Query com uma configuração central

`web/src/api/queryClient.ts` cria o `QueryClient`:

- **`staleTime` 0 (padrão).** Toda tela ou filtro revisitado mostra o que está guardado e pede de novo à API. A lista nunca fica velha por mais que uma resposta.
- **`gcTime` padrão, 5 minutos.** Consultas sem tela aberta são descartadas depois desse tempo.
- **`refetchOnWindowFocus` padrão (sim).** Voltar à aba atualiza a lista aberta, sem custo perceptível.
- **Novas tentativas:**
  - Nenhuma para respostas 4xx: 401, 403 e 422 não mudam ao repetir. O 401 já leva à entrada pelo middleware do cliente.
  - Uma para falha de rede ou 5xx.
  - Nos testes, nenhuma.
- **Erros.** As consultas usam um `fetchOrThrow` que lança `ApiError` (status e Problem Details) quando o `openapi-fetch` não traz `data`. É assim que o TanStack Query reconhece o erro.
- **Uma instância por aplicação.** O `App` cria uma na carga do módulo, e o `renderApp` dos testes cria uma nova a cada teste, para que um teste não veja o cache de outro.

**Alternativas:**

- **Hook próprio com um `Map`:** sem dependência, mas teríamos de escrever e testar o que a biblioteca já faz (invalidação, cancelamento, novas tentativas, deduplicação, foco).
- **SWR:** parecido, mas com menos controle de invalidação por prefixo e de dados provisórios.
- **RTK Query:** exige Redux, que o projeto não usa.

### 2. Consultas num arquivo só, com chaves por prefixo

`web/src/api/queries.ts` reúne as consultas com `queryOptions`. Cada função monta a chave, chama o cliente `api` com o `signal` do TanStack Query (cancela a chamada se a tela sai) e devolve os dados:

- `usersQuery(params)` com a chave `['users', 'list', params]`, usada pela tela Usuários e pela contagem da Administração (`pageSize: 1`);
- `userSearchQuery(search)` com a chave `['users', 'search', search]`, usada pelo filtro de pessoa e só pedida com o menu aberto (`enabled`);
- `auditQuery(params)` com a chave `['audit', 'list', params]`.

Os parâmetros entram na chave já normalizados: busca sem espaços, `undefined` para "todos", o período em ISO. Assim, o mesmo filtro dá a mesma chave. Depois de trocar o papel, desativar ou reativar, a tela Usuários invalida `['users']` e `['audit']`. Isso refaz a lista, a contagem e a auditoria, e as que não estão abertas são refeitas quando voltarem à tela. Duas partes da tela que peçam a mesma chave ao mesmo tempo geram um pedido só. Isso também acaba com o pedido em dobro do `StrictMode` no desenvolvimento.

As listas usam `placeholderData: keepPreviousData`. Ao trocar o filtro ou a página para uma chave sem dados guardados, a tabela continua com as linhas anteriores até a resposta. Uma chave já vista mostra os dados dela na hora.

### 3. Cache descartado quando a sessão termina

O `QueryClientProvider` fica por fora do `SessionProvider`. Toda vez que a sessão passa a `signed-out`, o `SessionProvider` chama `queryClient.clear()` (pelo `useQueryClient`), seja ao sair, seja por um 401 da API. O cache existe só na memória da página: o TanStack Query não grava nada no navegador sem um *persister*, e não usamos nenhum. Isso fica dentro do ADR-0054 (no navegador, só o cookie `HttpOnly`) e do cuidado com dados pessoais (ADR-0058).

### 4. Indicador de carregamento com espera de 300 ms

O `DataTable` recebe `loading` e troca o slot `loadingOverlay` do Data Grid por um `DelayedLoadingOverlay`:

- Ele não desenha nada nos primeiros 300 ms (`loadingDelay`) e depois desenha o `GridLoadingOverlay` do próprio Data Grid, com fundo transparente.
- Com linhas na tabela, a variante é a barra (`linear-progress`) no topo, sem esconder as linhas.
- Sem linhas, a variante é o esqueleto (`skeleton`). Durante a espera, o Data Grid já esconde o corpo com essa variante, então aparece só o cabeçalho, nunca a mensagem de lista vazia.

O timer começa quando o overlay monta, isto é, quando `loading` passa a `true`, e é desfeito quando ele sai.

Cada tela passa `loading = isFetching && (data === undefined || isPlaceholderData)`. A espera mostra o indicador em dois casos: na primeira carga e quando a tabela está com as linhas de outro filtro. A atualização em segundo plano de dados guardados para a mesma chave não mostra indicador nenhum.

Alternativa: mostrar o indicador sempre que houver pedido em andamento. Foi descartada porque é exatamente o pisca que se quer tirar.

### 5. Menus sem animação de saída

No tema, `MuiMenu.defaultProps.transitionDuration` passa a `{ enter: duration.enteringScreen, exit: 0 }`, com o `duration` do MUI (225 ms na entrada, próximo dos cerca de 250 ms do `'auto'` atual nesses menus). O `Select` usa o `Menu` do MUI, então o filtro e o menu de ações da tela Usuários mudam juntos. O `PeriodFilter` (`Popover` com botões) e o `Autocomplete` (`Popper` sem animação) ficam como estão.

Alternativa: só encurtar a saída, por exemplo para 100 ms. Foi descartada porque qualquer saída animada esconde a tabela que acabou de mudar, e o campo de seleção nativo do navegador também fecha na hora.

### 6. Propriedades estáveis no `DataTable`

`pageSizeOptions`, `paginationModel`, `sx`, `onRowClick`, `onCellKeyDown`, `onPaginationModelChange` e `getRowClassName` passam a ser memoizados no `DataTable`. Nas telas:

- `isRowMuted` vira uma função fora do componente;
- `displayedRowsLabel` e `columns` continuam memoizados.

Um teste do `DataTable` conta os desenhos de um `renderCell`: desenhar a tela de novo com as mesmas linhas não pode desenhar as células de novo.

### 7. Item Administração continua em `/admin`

O redirecionamento de `/admin` para `/admin/users` custa um desenho a mais só na primeira entrada. Levar o item direto para `/admin/users` exigiria outra regra para marcá-lo nas demais páginas da Administração, e a trilha também aponta para `/admin`. Fica como está.

## Risks / Trade-offs

- **[Dado guardado diferente do atual por um instante]** Ao voltar a uma lista, ela mostra o que estava guardado até a nova resposta, normalmente dezenas de milissegundos. → A atualização é imediata. As ações da tela usam o id da pessoa, e a API valida cada uma (último administrador, por exemplo). O resultado de uma ação invalida as listas.
- **[Agir sobre linhas do filtro anterior durante a troca]** Com `keepPreviousData`, a tabela mostra as linhas anteriores enquanto chega a resposta. → A ação vale para a pessoa da linha, que existe e é validada pela API. Em seguida, a lista se atualiza.
- **[Dados de uma pessoa vistos por outra no mesmo navegador]** → O cache é descartado em toda passagem para `signed-out` e existe só na memória. Um teste cobre a saída e a entrada de outra pessoa.
- **[Testes com cache compartilhado]** → Um `QueryClient` novo por `renderApp`, sem novas tentativas.
- **[Pacote maior]** São cerca de 13 kB a mais, comprimidos. → É aceitável diante do código que substitui e do que evita escrever nas próximas telas.

## Migration Plan

Nada para migrar: a mudança é só do frontend, sem dados nem API. Para voltar atrás, basta reverter o PR.
