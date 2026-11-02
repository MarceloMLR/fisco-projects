## Why

Na Linha do tempo, as fases se recolhem pela seta; na Lista, só as tarefas-resumo, e quem troca de visão perde o que tinha recolhido. Marcelo pediu, em 01/11/2026, recolher e mostrar as fases também na Lista, como na Linha do tempo. Fora do roadmap, aprovado por Marcelo antes da implementação; correção da Cronograma-4, por isso Cronograma-4.2.

## What Changes

- Uma seta ao lado do nome de cada fase na Lista, como a das tarefas-resumo, que recolhe e mostra as tarefas e os marcos da fase, pelo clique ou pelo teclado; a linha da fase continua abrindo o diálogo dela para quem edita.
- O que está recolhido passa a ser o mesmo na Lista, na Linha do tempo e no Expandir, enquanto a aba está aberta.
- Sem mudança na API; a exportação continua com o cronograma inteiro.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: recolher as fases na Lista e o mesmo estado de recolhido nas duas visões.

## Non-goals

- Guardar o recolhido entre uma visita e outra (no endereço ou no perfil).
- "Recolher tudo" e "Mostrar tudo".

## Impact

- `web/src`: `scheduleRows` (`schedule.ts`), a coluna do nome da Lista (`ProjectSchedulePage`), o `ScheduleGantt` e o `ScheduleTimeline` com o recolhido vindo da aba; os testes.
- Documentação: `CLAUDE.md` e o roadmap.
