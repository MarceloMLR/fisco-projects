# Design: subtarefas (Cronograma-4)

## Context

- O agregado `Schedule` (Cronograma-1) tem fases, itens (tarefas e marcos) e dependências termina → começa. As datas são calculadas no domínio pelo `IBusinessDays`, e as dependências empurram as seguintes em ordem topológica (`Push`).
- Os números da fase e do projeto saem de `ScheduleFigures`: datas do primeiro início ao último fim, progresso ponderado pela duração das tarefas (`WeightedProgress`) e atrasos.
- A Cronograma-2 deu ao progresso uma ação própria, que o responsável também faz. A Cronograma-3 trouxe a Linha do tempo (`ScheduleGantt`, com as linhas em `ganttRows.ts`) e as novas datas por `PUT .../dates`.
- O ADR-0088 decidiu: um nível, a tarefa-resumo calculada, dependências só entre quem faz o trabalho e tarefas e marcos dentro de uma tarefa.

## Goals / Non-Goals

**Goals:**

- Subtarefas com as mesmas regras de datas, dependências, progresso, permissões e auditoria das tarefas.
- A tarefa-resumo com uma só fonte para as datas: as subtarefas.
- O cálculo em cadeia (`Push`) sem mudança: só os itens com datas próprias têm dependências.

**Non-Goals:**

- Mais de um nível, dependências na tarefa-resumo, recolher e expandir na tela.
- Meu trabalho, avisos e termos de aceite das subtarefas, que vêm nas changes dessas telas.

## Decisions

### 1. `ParentId` no item, e a tarefa-resumo guardada

- `ScheduleItem` ganha `ParentId` (opcional): a tarefa em que o item está. A migração cria a coluna em `schedule_items`, com índice e chave estrangeira para a própria tabela, sem exclusão em cascata no banco (`ClientCascade`), como as dependências.
- O início, o fim, a duração, o progresso e a conclusão da tarefa-resumo ficam guardados nas colunas dela, recalculados pelo agregado a cada mudança (passo `Summarize`, depois do `Push`).
- Assim, quem lê o item (a API, a auditoria e, depois, Meu trabalho) não precisa saber se ele é resumo.
- Mudanças nas datas da tarefa-resumo entram na auditoria como "Alterou uma tarefa", como as tarefas empurradas.
- Alternativa: calcular a tarefa-resumo só na leitura, como a fase. Foi rejeitada, porque as colunas dela ficariam com valores velhos e cada leitor teria de repetir o cálculo.

### 2. O cálculo da tarefa-resumo

- Início e fim: o primeiro início e o último fim das subtarefas.
- Duração: os dias úteis entre o início e o fim, pelo `IBusinessDays.CountBusinessDays`.
- Progresso: `WeightedProgress` das subtarefas que são tarefas. Sem nenhuma (só marcos), 100% quando todos os marcos foram atingidos e 0% antes.
- Conclusão: o dia em que a última subtarefa chegou a 100% (`CompletedOn` mais recente), quando todas estão a 100%.
- Atraso: `IsLate` da tarefa-resumo é verdadeiro quando uma subtarefa está atrasada, como na fase.
- Na fase e no projeto, `WeightedProgress` e a contagem de tarefas atrasadas usam só os itens com datas próprias (subtarefas e tarefas sem subtarefas). A contagem de marcos atrasados inclui os marcos dentro de tarefas.

### 3. Regras no agregado

- **Nível:** o `parentId` precisa ser uma tarefa do cronograma, sem pai, na mesma fase do plano. Um marco não recebe subtarefas, e uma tarefa com subtarefas não vai para dentro de outra. Caso contrário, 400 no campo `parentId`.
- **Mudar a fase da tarefa-resumo** muda a fase das subtarefas.
- **Tarefa-resumo no `PUT` do item:**
  - mudam o nome, a fase e o responsável;
  - o início e a duração enviados são ignorados, porque vêm das subtarefas, e o progresso precisa vir vazio;
  - dependências recebem 400 em `predecessorIds`.
