## Why

Quando um botão demora a responder, não dá para saber se o clique foi aceito, se está processando ou se falhou, e o clique repetido pode mandar a ação de novo. Marcelo pediu, em 30/10/2026, o ícone de carregamento nos botões ao clicar, em todos os botões que mandam algo ao servidor (ADR-0131). Mudança fora do roadmap: Fundação-5.5.

## What Changes

- Todo botão e botão de ícone que manda algo ao servidor fica em andamento do clique até a resposta: o ícone de carregamento no lugar do ícone (antes do texto, quando não há ícone), o rótulo e a largura mantidos, as cores da variante, sem hover e sem novo clique.
- O botão de envio dos formulários e dos diálogos fica em andamento enquanto a gravação roda, também pelo Enter num campo.
- Os componentes `Button` e `IconButton` do sistema percebem o andamento pela ação que disparam, e o `Form` (usado também pelo `Dialog` com `onSubmit`) pelo envio; o lint recusa o botão do MUI usado direto.
- O design system ganha o estado em andamento do Button, no guia e na prévia.

## Capabilities

### New Capabilities

- Nenhuma.

### Modified Capabilities

- `estrutura-das-telas`: os botões em andamento.

## Non-goals

- Os itens dos menus "⋮" (depois, ADR-0131).
- Mudar o indicador das listas e das gavetas, que continua depois de 300 ms (ADR-0080).
- Barras de progresso de envio de arquivo.

## Impact

- `web/src/components`: o `Button` e o `IconButton` próprios, o `Form` e o `Dialog`; o tema do MUI (o estado em andamento do fp-btn).
- `web/src`: toda tela troca o `Button` e o `IconButton` do MUI pelos do sistema e passa a devolver a promessa da ação no clique e no envio.
- `web/eslint.config.js`: o import do `Button` e do `IconButton` do MUI só nos componentes do sistema.
- Design system: o estado em andamento do Button (guia e prévia).
- ADRs: 0062, 0065, 0080, 0131 (novo).
