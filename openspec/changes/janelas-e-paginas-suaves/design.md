## Context

O `Dialog` de `web/src/components` usa o `Dialog` do MUI, com o esmaecer padrão dele, e muitas telas o montam só enquanto está aberto (`{editing !== undefined && <VendorDialog />}`): ao fechar, a tela o tira de uma vez, sem saída. Outras deixam o diálogo montado com `open={removing !== undefined}` e o texto vindo do mesmo estado, que some antes da saída. A gaveta (`DetailsDrawer`) desliza pelo `Slide` do MUI, mas as telas passam `{chosen !== undefined && <Details />}` como conteúdo, que some ao fechar, e a gaveta desliza vazia. A troca de página só troca o conteúdo. As contagens das abas do projeto vêm de cinco consultas (`ticketCountsQuery`, `documentFoldersQuery`, `linksQuery`, `credentialsQuery`, `acceptancesQuery`) e cada uma aparece quando chega, alargando a aba.

## Goals / Non-Goals

**Goals:**

- O movimento escolhido, num lugar só, para todos os diálogos e gavetas de hoje e os que vierem, sem mudar cada tela.
- O mesmo conteúdo na tela durante a saída.

**Non-Goals:**

- Mudar como as telas abrem e fecham os diálogos.

## Decisions

- **Uma transição própria pela Web Animations API.** O `Dialog` e o `DetailsDrawer` passam ao MUI uma transição própria (`slots.transition`), que cumpre o contrato das transições do MUI (`in`, `appear`, `onEnter`, `onEntered`, `onExit`, `onExited`) e anima o elemento com `play` de `theme/motion.ts`: o diálogo de `scale(0.98) translateY(8px)` e opacidade 0 até o normal em 200 ms, e de volta em 150 ms; a gaveta de `translateX(100%)` em 250 ms e de volta em 200 ms. O fundo esmaece no mesmo tempo (`transitionDuration`). Com "reduzir movimento", os quadros viram só o esmaecer de 150 ms. Alternativas: o `Grow` e o `Slide` do MUI, que não têm a escala de 98% nem o movimento reduzido num lugar só.
- **O conteúdo guardado durante a saída.** Enquanto aberto, o diálogo e a gaveta guardam o título, o conteúdo e as ações; fechando, mostram o último que guardaram até a saída terminar. Assim o texto que vem do estado que fechou o diálogo continua até o fim.
- **A saída de quem é tirado da tela.** Quando a tela desmonta o diálogo ou a gaveta ainda abertos, o componente, antes de sair da página (`useLayoutEffect`), copia o próprio elemento (`cloneNode`) para o fim da página, sem leitura de tela nem clique (`aria-hidden`, `inert`), toca nele a saída e o apaga no fim. Sem animação (jsdom), nada é copiado. Alternativa descartada: trocar o `{x && <Dialog />}` de cada tela por um componente que espera a saída, que mexeria em dezenas de telas e nas que vierem.
- **A troca de página pelo endereço.** O `AppShell` anima a área de conteúdo quando muda o endereço da rota logo abaixo dele (`useMatches`, pelo `id` das rotas), o `ProjectLayout` anima a área da aba quando muda a aba e o `AdminLayout`, a área da seção: assim trocar de aba não mexe no cabeçalho do projeto, trocar de seção não mexe na coluna da Administração, e mudar só a busca do endereço (`?item=`, `?term=`) não anima nada. A animação é a de entrada, em `useLayoutEffect`, antes do primeiro quadro, sem desmontar nada, e não toca na primeira abertura.
- **As contagens juntas.** O `ProjectHeader` mostra o lugar de cada contagem (o fp-count vazio) desde o começo e só põe os números quando todas as consultas responderam (com dados ou com erro), esmaecendo em 150 ms; a contagem de Membros, que já vem com o projeto, entra junto com as outras.

## Risks / Trade-offs

- [Durante os 200 ms da entrada da página, o que tem `position: fixed` dentro do conteúdo é posicionado pela área animada] → é o botão flutuante do celular, que mexe 4px junto com a página.
- [A cópia do diálogo na saída não responde a nada] → dura 150 ms e não recebe clique nem leitura de tela.
- [As contagens esperam a consulta mais lenta] → a escolhida por Marcelo (C2): o lugar guardado não deixa as abas pularem.
