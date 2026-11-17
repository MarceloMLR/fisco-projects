## Context

As cores das telas vêm de `web/src/theme/tokens.json`, cópia exata do `tokens.json` do design system, e viram variáveis CSS e a paleta do MUI (`tokens.ts`, `theme.ts`). As cores da instalação (ADR-0146) trocam um grupo de tokens em `brandColors.ts`: cada token derivado mantinha a claridade do design system e girava o matiz e a saturação do próprio valor, na proporção da cor escolhida para o azul padrão. Os e-mails leem o mesmo arquivo (`EmailRenderer`). O ADR-0159 dá os valores da marca; o manual, a referência visual.

## Goals / Non-Goals

**Goals:** os valores do manual nos dois temas, com contraste AA em todos os pares de texto e fundo; as cores da instalação funcionando como antes sobre o padrão novo.

**Non-Goals:** logo, frase e formas da entrada (Marca-3); textos e nomes (Marca-4 em diante); tokens novos.

## Decisions

- **Só valores, sem tokens novos.** Os 109 tokens de cor ficam com os mesmos nomes; mudam os valores e as notas de uso. Assim nenhum componente muda por causa do nome de um token.
- **Tons fora da lista do ADR, dentro da família.** O ADR-0159 dá os tons principais. Os derivados (ao apontar, selecionado, contornos das mensagens, barras do Gantt) saem da escala grafite e da família esmeralda, conferidos pelo contraste. No tema claro, o trilho das barras de progresso fica um pouco mais claro que a borda, para a parte concluída em `#059669` ter 3:1 sobre ele.
- **Ação no tema escuro.** O botão continua `#047857` com texto branco (5,5:1) e tem 3:1 sobre o fundo; os links, o foco e o item ativo usam a esmeralda clara `#6EE7B7`. Ao apontar e ao pressionar, o botão escurece nos dois temas, como no manual, porque o tom mais claro não segura o texto branco.
- **Informativo em grafite.** O `Alert` de tom `info` passa a usar os tokens neutros, e a barra de progresso `info`, o texto secundário. As etiquetas "Em andamento", "Em análise" e parecidas continuam em `status-info-*`, agora azul-céu. Alternativa descartada: um token novo para o alerta; os neutros já servem.
- **Papéis na conversa.** O líder técnico recebe os valores azuis que eram do responsável (ADR-0159). O responsável ganha a família esmeralda nos tokens `msg-owner-*` e `role-owner-*`. O Funcional gira o matiz do oliva de 135° para 120° em OKLCH, com a mesma claridade e saturação, para ficar a 45° da esmeralda e a 55° do ocre do Desenvolvimento. Alternativa descartada: deixar o oliva como está, a 30° da esmeralda, com o responsável e o time Funcional quase na mesma cor numa mesma conversa.
- **Cores da instalação pela cor escolhida.** Com o padrão grafite, girar o matiz do token não dá cor nenhuma ao painel da entrada. Cada token derivado passa a ter a claridade do design system, o matiz da cor escolhida e uma parte fixa da saturação dela (`chromaShares`, as mesmas proporções que o azul antigo dava). O resultado para uma cor escolhida fica igual ao de antes, salvo pela claridade nova de cada token. Sem cor escolhida, valem os valores do `tokens.json`.
- **Canto das etiquetas de status.** O `StatusBadge` passa de `radius-md` para `radius-sm` (3 px), como o ADR-0159 decidiu.

## Risks / Trade-offs

- Ação e concluído na mesma família → a forma separa: sólido com texto branco é ação; etiqueta suave com ícone é status (ADR-0159).
- Instalação com cor principal própria continua com a linha selecionada e as barras do cronograma nos tons padrão, agora esmeralda, como antes ficavam azuis. A cor secundária troca a linha selecionada.
- O design system e o repositório precisam ficar iguais → o `tokens.json` é escrito uma vez e publicado nos dois lugares.
