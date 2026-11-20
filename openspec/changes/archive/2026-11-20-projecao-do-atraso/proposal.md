## Why

Uma tarefa atrasada hoje aparece em vermelho e, na Visão geral, com "bloqueia Configurar plano de contas", mas ninguém sabe o que ela faz com o go-live: o gerente segue as setas da Linha do tempo e conta os dias à mão para o status report. Status-2, do grupo 18 (Status do projeto), com as regras da projeção do atraso do ADR-0164 (aprovado por Marcelo em 19/11/2026); é a base dos riscos do relatório de status (ADR-0165).

## What Changes

- Tarefa atrasada, para a projeção (ADR-0164): a tarefa em aberto, com datas próprias, que já devia ter terminado (ADR-0020) ou que devia ter começado e está em 0%.
- O servidor projeta cada tarefa atrasada, sem gravar nada: o que falta (a duração vezes a parte que falta pelo progresso, em dias úteis, arredondada para cima, no mínimo 1 dia) a partir de hoje, ou do próximo dia útil quando hoje não é útil; as tarefas e os marcos que dependem dela andam pelas regras das dependências (ADR-0137), só quando não cabem mais, e o que está concluído não anda.
- Para cada tarefa atrasada, a projeção diz o novo fim dela, os marcos não atingidos que mudariam, com o dia de hoje, o dia projetado e quantos dias úteis andam, e o fim do projeto, quando muda.
- O `GET /schedule` traz `projection` em cada tarefa atrasada (o Minhas tarefas também, porque usa a mesma leitura), e o detalhe da tarefa ganha a seção "Projeção do atraso", para todos que veem o cronograma, com o aviso de que nada muda até quem edita decidir (ADR-0051, ADR-0140).
- Na Visão geral, "Precisa de atenção" mostra o que cada tarefa atrasada empurra no lugar do "bloqueia ..." ("empurra o marco Go-live em 3 dias úteis"), e passa a listar também a tarefa que devia ter começado e está em 0% ("Devia ter começado em 17/11"). **BREAKING** no contrato interno: `attention.tasks[].blocks` sai e entram `start` e `projection`.
- Projeto encerrado não projeta: o cronograma não anda mais.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: a regra da projeção do atraso, a projeção pela API e a seção "Projeção do atraso" no detalhe da tarefa.
- `visao-geral-do-projeto`: "Precisa de atenção" com o que cada tarefa atrasada empurra e com a tarefa que devia ter começado.

## Non-goals

- Gravar a projeção ou reprogramar o cronograma sozinho (ADR-0051, ADR-0164 B2): quem edita decide, alterando a tarefa ou movendo o cronograma (ADR-0140).
- Projetar pelo prazo dos chamados (ADR-0025) ou pelas ausências dos responsáveis (ADR-0141).
- Mudar o que conta como atrasado no resto das telas (a marca vermelha, a contagem, o filtro "Atrasadas", o Meu trabalho e o Minhas tarefas continuam pelo ADR-0020).
- O relatório de status (Status-4), que vai usar esta projeção.
- Avisos por e-mail, Teams ou sino quando uma tarefa passa a empurrar um marco.

## Impact

- `src/Modules/Projects`: a regra no domínio (`ScheduleProjections`, num lugar só, sobre cópias dos itens, com o `ScheduleLinks.EarliestStart`), a leitura do cronograma (`ScheduleView.Projections`, `ScheduleItemResponse.Projection`) e a Visão geral (`OverviewLateTask` com `Start` e `Projection` no lugar de `Blocks`).
- `web/src`: a seção no detalhe da tarefa (`web/src/projects`), as linhas de "Precisa de atenção" (`overview.ts`), os textos e o contrato da API (`npm run api:update`).
- Testes do domínio, da API e de tela; `CLAUDE.md`, `docs/glossario.md` e o roadmap.
