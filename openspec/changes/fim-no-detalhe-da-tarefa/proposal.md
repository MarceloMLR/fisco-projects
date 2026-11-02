## Why

No detalhe da tarefa e em "Nova tarefa", a pessoa digita o início e a duração em dias úteis, e o fim só aparece como "Termina em" depois de salvar. Quem planeja costuma saber o fim ("até o dia 30"), não quantos dias úteis cabem até lá. Marcelo perguntou por que a duração é obrigatória e pediu, em 01/11/2026, o campo "Fim" ao lado da duração, com um calculando o outro (ADR-0145). Cronograma-4.3.

## What Changes

- O detalhe da tarefa e "Nova tarefa" ganham o campo "Fim" ao lado da duração, na tarefa com datas próprias (não a tarefa-resumo nem o marco). Vale o último digitado: o fim calcula a duração, a duração calcula o fim, e mudar o início mantém o que vale e recalcula o outro.
- A conta é do servidor, pelo calendário da instalação (ADR-0051): nova rota de leitura `GET /api/projects/{id}/schedule/item-dates?start=&duration=` ou `?start=&end=`, que devolve o início no dia útil, o fim e a duração, sem gravar nada, para quem vê o projeto.
- O pedido de incluir e editar item (`POST .../schedule/items` e `PUT .../schedule/items/{itemId}`) aceita `end`: com ele, a duração da tarefa sai do início e do fim (`Schedule.DurationBetween`, a mesma conta do `PUT .../dates`); o fim antes do início, longe demais, num marco ou numa tarefa com subtarefas dá 400 em `end`. Sem `end`, nada muda.
- A ajuda "Termina em 13/10/2026" embaixo da duração sai: o fim fica no próprio campo.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: o fim no lugar da duração no pedido de item, as datas da tarefa antes de salvar e o campo "Fim" no detalhe e em "Nova tarefa".

## Non-goals

- O fim do marco (ele é um dia só) e o da tarefa-resumo (vem das subtarefas, ADR-0088).
- Mudar a edição na tabela (ADR-0143), que já aceita o fim.
- Mostrar, antes de salvar, o empurrão das dependências: a conta usa o início digitado, e o detalhe volta com as datas gravadas.

## Impact

- `src/Modules/Projects`: o `ScheduleItemForm` e o pedido com o fim, a conferência do item com o calendário, as datas de uma tarefa no domínio (`Schedule.DatesOf`), o caso de uso e a rota das datas antes de salvar.
- `web/src`: o campo "Fim" e a conta pelo servidor no `ScheduleItemFields`, o formulário (`scheduleItemValues.ts`), a consulta das datas, os textos, os tipos gerados e os testes.
- Documentação: o ADR-0145, `CLAUDE.md` e o roadmap (ADR-0018, ADR-0051, ADR-0071).
