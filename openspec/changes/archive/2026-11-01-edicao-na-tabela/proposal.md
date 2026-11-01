## Why

Para mudar o nome, as datas, a duração, as dependências ou o progresso de uma tarefa, a gerente abre o detalhe, muda e salva, item por item. Quem monta ou acerta um cronograma grande quer fazer isso como numa planilha, pelo teclado. GanttB-6, do grupo 15 (Aprimoramento do Gantt 2), a pedido de Marcelo; as decisões ficam no ADR-0143.

## What Changes

- "Editar na tabela" na barra da aba Cronograma, na Lista, no computador: um modo em que as células se editam pelo teclado (Enter, F2 ou digitar; Enter e Tab salvam; Esc desiste) e a linha não abre o detalhe.
- Editáveis: o nome; o início, o fim, a duração e "Depende de" de quem tem datas próprias; o progresso, para quem edita e para o responsável.
- Cada célula pede à API o que o detalhe e a Linha do tempo já pedem (editar o item, as datas ou o progresso), é um passo do desfazer e dá um aviso.
- Valor inválido deixa a célula em edição com o motivo; a recusa do servidor volta o valor de antes.
- Sem mudança na API.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: o modo de edição na Lista, as células editáveis, o que cada uma pede e o valor inválido.

## Non-goals

- Colar várias células, incluir ou excluir linhas pela tabela.
- Responsável, data-limite e nota na tabela; a edição na Linha do tempo e no celular.

## Impact

- `web/src`: o `DataTable` com a edição de células, a Lista do cronograma (`ProjectSchedulePage`), as células de edição e as regras (`scheduleTableEdit.ts`), os textos e os testes.
- Documentação: o ADR-0143, `CLAUDE.md`, `docs/glossario.md` e o roadmap.
