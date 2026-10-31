## Why

Em 30/10/2026, Marcelo achou os avisos feios: nem sempre centralizados, maiores do que deviam, brancos no tema escuro, aparecendo e sumindo do nada. No código, cada tela tem o próprio aviso (o `Snackbar` do MUI) e apaga o texto antes da saída, então a caixa encolhe vazia e some; o aviso do cartão da pessoa é posicionado a partir da janela em que está; e as cores invertidas do tema ficam brancas no tema escuro. Marcelo comparou as opções numa página de prévia e, em 31/10/2026, escolheu um aviso só, no alto, com fundo de janela e o ícone, que entra e sai suave (ADR-0133). Fundação-5.6.

## What Changes

- Um aviso só para todas as telas, na estrutura, no alto, no meio da área de conteúdo (ADR-0109), um de cada vez: o novo manda embora o que está na tela e entra depois.
- O visual da janela do design system: fundo de janela, borda, sombra de menu e o ícone de concluído; o aviso de uma falha leva o ícone de erro. Do tamanho do texto, até 420px, nos dois temas.
- Entra descendo 8px e esmaecendo em 200 ms e sai esmaecendo em 150 ms, com o texto; com "reduzir movimento" no sistema, só esmaece, em 150 ms.
- Fica 5 segundos, 8 quando tem ação, parado enquanto o ponteiro ou o foco está nele, com o botão "Fechar aviso".
- A confirmação dada antes de mudar de página (o projeto excluído, o chamado aberto) continua na tela seguinte, sem passar pelo endereço.
- As telas deixam o `Snackbar` próprio e chamam o aviso da estrutura (`useNotify`); o lint reprova o `Snackbar` do MUI fora dele.

## Capabilities

### New Capabilities

- Nenhuma.

### Modified Capabilities

- `estrutura-das-telas`: o aviso único, o visual, o movimento, o tempo, a pausa e o fechar.

## Non-goals

- Os diálogos, as gavetas, a troca de página e as contagens das abas do projeto (Fundação-5.7).
- Mudar os textos dos avisos ou o que cada tela confirma.
- Os alertas dentro das telas e dos diálogos (`Alert`), que continuam onde estão.
- Avisos empilhados: um de cada vez, em fila.

## Impact

- `web/src/notices` (novo): o `NoticeProvider`, o `useNotify` e o aviso.
- `web/src/theme/motion.ts` (novo): as curvas, os tempos e o movimento reduzido, usados também na Fundação-5.7.
- `web/src/installation/InstallationRoot.tsx` e `web/src/shell/AppShell.tsx`: o aviso em volta de todas as telas e o lugar dele na estrutura.
- As 20 telas e componentes com `Snackbar` próprio, os que levam o aviso por propriedade e o tema (sai o `MuiSnackbar`).
- Design system: o guia do Snackbar e "Estados e movimento".
- ADRs: 0109, 0133 (novo).
