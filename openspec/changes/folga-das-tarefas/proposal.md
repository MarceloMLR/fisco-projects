## Why

O caminho crítico diz quais tarefas não podem atrasar, mas não diz quanto as outras podem. Nos projetos fiscais, a gestão decide todo dia o que segurar e o que adiantar, e precisa saber, por exemplo, que a integração da NF-e tem 4 dias úteis de folga e a parametrização do ICMS nenhuma. No MS Project isso é a folga total. O servidor já calcula, para o caminho crítico, o dia mais tarde em que cada item pode terminar; falta mostrá-lo. GanttB-1, do grupo 15 (Aprimoramento do Gantt 2), a pedido de Marcelo.

## What Changes

- O servidor diz, para cada tarefa e marco com datas próprias, a folga: quantos dias úteis do calendário da instalação ele pode terminar depois sem mudar o fim do projeto, pelas dependências com o tipo e o intervalo (ADR-0018, ADR-0021, ADR-0137). O caminho crítico continua sendo os itens sem folga; a tarefa-resumo não tem folga própria.
- Na tela:
  - o detalhe da tarefa e do marco diz "4 dias úteis de folga" ou "Sem folga: no caminho crítico";
  - na Linha do tempo, com o caminho crítico ligado em "Exibir", cada item com folga tem uma linha fina depois da barra até o último dia em que pode terminar, e a legenda ganha "Folga";
  - o Excel ganha a coluna "Folga (dias úteis)".
- O `GET /schedule` traz `float` em cada item (nulo para a tarefa-resumo).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: a folga de cada item no cálculo, no detalhe, na Linha do tempo e no Excel.

## Non-goals

- A folga livre (quanto um item pode atrasar sem mover o seguinte), que o MS Project também tem.
- Filtrar pela folga ou avisar quando ela acaba.
- Folga na página de impressão (PDF).

## Impact

- `src/Modules/Projects`: o `ScheduleCriticalPath` devolve a folga de cada item junto com o caminho crítico; o `GET /schedule` a leva; os testes.
- `web/src`: o detalhe, a Linha do tempo (a linha da folga e a legenda), o Excel, os textos, os tipos gerados e os testes.
- Documentação: `CLAUDE.md`, `docs/glossario.md` e o roadmap (GanttB-1, ADR-0021, ADR-0051, ADR-0137).
