# Proposta: progresso-das-tarefas (Cronograma-2)

## Why

Na Cronograma-1, só o administrador e o gerente mudam o progresso, editando a tarefa. Pelo ADR-0020 e pela matriz do ADR-0043, quem faz a tarefa atualiza o próprio progresso, com os atalhos "Iniciar" e "Concluir". Além disso, o marco ainda não tem os estados atingido ou não. Esta change completa o andamento do cronograma no dia a dia.

## What Changes

- **Quem atualiza o progresso** (ADR-0043):
  - o administrador e o gerente atualizam o de qualquer tarefa;
  - o líder técnico, o colaborador e o desenvolvedor, só o das tarefas de que são responsáveis;
  - o leitor, nunca;
  - o projeto encerrado é somente leitura (ADR-0016).
- **Atalhos do progresso** (ADR-0020, ADR-0086):
  - "Iniciar" numa tarefa a 0% pede o percentual, que a pessoa digita;
  - "Atualizar progresso" muda o percentual de uma tarefa iniciada;
  - "Concluir" põe 100% e grava a data de conclusão.
  - O progresso deixa o formulário da tarefa e vira uma ação própria, salva na hora. Assim, editar a tarefa não desfaz o percentual que o responsável informou ao mesmo tempo.
- **Marco atingido** (ADR-0020, ADR-0043, ADR-0086):
  - o administrador e o gerente marcam e desmarcam o marco como atingido;
  - o marco guarda a data em que foi atingido;
  - o marco não atingido depois da data fica atrasado, como a tarefa;
  - a fase com um marco atrasado fica atrasada, e as listas contam os marcos atrasados junto das tarefas.
- **Telas:**
  - na Lista, o marco mostra Atingido, Pendente ou Atrasado;
  - no detalhe da tarefa, o progresso com "Iniciar", "Atualizar progresso" e "Concluir" para quem pode atualizar;
  - no detalhe do marco, "Marcar como atingido" e "Desmarcar" para quem edita o cronograma;
  - na lista de Projetos, o prazo final diz as tarefas e os marcos atrasados.
- **ADR-0086** (novo, proposta): "Iniciar" pede o percentual, e o marco não atingido depois da data fica atrasado, pelas respostas de Marcelo.

## Non-goals

- As ações de Meu trabalho ("Iniciar", "Concluir", "Atualizar"): Síntese-1, que usa as mesmas rotas.
- O termo de aceite que marca o marco como atingido: grupo Documentos, links, acessos e termos (ADR-0035).
- Avisos de tarefa vencida ou marco atrasado: grupo Avisos.
- Linha do tempo, linha de base e caminho crítico: Cronograma-3 e Cronograma-4.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: quem atualiza o progresso, os atalhos, o marco atingido, o marco atrasado, a Lista e o detalhe da tarefa e do marco.
- `projetos`: o prazo final da lista de projetos conta os marcos atrasados.

## Impact

- **Backend (`Projects`):**
  - `ScheduleItem.ReachedOn`, `Schedule.UpdateProgress` e `Schedule.SetReached`;
  - o atraso do marco em `ScheduleFigures`;
  - a permissão `CanUpdateOwnProgress`;
  - as rotas `PUT /schedule/items/{itemId}/progress` e `/reached`;
  - o progresso opcional na alteração do item;
  - `lateMilestones` e `canUpdateProgress` nas respostas;
  - a migração `MilestoneReached`;
  - testes do domínio, dos casos de uso e da API, com a matriz de permissões.
- **Contrato OpenAPI** e tipos do frontend.
- **Frontend:** a Lista, o detalhe da tarefa e do marco, o diálogo do percentual, a lista de Projetos, os textos e os testes.
- **Documentação:** glossário, ADR-0086 e roadmap.
