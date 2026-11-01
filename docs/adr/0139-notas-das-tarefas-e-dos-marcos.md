---
status: aceita
date: 2026-11-01
decision-makers: Marcelo Ribeiro
---

# ADR-0139: Notas nas tarefas e nos marcos

Completa o ADR-0019, que não tratava de anotações nos itens do cronograma.

## Contexto e problema

Num projeto fiscal, boa parte do atraso vem de fora: a tarefa espera o cliente mandar os arquivos do plano de contas, o marco depende da homologação na Sefaz. Hoje isso fica em e-mails ou na cabeça da gerente; quem abre o cronograma vê a tarefa atrasada e não sabe por quê. O MS Project tem um campo de notas em cada tarefa, que os arquivos importados trazem (ADR-0135) e que se perde na conversão. Marcelo pediu as notas no grupo 15 (GanttB-2). Que forma elas têm, quem as escreve e por onde passam?

## Fatores de decisão

- Dizer, no próprio item, o que a equipe precisa saber dele.
- Ida e volta pelo MS Project sem perder as notas.
- Manter as permissões do cronograma (ADR-0043) e o desfazer (ADR-0091).
- Escopo enxuto: a conversa com histórico já existe nos chamados (ADR-0026).

## Opções consideradas

Forma da nota:

- A1. Um texto simples por tarefa e marco, como o campo de notas do MS Project.
- A2. Uma conversa de notas, cada uma com o autor e a data, como a dos chamados.

Quem escreve:

- B1. Quem edita o cronograma (o administrador e o gerente), como os outros campos do item.
- B2. Também o responsável pela tarefa, como o progresso.

## Resultado da decisão

Opções escolhidas: **A1** e **B1**, porque cobrem o caso da equipe ("aguardando o cliente mandar os arquivos"), vão e voltam no MS Project sem conversão e não mudam a matriz de permissões do cronograma. A conversa com histórico fica para os chamados, que já a têm.

Regras:

- **Nota.** Cada tarefa (também a resumo e a subtarefa) e cada marco tem, opcionalmente, uma nota de texto simples, com quebras de linha, de até 2.000 caracteres. A fase não tem nota.
- **Quem escreve e quem vê.** Quem edita o cronograma (o administrador e o gerente) escreve, troca e apaga a nota pelo mesmo pedido de editar o item; todos do projeto a veem (ADR-0043).
- **Pedido.** A nota ausente ou nula no pedido de editar o item a mantém, e a nota vazia a apaga, para o arrastar e o ligar na Linha do tempo não a perderem.
- **Cronograma.** A nota entra no desfazer (ADR-0091) e na auditoria (ADR-0039); não muda datas nem progresso e fica fora da linha de base e dos modelos (ADR-0017).
- **MS Project.** A exportação grava a nota no `Notes` da tarefa, e a importação a traz, cortada em 2.000 caracteres com um aviso (ADR-0135).
- **Telas.** A Lista e a tabela da Linha do tempo mostram um ícone de nota ao lado do nome, com o texto ao apontar; o detalhe mostra a nota e, para quem edita, o campo "Notas"; o Excel tem a coluna "Notas".

Fora:

| Item | Classificação |
| --- | --- |
| Conversa de notas com autor e data, menções e anexos | fora: é o chamado (ADR-0026) |
| Nota escrita pelo responsável da tarefa | depois, se a equipe pedir |
| Texto formatado na nota | fora |
| Nota na fase | fora |

### Consequências

- Bom, porque o motivo de um atraso fica no item, à vista de todos do projeto.
- Bom, porque a nota do MS Project não se perde na ida nem na volta.
- Ruim, porque o responsável pela tarefa, que sabe o que está esperando, pede à gestão para escrever a nota.
- Ruim, porque a nota não guarda quem a escreveu nem quando; a auditoria guarda.

### Confirmação

Testes cobrindo: a nota escrita, trocada e apagada por quem edita; a nota mantida quando o pedido não a traz; a nota acima de 2.000 caracteres recusada; o desfazer e a auditoria da nota; a nota na exportação e na importação do MS Project, com o corte e o aviso; e a nota na Lista, no detalhe e no Excel.

## Prós e contras das opções

### A1. Um texto simples

- Bom, porque é o campo do MS Project e cabe no item sem tabela nova.
- Ruim, porque não guarda o histórico na tela.

### A2. Uma conversa de notas

- Bom, porque guarda quem escreveu e quando.
- Ruim, porque repete a conversa dos chamados e não vai e volta no MS Project.

### B1. Quem edita o cronograma

- Bom, porque segue o ADR-0043 sem mudança.
- Ruim, porque o responsável não escreve.

### B2. Também o responsável

- Bom, porque quem faz a tarefa diz o que espera.
- Ruim, porque abre ao responsável a edição de um campo do item, que hoje é só da gestão.
