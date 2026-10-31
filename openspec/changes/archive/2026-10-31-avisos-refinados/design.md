## Context

Hoje são 20 telas e componentes com o próprio `<Snackbar open={message !== undefined} message={message} onClose={() => setMessage(undefined)}>`, e o texto chega a eles por `setMessage` ou por propriedades (`onMessage`, `onDone`, `onSaved`, `notify` do `useProjectOutlet`). O tema (`MuiSnackbar`, ADR-0109) põe todos no alto, no meio, com as cores invertidas (`inverse-bg`, branco no tema escuro). Ao fechar, a tela apaga o texto na hora e a transição do MUI encolhe a caixa vazia. O cartão da pessoa abre o aviso num `Portal` de dentro do `Popover`, e a posição sai da janela transformada. Duas telas recebem a confirmação pelo `state` da navegação (Projetos, depois de excluir; o chamado, depois de abrir).

## Goals / Non-Goals

**Goals:**

- Um aviso, num lugar só, com o visual e o movimento escolhidos, para as telas de hoje e as que vierem.
- O aviso de falha distinguível do de sucesso por ícone e cor, não só pela cor (ADR-0065).

**Non-Goals:**

- Mais de um aviso ao mesmo tempo, avisos que ficam até a pessoa fechar, ou avisos guardados (o sino é que guarda, ADR-0038).

## Decisions

- **Um provedor na raiz.** O `NoticeProvider` fica no `InstallationRoot`, em volta de todas as rotas (com sessão ou sem), e as telas pegam a função pelo `useNotify()`: `notify(texto)` para confirmar, `notify(texto, { tone: 'error' })` para uma falha e `{ action }` para uma ação. Como o aviso não pertence à tela, ele continua quando a tela fecha ou muda: a exclusão do projeto e a abertura do chamado avisam e navegam, sem o `state`. Alternativa descartada: manter um aviso por tela com o texto guardado até o fim da saída, que não resolveria a mudança de página nem o aviso dentro de outra janela.
- **O lugar pela estrutura.** O `AppShell` diz ao provedor que está na tela (`useNoticesInShell`): no computador, o aviso fica na faixa do cabeçalho, no meio da área de conteúdo (`left: calc(50% + var(--sidebar-width) / 2)`); no celular, logo abaixo da barra do topo; fora da estrutura (entrada, página de assinatura), no alto, no meio. O aviso vai por `Portal` ao fim da página, com o `zIndex` do snackbar, longe de qualquer janela transformada.
- **Fila de um.** Os avisos entram numa fila; só o primeiro aparece. Quando chega outro, o da tela sai (a animação de saída) e o seguinte entra. Assim duas confirmações seguidas não se sobrepõem nem trocam de texto no meio.
- **O tempo.** Cada aviso guarda quanto falta: 5 s, 8 s com ação. O ponteiro ou o foco dentro dele param a contagem, e sair dele continua de onde parou. O "Fechar aviso" e a ação encerram na hora.
- **O movimento pela Web Animations API.** O aviso anima com `element.animate` (`play` de `theme/motion.ts`), que diz quando terminou: a saída termina, e só então o aviso sai da fila. No jsdom, sem `animate`, a promessa resolve na hora, e os testes não esperam. As curvas e os tempos ficam em `motion` (entrada `cubic-bezier(0.2, 0, 0, 1)`, saída `cubic-bezier(0.4, 0, 1, 1)`), e `reducesMotion()` troca tudo por um esmaecer de 150 ms. Alternativa: as transições do MUI (`Slide`, `Fade`), que exigiriam manter o componente montado e não dariam o movimento reduzido num lugar só.
- **Uma região viva.** A área do aviso é `role="status"` com `aria-live="polite"`, sempre na página, para o leitor de tela ler cada aviso que entra.
- **As falhas.** Os textos de falha (`*.failed`, `*Failed`, a recusa do servidor) passam `tone: 'error'`. As propriedades que levam avisos de falha passam a ter o tipo `Notify`.
- **Sem o `Snackbar` do MUI.** Sai o `MuiSnackbar` do tema, e uma regra do lint (`no-restricted-imports`) reprova `@mui/material/Snackbar` fora do aviso.

## Risks / Trade-offs

- [Mudança em muitas telas] → mecânica: trocar `setMessage` por `notify`, tirar o `Snackbar` e marcar as falhas; os testes das telas continuam procurando o texto do aviso.
- [Um aviso que dependia do `state` da navegação, aberto de novo pelo endereço, não aparece de novo] → é o esperado: a confirmação é do momento, não da página.
- [O aviso da tela anterior aparece por cima da página seguinte] → é o desejado: confirma o que acabou de acontecer.
