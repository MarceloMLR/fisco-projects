# Proposta: ajustes das telas (Cronograma-4.1)

## Why

Ao conferir as Cronograma-3, 3.1 e 4, Marcelo encontrou pontos que atrapalham o uso: uma barra de rolagem pequena ao lado das abas do projeto; tarefas e marcos atrasados que não dizem há quantos dias estão atrasados; uma linha da Linha do tempo que não abre o detalhe de novo depois de fechado; uma mensagem de CNPJ inválido difícil de entender; subtarefas pouco visíveis na Lista e no detalhe, com o "Nova subtarefa" menor que o "Salvar" ao lado; e o `npm run demo:data`, que mostra o endereço da API (5175), onde o build pode estar desatualizado, e não o das telas (5173).

## What Changes

- **Abas do projeto sem barra de rolagem:** o traço da aba escolhida passava 1 px da linha das abas e criava uma rolagem vertical. As abas continuam rolando para o lado no celular, se não couberem.
- **Dias de atraso** (ADR-0020, ADR-0086):
  - a API passa a dizer, em cada tarefa e marco, há quantos dias úteis está atrasado (`lateDays`): os dias úteis do dia seguinte ao fim até hoje, no calendário da instalação; na tarefa-resumo, os da subtarefa mais atrasada (ADR-0088);
  - a tela mostra "1 dia útil de atraso" ou "N dias úteis de atraso", o mesmo texto das telas aprovadas: na coluna Fim da Lista (embaixo da data da tarefa e sozinho no marco), no detalhe e na dica do ícone na Linha do tempo;
  - quando o atraso começou num dia não útil (uma tarefa da sexta, vista no sábado), ainda não há dia útil perdido, e a tela continua dizendo "Atrasada" na tarefa e "Atrasado" no estado do marco.
- **Reabrir pela Linha do tempo:** ao fechar o detalhe de uma tarefa ou de uma fase, a Linha do tempo deixa de manter a linha escolhida, e escolher a mesma barra ou linha de novo abre o detalhe outra vez.
- **CNPJ inválido com o motivo exato** (ADR-0015): o campo diz se falta caractere ("O CNPJ tem 14 caracteres"), se os dois últimos dígitos não batem com os anteriores ou se todos os caracteres são iguais.
- **Subtarefas mais claras** (ADR-0088):
  - na Lista, a tarefa-resumo ganha a seta de recolher e mostrar as subtarefas, como na Linha do tempo;
  - no detalhe de uma tarefa direto na fase, uma seção "Subtarefas" lista as subtarefas, cada uma abrindo o próprio detalhe, e traz o "Nova subtarefa" para quem edita;
  - os botões do rodapé do detalhe ("Excluir" e "Salvar") ficam do mesmo tamanho.
- **Endereço das telas na carga de demonstração** (ADR-0089): ao terminar, o `npm run demo:data` diz para abrir as telas em `http://localhost:5173`, com o `npm run dev`.

## Non-goals

- Dias de atraso das fases e do projeto: a fase continua dizendo "Atrasada", e o projeto, quantas tarefas e marcos estão atrasados. O desvio do projeto contra a linha de base vem na Cronograma-5.
- Recolher as fases na Lista: as fases continuam como linhas de grupo sempre abertas.
- Redirecionar a porta 5175 para a 5173 no desenvolvimento: Marcelo preferiu abrir as telas pela 5173.
- Avisos de tarefa vencida: vêm na change dos avisos (ADR-0037).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: os dias de atraso das tarefas e dos marcos, na API e na tela; reabrir o detalhe pela Linha do tempo; recolher as subtarefas na Lista e a seção Subtarefas no detalhe.
- `clientes`: o motivo exato do CNPJ inválido.
- `projetos`: as abas do cabeçalho sem barra de rolagem.
- `dados-de-demonstracao`: o endereço das telas no fim da carga.

## Impact

- **Backend (`Projects`):** `ScheduleItem.DaysLate`, com o `IBusinessDays` do `Calendar`, e `ScheduleFigures.LateDaysOf`, com a tarefa-resumo; o `GetSchedule` e a resposta do item com `lateDays`. Sem migração.
- **Contrato OpenAPI** e tipos do frontend.
- **Frontend:** `ProjectHeader` (abas), `ProjectSchedulePage` (Lista com a seta, a coluna Fim e o detalhe com a seção Subtarefas), `ScheduleStatus` e `ScheduleGantt` (textos do atraso e a seleção limpa ao fechar), `ClientDialog` e `format/cnpj.ts` (o motivo do CNPJ), os textos e os testes.
- **Dados de demonstração:** a mensagem do fim da carga (`web/scripts/demo/load.ts`).
- **Documentação:** roadmap (a linha da Cronograma-4.1 com os novos itens) e `CLAUDE.md` (o `lateDays` e a seção Subtarefas).
