## Why

Quem vem do MS Project manda o cronograma ao cliente e à diretoria numa planilha, para filtrar, comentar e juntar a outros controles. O FiscoProjects exporta só o PDF, para todos, e o XML do MS Project, para a gestão. O Excel ficou para depois da v1 no ADR-0021 e é o item mais rápido do mapeamento do que o MS Project faz e o FiscoProjects não. GanttA-1, primeira change do grupo 14 (Aprimoramento do Gantt 1), a pedido de Marcelo.

## What Changes

- O menu "⋮" do cronograma ganha "Exportar para Excel", para todas as pessoas do projeto, como o PDF, também com o projeto encerrado.
- A planilha sai do cronograma que a tela já tem, montada no navegador, como a da Auditoria e a das Horas dos projetos (ADR-0099), sem rota nova na API.
- Aba "Cronograma", uma linha por fase, tarefa, subtarefa e marco, na ordem da tela, com o filtro do Excel:
  - o número da linha, a fase, o nome (a subtarefa recuada) e o tipo;
  - o responsável (a pessoa ou o cliente);
  - o início, o fim e a duração em dias úteis;
  - o progresso, a situação e os dias úteis de atraso;
  - as dependências, pelos números das linhas;
  - se está no caminho crítico;
  - o início, o fim e a diferença da linha de base, quando há.
- Aba "Projeto": o código, o nome, o cliente, o gerente, o período, o progresso, as tarefas e os marcos atrasados, a data da linha de base e quando a planilha foi gerada.
- A planilha não leva e-mails: eles ficam só no XML, que é da gestão (ADR-0136).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: exportar o cronograma para o Excel, e o menu "⋮" de quem não edita passa a ter o Excel ao lado do PDF.

## Non-goals

- Importar uma planilha do Excel como cronograma.
- Gerar a planilha no servidor ou mandá-la por e-mail.
- Levar para a planilha a linha do tempo desenhada (barras); o desenho continua no PDF.
- Levar os e-mails dos responsáveis, que continuam só no XML da gestão (ADR-0136).
- Escolher as colunas ou o período antes de exportar.

## Impact

- `web/src/format/xlsx.ts`: a célula de dia (sem hora) e a de porcentagem.
- `web/src/projects`: a montagem da planilha (`scheduleReport.ts`) e o item no "⋮" (`ScheduleBaseline.tsx`), com os textos em `pt-BR.json` e os testes.
- Documentação: `CLAUDE.md` e o roadmap (GanttA-1, ADR-0021, ADR-0099).
