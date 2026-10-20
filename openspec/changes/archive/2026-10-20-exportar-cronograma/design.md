# Design: exportar o cronograma (Cronograma-7)

## Context

O servidor calcula todas as datas (ADR-0051), e o `GET /schedule` já traz fases, itens, dependências, dias não úteis, linha de base e caminho crítico. O ADR-0021 decidiu o PDF pela página de impressão do navegador, com o período e a escala escolhidos pela pessoa, e o XML do MS Project. O ADR-0051 recomenda gerar o XML no servidor. O ADR-0043 deixa exportar para todas as pessoas do projeto.

## Goals / Non-Goals

**Goals:**

- Um XML que o MS Project abre com as mesmas fases, tarefas, marcos, dependências e datas.
- Uma página de impressão legível em A4 paisagem, em claro, que quebra entre as linhas.

**Non-Goals:**

- PDF no servidor, Excel e .mpp (ADR-0021).

## Decisions

### 1. XML do MS Project (MSPDI) no servidor

- `MsProjectExport.Of(project, schedule, figures, days, ownerNames)` monta o documento com `System.Xml.Linq`, no namespace `http://schemas.microsoft.com/project`.
- **Projeto:** o nome (código e nome), o início e o fim, o calendário 1, 08:00 a 17:00, 480 minutos por dia.
- **Calendário** "Calendário da instalação":
  - os dias da semana com trabalho são os que têm algum dia útil no período, das 08:00 às 12:00 e das 13:00 às 17:00;
  - os feriados do período em dia de trabalho vão como exceções (`WeekDay` com `DayType` 0 e `TimePeriod`), o formato que todas as versões leem.
- **Tarefas:**
  - a tarefa 0 é o resumo do projeto;
  - cada fase é uma tarefa-resumo de nível 1, com os itens em nível 2 e as subtarefas em nível 3, na ordem da Lista;
  - cada tarefa leva o início às 08:00, o fim às 17:00, a duração em horas de dias úteis (`PT{8×n}H0M0S`, formato em dias) e o progresso;
  - o marco tem duração zero, com o início e o fim no fim do dia, como o MS Project põe um marco depois de uma tarefa;
  - para o MS Project não mover as datas ao abrir, cada item com datas próprias leva a restrição "Não iniciar antes de" (`ConstraintType` 4) na data de início;
  - as dependências vão como `PredecessorLink` do tipo 1 ("termina, então começa"), sem atraso;
  - com a linha de base salva, cada item dela leva `Baseline` número 0, com o início e o fim.
- **Recursos e atribuições:** cada responsável vira um recurso de trabalho, atribuído às suas tarefas.
- **Rota:** `GET /api/projects/{id}/schedule/ms-project` responde `application/xml`, com o arquivo `{código}-cronograma.xml`. Quem vê o cronograma exporta, e quem não está no projeto recebe 404 (ADR-0043). Entra na matriz de permissões como leitura.

- **Alternativa:** a exportação do Syncfusion. Recusada: o ADR-0051 manda gerar no servidor, para não depender do componente.
- **Alternativa:** o MPXJ. Recusado: exige IKVM e licença LGPL, para um formato documentado que se escreve à mão.

### 2. PDF pela página de impressão

- **Diálogo "Exportar em PDF" no "⋮":**
  - o período vem com o início e o fim do cronograma;
  - a escala vem com a escala atual;
  - "Abrir para imprimir" abre `/projects/{id}/schedule/print?from=…&to=…&scale=…&baseline=…&critical=…` numa aba nova.
- **Página de impressão:**
  - a rota fica fora da estrutura das telas (sem menu nem barra), mas exige sessão, e usa o mesmo `GET /schedule`;
  - sempre no tema claro, porque é para papel;
  - cabeçalho: o código, o nome, o cliente, o gerente, o período escolhido e "Gerado em" com o dia;
  - a tabela tem uma linha por fase e item: nome, início, fim, progresso, "Depende de" e a linha do tempo da linha;
  - a escala fica no `thead`, que o navegador repete em cada página;
  - cada linha desenha a própria faixa em SVG (`PrintTimeline`), então a quebra de página cai entre as linhas (`break-inside: avoid`);
  - as dependências ficam de fora: o desenho por linha não liga uma linha à outra, e a coluna "Depende de" diz de quem cada item depende;
  - o desenho segue o Gantt do design system (`g-bar`, `g-progress`, `g-summary`, `g-milestone`, `g-baseline`, `g-bar--critical`, `g-nonworking`, `g-today`) com os tokens, sem o Syncfusion (ADR-0051);
  - `@page { size: A4 landscape }`, e o botão "Imprimir" (`window.print`) some na impressão.
- **Corte pelo período:** os itens fora do período aparecem na tabela, mas a barra é cortada nas bordas, e só aparece o pedaço que cai dentro dele.

### 3. "⋮" para todos

- O menu passa a aparecer para quem vê o cronograma.
- "Refazer" e a linha de base aparecem só para quem edita; as duas exportações aparecem para todos.

## Risks / Trade-offs

- **[MS Project recalcula]:** sem a restrição, o MS Project moveria os itens pelo calendário dele. → A restrição "Não iniciar antes de" e o calendário exportado mantêm as datas. O teste confere a estrutura lida de volta; a abertura no MS Project fica para Marcelo conferir.
- **[Impressão no navegador]:** cada navegador quebra a página do seu jeito. → Uma linha por `tr`, sem altura fixa, e a escala no `thead`.
