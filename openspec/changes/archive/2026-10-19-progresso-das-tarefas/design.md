# Design: progresso-das-tarefas (Cronograma-2)

## Context

- A Cronograma-1 criou o agregado `Schedule`, com as mudanças em `ScheduleEditing.RunAsync`. Essa rotina recusa quem não tem `CanEditSchedule` e roda numa transação serializável.
- O progresso entra no formulário da tarefa (`ScheduleItemForm.Progress`) e a data de conclusão é gravada em `ScheduleItem.SetProgress`.
- O atraso e as contagens ficam num lugar só, `ScheduleFigures`, usado pela aba, pela lista de projetos e pelo cabeçalho.

## Goals / Non-Goals

**Goals:**

- Uma regra única de quem atualiza o progresso de cada tarefa, devolvida pela API por item, para a tela não repetir a regra.
- O progresso salvo por uma ação própria, sem que a edição da tarefa desfaça o percentual informado pelo responsável.
- O marco com os estados atingido, pendente e atrasado, contado como as tarefas.

**Non-Goals:**

- As ações em Meu trabalho (Síntese-1) e o termo de aceite que marca o marco: usam estas mesmas regras depois.

## Decisions

### 1. Permissão do progresso na `ProjectPermissions`

- `ProjectPermissions` ganha `CanUpdateOwnProgress`: verdadeiro para o administrador, o gerente, o líder técnico, o colaborador e o desenvolvedor; falso para o leitor.
- A regra fica num método só: `MayUpdateProgress(isOwner) => CanEditSchedule || (CanUpdateOwnProgress && isOwner)`.
- A API devolve `canUpdateProgress` em cada item do cronograma, já falso no projeto encerrado.
- Alternativa: calcular na tela a partir do papel e do responsável. Foi rejeitada, porque repetiria a regra de permissão fora do servidor.

### 2. Recusa parametrizada em `ScheduleEditing`

- `RunAsync` passa a receber quem pode mudar: uma função das permissões e do cronograma, que por padrão é `CanEditSchedule`.
- A ordem das recusas continua a das outras ações:
  1. 404 quando a pessoa não vê o projeto;
  2. 403 quando a função recusa;
  3. 409 quando o projeto está encerrado;
  4. 400 nos campos.
- No progresso, a função olha o responsável do item. Um item que não existe passa pela função e recebe 404 do próprio caso de uso.
- A checagem roda antes da transação, para não abrir uma transação serializável à toa, e de novo dentro dela, com o cronograma carregado.

### 3. Rotas próprias para o progresso e o marco

- `PUT /schedule/items/{itemId}/progress` com `{ progress }` devolve 204.
- `PUT /schedule/items/{itemId}/reached` com `{ reached }` devolve 204.
- Na alteração do item, o `progress` vira opcional: sem ele, o progresso fica como está.
- A tela não manda mais o progresso no formulário; o progresso só muda pela ação própria. A API continua aceitando o progresso na alteração, para quem edita o cronograma.
- Alternativa: tirar o progresso da alteração do item. Foi rejeitada, porque mudaria o contrato e os testes da Cronograma-1 sem ganho para quem edita.
- "Iniciar", "Atualizar progresso" e "Concluir" são a mesma rota. "Concluir" manda 100; os outros dois mandam o número digitado (ADR-0086).

### 4. Marco atingido e atrasado no domínio

- `ScheduleItem.ReachedOn` (`date`, nulo) é preenchido por `Schedule.SetReached(itemId, reached, today)`. Marcar de novo um marco atingido mantém a data, e desmarcar a apaga.
- `ChangeItem` não mexe em `ReachedOn`, e uma tarefa nunca o tem.
- `IsLate(today)`:
  - na tarefa, continua igual: fim antes de hoje, abaixo de 100%;
  - no marco, é a data antes de hoje, sem `ReachedOn`.
- `ScheduleFigures` passa a contar `LateMilestones` à parte de `LateTasks`. A fase fica atrasada com qualquer item atrasado, e a fase atual (a primeira atrasada) passa a considerar os marcos.
- A conclusão do cronograma continua olhando só as tarefas.
- A migração `MilestoneReached` acrescenta a coluna.

### 5. Telas

- **Lista:** a coluna Progresso do marco mostra o estado pelo `DueDate`:
  - "Atingido em 09/10" (met);
  - "Pendente" (onTrack);
  - "Atrasado" (overdue).
- **Resumo e lista de Projetos:** dizem "N tarefas atrasadas" e "M marcos atrasados", cada um quando houver.
- **Detalhe da tarefa:** o bloco Progresso (`TaskProgress`) mostra a barra, a conclusão e os botões que o `canUpdateProgress` libera:
  - "Iniciar" a 0%;
  - "Atualizar progresso" acima de 0%;
  - "Concluir" abaixo de 100%.

  "Iniciar" e "Atualizar progresso" abrem um diálogo pequeno com o percentual (React Hook Form com Zod, de 0 a 100). O formulário da tarefa perde o campo Progresso.
- **Detalhe do marco:** o bloco Estado mostra o estado e, para quem edita, "Marcar como atingido" ou "Desmarcar".
- **Gaveta para quem não edita, mas atualiza o progresso:** fica com o detalhe só leitura da Cronograma-1 mais o bloco Progresso.

## Risks / Trade-offs

- [O gerente e o responsável mudam a mesma tarefa ao mesmo tempo] → A edição sem o progresso não o sobrescreve, e as duas rodam na transação serializável.
- [Um marco fica atrasado sem o termo de aceite, que só chega no grupo de termos] → O gerente marca à mão até lá (ADR-0043).

## Migration Plan

- Migração `MilestoneReached` no schema `projects`, com a coluna `ReachedOn`, nula, em `schedule_items`.
- Os marcos existentes ficam pendentes.
