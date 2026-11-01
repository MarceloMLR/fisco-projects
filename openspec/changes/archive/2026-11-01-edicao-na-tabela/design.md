## Context

A Lista é o `DataTable` (MUI X Data Grid gratuito), com as fases como linhas de grupo e o clique na linha abrindo o detalhe. O Data Grid gratuito tem a edição de células (`editable`, `isCellEditable`, `processRowUpdate`, `preProcessEditCellProps`, `renderEditCell`), com o teclado de planilha.

## Goals / Non-Goals

**Goals:** editar pelo teclado o nome, as datas, a duração, as dependências e o progresso, com as rotas e as regras que já existem.

**Non-Goals:** colar, incluir e excluir linhas, editar na Linha do tempo.

## Decisions

- **`DataTable`.** Ganha `editing` (`isCellEditable`, `processRowUpdate`, `onProcessRowUpdateError`), passado ao Data Grid; com a edição ligada, a tela não passa `onRowChoose`, e o Enter fica com o Data Grid.
- **Regras num lugar só.** `scheduleTableEdit.ts`: `editableFields` (o que cada item deixa editar a quem), `cellError` (a conferência da célula, com as chaves de erro do formulário do item) e `cellRequest` (o pedido: o item, as datas ou o progresso, ou nada quando o valor não mudou).
- **Colunas.** As editáveis ganham `valueGetter` com o valor de edição (o dia em dd/mm/aaaa, a duração e o progresso como número, as dependências como ids) e um `valueSetter` que guarda a célula editada (`edit` na linha), lida pelo `processRowUpdate`; a exibição continua no `renderCell`.
- **Células de edição.** `ScheduleEditCell` (um campo do design system dentro da célula, com o motivo do erro no `title` e no nome acessível) e `DependsOnEditCell` (o `MultiSelect` num `Popper` sobre a célula, como o Data Grid indica para valores longos).
- **Salvar.** O `processRowUpdate` manda o pedido, avisa, pede o cronograma de novo e devolve a linha nova lida do cache; na recusa, avisa e devolve a linha de antes, que volta a célula.

## Risks / Trade-offs

- [O Data Grid muda a API de edição entre versões] → o `overrides` já fixa o `@mui/x-internals`, e os testes de tela cobrem cada célula.
- [Um pedido por célula] → como o detalhe; o desfazer volta uma célula por vez.