- **Progresso e novas datas** (`PUT .../progress`, `PUT .../dates`) numa tarefa-resumo: 400, como o progresso de um marco.
- **Dependências:** `CheckedPredecessors` recusa uma tarefa-resumo, como anterior ou como seguinte (400 em `predecessorIds`).
- **Primeira subtarefa:** quando um item entra numa tarefa sem subtarefas (ao ser incluído ou movido), as dependências da tarefa passam para ele, somadas às próprias. O ciclo é conferido antes, e o item é colocado e empurra as seguintes como numa alteração.
- **Excluir a última subtarefa:** as dependências dela voltam para a tarefa, que guarda as datas e o progresso calculados.
- **Sair da tarefa:** a subtarefa que vai para outra tarefa ou para a fase leva as próprias dependências. Se era a última, a tarefa fica sem dependências.
- **Excluir a tarefa-resumo:** exclui as subtarefas e as dependências delas, sem mover ninguém.
- **Ordem:** os itens de uma fase vêm em ordem de árvore. As tarefas e os marcos da fase vêm por `InOrder`, cada tarefa seguida das subtarefas também por `InOrder`.

### 4. API

- `ScheduleItemRequest` ganha `parentId` (opcional; vazio é direto na fase).
- `ScheduleItemResponse` ganha `parentId` e `hasSubtasks`. Na tarefa-resumo, `canUpdateProgress` é sempre falso.
- Os itens de `SchedulePhaseResponse` continuam numa lista só, em ordem de árvore, para a tela não remontar a hierarquia.
- A matriz de permissões não ganha ações: incluir e mover subtarefas é alterar o cronograma (`CanEditSchedule`), e o progresso da subtarefa segue o responsável dela (ADR-0043).

### 5. Tela

- **Lista** (`scheduleRows`): a subtarefa com um recuo a mais, o dobro do da tarefa (`var(--space-5)`). A tarefa-resumo tem o nome em peso médio, como a fase, e mostra as datas, a duração, o responsável e o progresso calculados.
- **Linha do tempo** (`ganttRows.ts`):
  - a subtarefa tem a tarefa como pai, e a tarefa-resumo vira barra de resumo com o mesmo visual da fase (`gantt-summary`);
  - como a fase, a tarefa-resumo não se arrasta nem se liga (o `taskbarEditing` já recusa o que não é tarefa ou marco com datas próprias);
  - as subtarefas se arrastam, se esticam e se ligam como as tarefas.
- **Formulário:**
  - "Nova tarefa" e o detalhe ganham "Dentro de", uma lista com as tarefas da fase escolhida que não são subtarefas, mais "Direto na fase";
  - escolher uma tarefa sem subtarefas mostra a ajuda: "Ela passa a ser resumo: as datas e o progresso vêm das subtarefas, e as dependências dela passam para esta subtarefa".
- **Detalhe da tarefa-resumo:**
  - o nome, a fase e o responsável se editam;
  - o início, o fim, a duração e o progresso aparecem sem editar, com "Calculado pelas N subtarefas";
  - tem o botão "Nova subtarefa".
- **Detalhe de uma tarefa sem subtarefas:** ganha "Nova subtarefa", que abre "Nova tarefa" com "Dentro de" já escolhido.
- **"Depende de"** não oferece tarefas-resumo.

### 6. Auditoria

- O `ParentId` é uma referência a outro item, mostrada pelo nome atual pela fonte `ScheduleItemReferenceNames` que já existe (ADR-0084), com o rótulo "Dentro de".

## Risks / Trade-offs

- [A passagem de dependências pode surpreender] → A ajuda no formulário avisa antes de salvar, e a mensagem de sucesso diz quantos itens foram empurrados.
- [O cálculo a cada mudança percorre todas as tarefas-resumo] → Os cronogramas da v1 têm dezenas ou poucas centenas de itens; o custo é desprezível perto da transação serializável.
- [A Linha do tempo com três níveis no celular] → A tabela estreita do celular já mostra só os nomes; o recuo do Syncfusion para o terceiro nível é pequeno.

## Migration Plan

- A migração `ScheduleSubtasks` cria `ParentId` nula: os cronogramas existentes continuam em dois níveis, sem mudança de dados.
- A reversão apaga a coluna.
