## Why

Num cronograma de implantação, com dezenas de tarefas, a gestão e cada pessoa da equipe querem ver só o que interessa naquele momento: o que está atrasado, o que passou da data-limite, o caminho crítico, as tarefas de uma pessoa ou as das próximas duas semanas. Hoje a Lista e a Linha do tempo mostram sempre tudo, e é preciso rolar e procurar. No MS Project isso são os filtros. GanttA-3, do grupo 14 (Aprimoramento do Gantt 1), a pedido de Marcelo.

## What Changes

- O "Filtrar" (um ícone, para a barra continuar numa linha) na barra da aba Cronograma e no Expandir, para todas as pessoas do projeto, com um filtro por vez:
  - Atrasadas: as tarefas e os marcos atrasados (ADR-0020, ADR-0086);
  - Datas-limite estouradas: os itens em aberto que passaram da data-limite (GanttA-2);
  - Caminho crítico (ADR-0021);
  - Minhas tarefas: as de quem está na sessão;
  - Próximas duas semanas: as tarefas e os marcos em aberto que acontecem de hoje até 13 dias depois;
  - Por responsável: uma pessoa do cronograma, o cliente do projeto (ADR-0102) ou "Sem responsável".
- O filtro vale para a Lista, a Linha do tempo e o Expandir: aparecem os itens que passam nele, com a fase e, para uma subtarefa, a tarefa em que está, para não perder o lugar; as setas das dependências ficam só entre os que aparecem. As datas, o período da Linha do tempo e o resumo da aba não mudam.
- O ícone de filtro fica marcado com um filtro escolhido, o resumo diz o filtro e quantos itens aparecem ("Atrasadas · 4 de 20 tarefas e marcos"), o rodapé da Lista também, e "Limpar filtro" volta a mostrar tudo. Sem nenhum item, a tela diz "Nenhuma tarefa ou marco neste filtro."
- O filtro fica no endereço (`?filter=`), como a visão e a escala, para voltar a ele e mandar o link.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: os filtros da Lista, da Linha do tempo e do Expandir.

## Non-goals

- Combinar filtros, salvar filtros com nome ou criar filtros próprios.
- Filtrar a exportação (PDF, Excel e XML continuam com o cronograma inteiro).
- Mudar a API: o cronograma já vem inteiro, e o filtro é só da tela.

## Impact

- `web/src/projects`: as regras dos filtros em `scheduleFilter.ts`, o botão `ScheduleFilterMenu`, a aba e o Expandir, a Linha do tempo com o filtro, os textos e os testes.
- Documentação: `CLAUDE.md`, `docs/glossario.md` e o roadmap (GanttA-3, ADR-0071, ADR-0134).
