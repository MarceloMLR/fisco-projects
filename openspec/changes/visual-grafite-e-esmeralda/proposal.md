## Why

O ADR-0159 registrou a marca GerenWare, em grafite e esmeralda. As telas continuam no azul corporativo, com o marinho na entrada. Esta change troca os valores dos tokens pelos do manual, nos dois temas, para o produto ficar com as cores da marca antes de ganhar a logo e o nome (Marca-3 e Marca-4).

## What Changes

- Tokens de cor na paleta do manual, nos temas claro e escuro: ação em esmeralda, neutros na escala grafite, concluído em esmeralda, "em andamento" em azul-céu, página de entrada em grafite, e sombras e avatar nos tons grafite.
- Na conversa do chamado: o líder técnico passa ao azul que era do responsável; o responsável, que segue a cor principal, fica em esmeralda; o time Funcional vai do verde-oliva para um oliva mais amarelado, para não se confundir com a esmeralda.
- O alerta informativo passa aos tons neutros, e a barra de progresso "informativa" (o consumo das horas), ao grafite: o azul-céu fica só no "em andamento".
- As etiquetas de status passam ao canto de 3 px.
- A barra das tarefas na Linha do tempo fica em esmeralda clara, com a parte concluída em esmeralda.
- As cores da instalação (ADR-0146, ADR-0147) continuam valendo sobre os valores padrão. O cálculo passa a tirar o matiz e a saturação da cor escolhida, e só a claridade do token, porque o painel e o fundo da entrada padrão agora são grafite, sem matiz para girar.
- O design system no Claude Design passa a se chamar GerenWare, com os mesmos valores; `web/src/theme/tokens.json` continua a cópia exata dele.
- Os e-mails, que leem as cores do mesmo arquivo, mudam junto.

## Capabilities

### New Capabilities

### Modified Capabilities

- `estrutura-das-telas`: as cores padrão passam a ser as da marca GerenWare, com o contraste conferido também nas etiquetas de status e nos selos de papel, o alerta informativo em grafite e o canto das etiquetas de status.
- `configuracao-da-instalacao`: sem cor escolhida, o fundo da entrada é o grafite do design system.

## Non-goals

- A logo, a frase e as formas da página de entrada, e os ícones do navegador (Marca-3).
- O nome GerenWare nos textos (Marca-4) e no código (Marca-5).
- Mudar as cores de aguardando, atrasado e perigo, que ficam como estão.
- Mudar a regra das cores da instalação: qualquer cor continua aceita, com os mesmos ajustes de contraste.

## Impact

- `web/src/theme/tokens.json` (os valores e as notas de uso), `web/src/theme/brandColors.ts` (o cálculo das cores da instalação), `Alert`, `StatusBadge`, `ProgressBar` e `ColorField`, com os testes.
- Design system no Claude Design: `tokens.json`, o guia e o nome.
- Nenhuma mudança na API nem no banco.
