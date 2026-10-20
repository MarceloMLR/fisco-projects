# Design: desfazer no cronograma (Cronograma-6)

## Context

As mudanças do cronograma passam todas pelo `ScheduleEditing.RunAsync`, numa transação serializável, com as recusas na ordem 404, 403, 409 e 400. O domínio calcula as datas e empurra as seguintes (ADR-0019, ADR-0051), recalcula as tarefas-resumo (ADR-0088) e guarda tudo no agregado `Schedule` (fases, itens e dependências). O ADR-0091 decidiu guardar as fotos dos registros que cada mudança mudou.

## Goals / Non-Goals

**Goals:**

- Desfazer e refazer as próprias mudanças, com tudo o que elas empurraram, protegendo o trabalho de outra pessoa.
- Um mecanismo só para todas as ações do cronograma.

**Non-Goals:**

- Atalhos de teclado, desfazer de outras áreas e o progresso de quem não edita (proposta).

## Decisions

### 1. Fotos e diferença no domínio

- `ScheduleSnapshot`: listas de `PhaseState`, `ItemState` e `DependencyState`, valores com todos os campos de cada registro.
- `ScheduleDiff.Between(antes, depois)`: só os registros que mudaram, cada um com o antes e o depois (nulo quando não existia ou deixou de existir).
- `Schedule.Apply(diff)`:
  - confere que cada registro está como o "antes" do diff;
  - se estiver, deixa cada um como o "depois": cria, altera ou exclui fases, itens e dependências, com os mesmos identificadores;
  - se não estiver, não muda nada e devolve falso.
- Desfazer é aplicar o diff invertido (`Inverse`); refazer é aplicar o diff.
- A comparação é por valor: o cronograma voltar ao mesmo estado por outro caminho não bloqueia.

### 2. Histórico

- `ScheduleChange` (tabela `projects.schedule_changes`): o projeto, a pessoa, a ação (`ScheduleAction`), o nome do item, o diff em JSON e se foi desfeita.
  - A ordem é a do identificador (GUID v7).
  - A chave estrangeira para o cronograma é em cascata.
  - Fica fora da auditoria, porque os itens que voltam já são auditados.
- O `RunAsync`:
  - tira a foto antes e depois da mudança;
  - com diferença, rótulo e a pessoa podendo editar o cronograma, apaga as mudanças desfeitas dela (o refazer), grava a nova e deixa só as 20 mais recentes da pessoa no projeto.
- Cada caso de uso devolve o rótulo no `ScheduleResult` (`Labeled`):
  - a ação: incluir, renomear e excluir fase; incluir, alterar e excluir tarefa ou marco; mudar as datas; atualizar o progresso; marcar ou desmarcar o marco;
  - o nome do item.
- A linha de base não muda fases, itens nem dependências, então não entra.

### 3. Desfazer e refazer

- Casos de uso `UndoScheduleChange` e `RedoScheduleChange`, com as mesmas recusas: 404, 403 (só quem edita), 409 do projeto encerrado.
- Sem mudança a desfazer ou refazer: 409 `schedule-nothing-to-undo`.
- Desfazer pega a mudança mais recente da pessoa não desfeita. Refazer pega a desfeita mais antiga, a última que foi desfeita, porque o desfazer vai da mais nova para a mais velha e uma mudança nova apaga as desfeitas.
- Bloqueado: a mudança sai do histórico, a transação grava só isso, e a resposta é 409 `schedule-undo-blocked`.
- Feito: 200 com a ação e o nome, para a tela dizer o que voltou.

### 4. API e tela

- `GET /schedule`: `undo` e `redo` (`{ action, name }` ou nulo), só para quem edita e com o projeto aberto.
- **Tela:**
  - o botão "Desfazer" (ícone `undo`, discreto) fica entre "Exibir" e "Expandir" e no cabeçalho do Expandir, como no canvas;
  - a dica do botão é "Desfazer a inclusão da tarefa X";
  - "Refazer" fica no "⋮", que passa a ter "Refazer" e a linha de base;
  - as mensagens: "A inclusão da tarefa X foi desfeita.", "… foi refeita." e "Não dá para desfazer: alguém mudou esses itens depois.".

## Risks / Trade-offs

- **[JSON do diff]:** a estrutura dos registros pode mudar em changes futuras. → Os estados são registros do domínio serializados por nome de campo. Um campo novo vem nulo ou com o valor padrão nas mudanças antigas, e o pior caso é um desfazer bloqueado.
- **[Mudanças fora do RunAsync]:** a saída de uma pessoa do projeto tira o responsável das tarefas dela sem passar pelo histórico. → A comparação com o "depois" bloqueia o desfazer que tocaria esses itens.
