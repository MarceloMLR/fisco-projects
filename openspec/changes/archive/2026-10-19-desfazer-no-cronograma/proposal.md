# Proposta: desfazer no cronograma (Cronograma-6)

## Why

Quem edita o cronograma move uma tarefa e empurra uma cadeia inteira, exclui uma fase por engano ou troca a duração errada, e hoje só consegue voltar refazendo tudo à mão. O ADR-0021 pôs na v1 desfazer as últimas ações da própria pessoa, inclusive as tarefas empurradas em cadeia, bloqueado quando alguém alterou essas tarefas depois. O ADR-0071 pôs o botão "Desfazer" na barra da aba e o "refazer" no menu "⋮".

## What Changes

- **Histórico de cada pessoa** (ADR-0091):
  - toda mudança do cronograma feita pelo administrador ou pelo gerente guarda os registros que mudou, antes e depois, com o nome da ação;
  - as tarefas empurradas e as tarefas-resumo recalculadas entram na mesma mudança;
  - ficam as últimas 20 mudanças de cada pessoa por projeto.
- **Desfazer** (`POST /api/projects/{id}/schedule/undo`):
  - volta a última mudança da pessoa que ainda não foi desfeita, com tudo o que ela empurrou;
  - se alguém mudou esses itens depois, recusa com 409 e tira a mudança do histórico;
  - sem nada a desfazer, 409.
- **Refazer** (`POST /api/projects/{id}/schedule/redo`):
  - aplica de novo a última mudança desfeita, com a mesma proteção;
  - uma mudança nova da pessoa apaga o que ela podia refazer.
- **Quem desfaz:** só quem edita o cronograma (ADR-0043), nas próprias mudanças, com o projeto aberto; os outros recebem 403, e quem não está no projeto, 404.
- **API:** o `GET /schedule` passa a dizer, para quem edita, o que o desfazer e o refazer fariam (`undo` e `redo`, com a ação e o nome).
- **Tela** (ADR-0071):
  - o botão "Desfazer" na barra da aba e no cabeçalho do Expandir, com o que será desfeito na dica, desligado quando não há o que desfazer;
  - "Refazer" no menu "⋮";
  - a mensagem diz o que foi desfeito ou refeito, ou por que não deu.
- **Auditoria:** desfazer e refazer aparecem como as mudanças dos itens que voltaram (ADR-0039).

## Non-goals

- Desfazer o progresso atualizado pelo responsável que não edita o cronograma, a linha de base, as pessoas do projeto e os dados do projeto: só as mudanças do cronograma feitas por quem o edita.
- Atalhos de teclado (Ctrl+Z): a Linha do tempo e os campos já usam o teclado; ficam para depois.
- Desfazer a mudança de outra pessoa.
- Dados de demonstração novos: a carga monta o cronograma como a gerente, então o histórico dela já vem com as últimas mudanças.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: desfazer e refazer as próprias mudanças, pela API e pela tela, e quem pode.

## Impact

- **Backend (`Projects`):**
  - `ScheduleSnapshot` e `ScheduleDiff` no domínio, com `Schedule.Snapshot` e `Schedule.Apply`;
  - `ScheduleChange`, a tabela `schedule_changes` e a migração;
  - o registro no `ScheduleEditing.RunAsync`, com o nome da ação de cada caso de uso;
  - os casos de uso `UndoScheduleChange` e `RedoScheduleChange`, as rotas e a matriz de permissões;
  - o `GetSchedule` com `undo` e `redo`.
- **Frontend:** o botão "Desfazer", o "Refazer" no "⋮", os textos e os testes.
- **Docs:** ADR-0091, `CLAUDE.md` e roadmap.
