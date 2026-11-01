## Why

O início de um projeto fiscal escorrega com frequência (o contrato atrasa, o cliente adia o kickoff), e hoje a gerente move tarefa por tarefa: as dependências empurram só as que dependem da primeira, e nada puxa quando o projeto adianta. GanttB-3, do grupo 15 (Aprimoramento do Gantt 2), a pedido de Marcelo; o que anda e como fica no ADR-0140.

## What Changes

- Quem edita o cronograma move, de uma vez, todas as tarefas e marcos em aberto de 1 a 999 dias úteis, para depois ou para antes, com a mesma duração em dias úteis, e as dependências seguram o que não pode começar antes.
- As tarefas a 100%, os marcos atingidos, as datas-limite e a linha de base não andam.
- O movimento é uma mudança só no desfazer, vai para a auditoria e avisa os responsáveis das tarefas que mudam de datas.
- API: `POST /api/projects/{id}/schedule/move` com `{ days }` (positivo para depois, negativo para antes), que devolve quantos itens mudaram.
- Na tela: "Mover o cronograma" no "⋮" da aba, para quem edita, abre o diálogo com a direção, os dias úteis e o que anda; o desfazer diz "o movimento do cronograma".

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: mover o cronograma em dias úteis, na API, no desfazer, na auditoria e na tela.

## Non-goals

- Mover só uma fase ou só os itens escolhidos.
- Mover pela nova data de início (ADR-0140).
- Mover os itens concluídos, as datas-limite ou a linha de base.

## Impact

- `src/Modules/Projects`: `Schedule.Move` no domínio, o caso de uso, a rota e o rótulo do desfazer; os testes.
- `web/src`: o item do "⋮", o diálogo, o texto do desfazer, os textos, os tipos gerados e os testes.
- Documentação: o ADR-0140, `CLAUDE.md`, `docs/glossario.md` e o roadmap (GanttB-3, ADR-0018, ADR-0051).
