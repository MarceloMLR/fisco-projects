# Proposta: subtarefas (Cronograma-4)

## Why

O ADR-0019 deixou o cronograma em dois níveis e as subtarefas para depois da v1. Marcelo decidiu trazê-las para a v1: uma entrega grande costuma ser quebrada em partes com responsáveis e prazos próprios, e os cronogramas que vêm do MS Project e de planilhas já chegam assim. Ela entra logo depois da Linha do tempo, para a linha de base, o caminho crítico, o desfazer e a exportação já nascerem com as subtarefas.

## What Changes

- **Subtarefas em um nível** (ADR-0088): uma tarefa que está diretamente na fase pode ter tarefas e marcos dentro dela. Subtarefa não tem subtarefas, e marco não tem subtarefas. A subtarefa fica na fase da tarefa.
- **Tarefa-resumo:** a tarefa com subtarefas tem o início, o fim, a duração, o progresso, a conclusão e o atraso calculados delas, como a fase. Nela só se mudam o nome, a fase (que leva as subtarefas) e o responsável. Progresso e novas datas recebem 400.
- **Progresso e atrasos sem contar duas vezes:** na fase, no projeto e na contagem de atrasadas, contam as subtarefas e as tarefas sem subtarefas.
- **Dependências só entre quem faz o trabalho:**
  - subtarefas, tarefas sem subtarefas e marcos dependem uns dos outros, inclusive entre tarefas e fases diferentes;
  - a tarefa-resumo não entra em dependências (400);
  - a primeira subtarefa herda as dependências da tarefa, e a última, ao sair, as devolve.
- **Mover para dentro e para fora:** uma tarefa sem subtarefas pode ir para dentro de outra tarefa e sair dela; a tarefa-resumo não.
- **Excluir a tarefa-resumo** exclui as subtarefas e as dependências delas, sem mover as outras.
- **API:** o item ganha `parentId` ("Dentro de") no pedido e na resposta, e `hasSubtasks` na resposta; os itens de cada fase vêm em ordem de árvore (cada tarefa seguida das subtarefas).
- **Tela:**
  - Lista: as subtarefas abaixo da tarefa, com um recuo a mais; a tarefa-resumo com o nome em peso médio, como a fase;
  - Linha do tempo: a tarefa-resumo como barra de resumo, igual à da fase, com as subtarefas abaixo; só as subtarefas se arrastam e se ligam;
  - "Nova tarefa" ganha o campo "Dentro de" (opcional), e o detalhe de uma tarefa ganha "Nova subtarefa";
  - o detalhe da tarefa-resumo mostra as datas e o progresso calculados, sem editar, e quantas subtarefas tem;
  - "Depende de" não oferece tarefas-resumo.
- **Auditoria:** "Dentro de" aparece pelo nome da tarefa (ADR-0084).
- **ADR-0088** (novo, proposta), que substitui a linha "Subtarefas | depois" do ADR-0019.

## Non-goals

- Mais de um nível de subtarefas, e dependências na tarefa-resumo (ADR-0088).
- Recolher e expandir as tarefas-resumo na Lista e na Linha do tempo: elas ficam sempre abertas, como as fases hoje.
- Meu trabalho, avisos e termos de aceite das subtarefas: entram nas changes dessas telas, que já tratam a subtarefa como tarefa e o marco interno como marco.
- Linha de base, caminho crítico, desfazer e exportação: Cronograma-5 a 7, já com as subtarefas.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: subtarefas e tarefa-resumo, as dependências com subtarefas, a exclusão da tarefa-resumo, o progresso e os atrasos da fase e do projeto, e as subtarefas na Lista, na Linha do tempo e na auditoria.

## Impact

- **Backend (`Projects`):**
  - `ScheduleItem.ParentId` e a migração que cria a coluna, com índice;
  - no `Schedule`: as regras de nível, o cálculo da tarefa-resumo a cada mudança, a passagem das dependências, a recusa de dependências com tarefa-resumo, a exclusão em conjunto e a mudança de fase;
  - `ScheduleFigures` sem contar a tarefa-resumo duas vezes;
  - pedidos e respostas da API com `parentId` e `hasSubtasks`; a auditoria com o nome da tarefa em "Dentro de".
- **Contrato OpenAPI** e tipos do frontend.
- **Frontend:** a Lista, as linhas da Linha do tempo (`ganttRows.ts`), o formulário de tarefa, o detalhe, os textos e os testes.
- **Documentação:** ADR-0088, ADR-0019 marcado como substituído em parte, `docs/discovery.md` (subtarefas na v1), glossário, `CLAUDE.md` e roadmap (Cronograma-4, com as seguintes renumeradas).
