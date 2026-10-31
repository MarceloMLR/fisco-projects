## Why

Em 30/10/2026, Marcelo achou bruscos os diálogos, as gavetas, a troca de página e as abas do projeto: o diálogo some de uma vez ao fechar, a gaveta perde o conteúdo antes de deslizar, a página nova aparece do nada e as contagens das abas chegam uma a uma, alargando as abas e empurrando as outras. Comparou as opções numa página de prévia e, em 31/10/2026, escolheu o diálogo que esmaece e cresce, a gaveta que desliza nos dois sentidos, o conteúdo que esmaece subindo na troca de página e as contagens juntas, no lugar já guardado (ADR-0133). Fundação-5.7, a segunda parte do ADR-0133, depois da Fundação-5.6.

## What Changes

- Diálogo: o fundo esmaece e a janela cresce de 98% subindo 8px em 200 ms; ao fechar, o caminho de volta em 150 ms. O diálogo fica na tela, com o mesmo conteúdo, até a saída terminar, também quando a tela o tira de uma vez.
- Gaveta dos detalhes: desliza da direita ao abrir (250 ms) e ao fechar (200 ms), com o fundo esmaecendo e o conteúdo até o fim da saída.
- Troca de página: só o conteúdo da página, a aba do projeto ou a seção da Administração esmaece subindo 4px em 200 ms; a barra lateral, o cabeçalho, o cabeçalho do projeto e a coluna da Administração ficam parados, e a página anterior sai na hora.
- Contagens das abas do projeto: o lugar de cada contagem aparece com as abas, e os números entram juntos, esmaecendo em 150 ms, quando a última chega.
- Com "reduzir movimento" no sistema, nada desliza nem cresce: tudo só esmaece, em 150 ms.

## Capabilities

### New Capabilities

- Nenhuma.

### Modified Capabilities

- `estrutura-das-telas`: o movimento dos diálogos, das gavetas e da troca de página.
- `projetos`: as contagens das abas do projeto juntas.

## Non-goals

- Os menus, as dicas e os campos de seleção, que continuam com o movimento do MUI.
- Animações de entrada nas listas, nos cartões ou nas linhas.
- A troca de projeto pelo mesmo endereço de aba (de um projeto para outro), que não refaz a página.

## Impact

- `web/src/theme/motion.ts`: os tempos e os quadros das janelas e das páginas.
- `web/src/components/Dialog.tsx` e `DetailsDrawer.tsx`: a transição própria, o conteúdo guardado até o fim da saída e a saída de quem é tirado da tela.
- `web/src/shell/AppShell.tsx`, `web/src/projects/ProjectLayout.tsx` e `web/src/admin/AdminLayout.tsx`: a entrada da página, da aba e da seção; `web/src/routes.tsx`: o `id` das rotas de estrutura.
- `web/src/projects/ProjectHeader.tsx`: as contagens juntas.
- Design system: Dialog, "Estados e movimento" e Tabs.
- ADRs: 0133.
