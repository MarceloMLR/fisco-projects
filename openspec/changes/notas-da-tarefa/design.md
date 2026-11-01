## Context

O `ScheduleItem` guarda o nome, as datas, o responsável, o progresso e a data-limite; o `ScheduleItemPlan` leva o que o pedido de incluir ou de editar item pede, e o `Reschedule` (o arrastar e a borda na Linha do tempo) monta um plano com o que o item já tem. O desfazer guarda o `ItemState` de cada item; a auditoria grava os campos do item ao salvar. A exportação do MS Project monta o XML à mão (`MsProjectExport`), e a importação lê as linhas (`MsProjectRow`), converte num `SchedulePlanOutline` e o devolve à tela, que o manda de volta na criação (`ImportedItemRequest`).

## Goals / Non-Goals

**Goals:**

- A nota no item, com as regras dos outros campos: quem edita, o desfazer, a auditoria.
- Nunca perder a nota num pedido que não a traz.
- A nota na ida e na volta do MS Project.

**Non-Goals:**

- Conversa de notas, responsável escrevendo, texto formatado, nota na fase, PDF.

## Decisions

- **Domínio.** `ScheduleItem.Note` (`nvarchar(2000)`, nulo sem nota) e `ScheduleItem.NoteMaxLength`; `Schedule.NoteOf(text)` limpa a nota digitada (sem espaços nas pontas, as quebras de linha e as tabulações mantidas, os outros caracteres de controle recusados), devolvendo vazio para apagar e nulo quando não vale. O `ScheduleItemPlan` ganha `Note`: nulo mantém, vazio apaga; o `Reschedule` não a passa, e o item fica com a dele. O `ItemState` leva a nota para o desfazer. O `OutlineItem` ganha `Note`, que o `ApplyTemplate` põe no item (o `ProjectTemplate` não tem nota).
- **Casos de uso.** `ScheduleItemForm.Note` e `ScheduleField.Note` (o campo `note` no 400).
- **API.** `note` no pedido de item (ausente ou nulo mantém) e na resposta de cada item.
- **MS Project.** A exportação grava `<Notes>` depois do `<Deadline>`; a importação lê `<Notes>` na `MsProjectRow`, limpa e corta em 2.000 caracteres com o aviso `CutNotes` (os nomes dos itens), e a prévia leva `note` em cada item do plano, que volta no `ImportedItemRequest`.
- **Tela.**
  - `noteOf` e as regras em `schedule.ts`; o ícone (`StickyNote2Outlined`, `NoteMark`) depois do nome na Lista e na tabela da Linha do tempo, com o texto no `title` e para quem lê a tela.
  - O campo "Notas" (`TextArea`, opcional) no fim do `ScheduleItemFields`, com o contador de 2.000; o `bodyOf` manda a nota sempre, vazia para apagar; o ligar na Linha do tempo continua sem ela.
  - No detalhe de quem só vê, a seção "Notas" com o texto, quebras de linha mantidas.
  - O Excel ganha a coluna "Notas" no fim da aba Cronograma; a Auditoria diz o campo "Notas".
- **Demonstração.** O plano ganha `note` nos itens; a gerente as escreve ao montar o cronograma.

## Risks / Trade-offs

- [A semântica de nulo mantém, diferente da data-limite, em que nulo tira] → É a do `links` (ADR-0137); evita que o arrastar e o ligar, que mandam o pedido inteiro, apaguem a nota.
- [Notas longas no arquivo do MS Project] → Cortadas em 2.000 caracteres, com o aviso na prévia.
