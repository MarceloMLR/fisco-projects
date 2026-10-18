# Design: detalhes em gaveta sobreposta (Acesso-3.1)

## Context

A motivação está em proposal.md, e a decisão visual no ADR-0081. Hoje a `AuditPage` tem dois jeitos de mostrar os detalhes:

- em telas de 1200 px ou mais, uma coluna fixa (`aside`, 340 px, `bg-subtle`) que já começa com o primeiro registro da página;
- abaixo disso, um `Drawer` do MUI de 360 px, aberto ao escolher uma linha.

A tabela de mudanças do `AuditDetails` usa `overflowWrap: 'anywhere'`, que deixa a coluna encolher abaixo da largura de uma palavra e a quebra no meio. O filtro de pessoa tem 180 px.

O design system já tem o que a gaveta precisa:

- `bg-elevated`, para "tudo que flutua";
- `shadow-dialog`;
- `overlay`, o fundo escurecido já usado no menu do celular.

## Goals / Non-Goals

**Goals:**

- Um componente só para detalhes de itens de lista, usado na Auditoria agora e por documentos, termos e membros depois.
- Teclado e leitor de tela: a gaveta é um diálogo com nome, prende o foco enquanto aberta e devolve o foco ao fechar.

**Non-Goals:**

- Abrir a gaveta pelo endereço (link direto para um registro). Pode vir quando uma tela precisar.

## Decisions

### 1. `DetailsDrawer` em `web/src/components`

É um wrapper do `Drawer` do MUI com `anchor="right"`, modal:

- **Fundo e superfície.** O fundo usa `var(--overlay)`. O papel usa `bg-elevated`, `shadow-dialog` e borda à esquerda com `border`.
- **Largura.** `width: 100%` com `maxWidth: 640px`, então ocupa a tela toda no celular. 640 px cabem a tabela de mudanças de três colunas sem quebrar os valores comuns (papéis, situação, nomes) e ainda deixam, num monitor de 1440 px, a maior parte da lista visível atrás.
- **Estrutura.** Uma linha de topo com o botão Fechar (ícone `CloseOutlined`, nome da tradução) e a área de conteúdo, que rola por dentro (ADR-0066).
- **Acessibilidade.** O papel tem `role="dialog"` e `aria-label` vindo de quem usa. Fechar pelo Esc e pelo clique no fundo é do `Modal` do MUI, e ele devolve o foco ao elemento que estava focado antes, a célula da linha escolhida.
- **Animações.** A entrada e a saída usam as do `Drawer` do MUI. A saída não esconde nada que mudou: a lista atrás é a mesma de antes de abrir.

**Alternativas:**

- **Manter a coluna fixa e só alargá-la:** descartada, porque tira ainda mais largura da tabela, o que Marcelo quis evitar.
- **Uma página só para o detalhe:** descartada, porque perde o contexto da lista e exige ir e voltar a cada registro.

### 2. Auditoria sem coluna fixa

- Saem o `useMediaQuery` de 1200 px e o `aside`.
- O estado passa a ser o registro escolhido, que fica guardado mesmo se a lista se atualizar por trás, e se a gaveta está aberta.
- Escolher uma linha guarda o registro e abre a gaveta. O `selectedRowId` da tabela só vale com a gaveta aberta.
- Nenhum registro é escolhido de início.

### 3. Tabela de mudanças sem quebrar palavras

`overflowWrap: 'break-word'` no lugar de `'anywhere'`. Com isso, a largura mínima de cada coluna é a da maior palavra, e só um texto maior que a coluna quebra no meio, como um e-mail longo. As três colunas dividem a largura da gaveta (`table-layout: auto`).

### 4. Filtro de pessoa

- A largura passa de 180 para 240 px, já que a lista ganhou a largura da coluna.
- O texto do campo ganha `text-overflow: ellipsis`. Fora do foco, o nome que não cabe termina em "…"; com o foco, o campo rola o texto normalmente para edição.

## Risks / Trade-offs

- **[Detalhe cobre a lista]** Com a gaveta aberta, não dá para ver a linha ao lado sem fechar. → Fechar é imediato (Esc ou clique fora), e a linha escolhida fica marcada.
- **[Padrão para telas que ainda não existem]** Documentos, termos e membros podem ter detalhes maiores. → A gaveta rola por dentro e tem largura máxima. Uma tela que precise de outro formato propõe isso na própria change.

## Migration Plan

Nada para migrar: só frontend. Para voltar atrás, basta reverter o PR.
