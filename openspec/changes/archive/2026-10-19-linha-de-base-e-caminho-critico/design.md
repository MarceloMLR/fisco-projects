# Design: linha de base e caminho crítico (Cronograma-5)

## Context

O agregado `Schedule` guarda fases, itens e dependências, e o servidor calcula todas as datas em dias úteis (ADR-0051): uma tarefa começa no dia útil seguinte ao fim das anteriores, e um marco, no mesmo dia (`EarliestAfter`). A tela desenha o que a API manda, e o Syncfusion só exibe (ADR-0051).

O ADR-0021 decidiu uma linha de base por projeto, salva e atualizada pelo gestor, com os dias de diferença por tarefa, e o caminho crítico destacado. O ADR-0071 pôs "Exibir" (liga e desliga os dois) e o "⋮" na barra da aba. O ADR-0067 deixou o caminho crítico em vermelho, com o concluído em verde. O design system tem os tokens `gantt-critical-bar`, `gantt-critical-border` e `gantt-baseline` e a legenda com "Caminho crítico" e "Linha de base".

## Goals / Non-Goals

**Goals:**

- Salvar e atualizar a linha de base, com a diferença por item e do projeto em dias úteis.
- Calcular o caminho crítico no servidor e destacá-lo na Linha do tempo.
- Auditar a linha de base sem um registro por tarefa.

**Non-Goals:**

- Histórico de linhas de base, a diferença na lista de Projetos e na Visão geral, uma coluna na Lista, desfazer e exportar (proposta).

## Decisions

### 1. Linha de base como entidade própria do agregado

- **`ScheduleBaseline`**:
  - id igual ao do projeto, como o `Schedule`;
  - `SavedOn`: o dia em que foi salva, no fuso da instalação;
  - `Start` e `End`: o início e o fim do projeto naquele dia;
  - os itens, em `ScheduleBaselineItem`: o id do item, o início e o fim.
- **Salvar** (`Schedule.SaveBaseline(today)`):
  - cria a linha de base, ou atualiza a existente;
  - os itens são trocados pelos atuais, inclusive as tarefas-resumo, com as datas calculadas delas;
  - sem tarefas nem marcos, recusa.
- **Item excluído:** o `ScheduleBaselineItem` não tem chave estrangeira para o item. A linha de base é uma fotografia, e só os itens que ainda existem são lidos. O início e o fim do projeto ficam no próprio `ScheduleBaseline`, então excluir uma tarefa não muda o fim salvo.
- **Auditoria:**
  - o `ScheduleBaseline` é auditado pelo nome do projeto (`HasAuditLabelReference`), com `SavedOn`, `Start` e `End` antes e depois;
  - o `ScheduleBaselineItem` fica fora (`ExcludeFromAudit`), para salvar não gerar um registro por tarefa.

- **Alternativa:** colunas `BaselineStart` e `BaselineEnd` em `schedule_items`. Recusada por três motivos:
  - salvar alteraria todas as linhas da tabela de itens;
  - a auditoria teria de esconder essas colunas;
  - o fim do projeto na linha de base mudaria ao excluir uma tarefa.

### 2. Diferença em dias úteis, com sinal

`ScheduleBaseline.DeviationOf(end, baselineEnd, days)`:
- se o fim atual está depois do fim salvo, a diferença é positiva: `CountBusinessDays(baselineEnd + 1, end)`;
- se está antes, é negativa: `-CountBusinessDays(end + 1, baselineEnd)`;
- se é o mesmo dia, é zero.

A conta é a mesma dos dias de atraso: conta o último dia e não conta o primeiro. Vale para cada item com linha de base e para o fim do projeto.

### 3. Caminho crítico pelo método do caminho crítico, nas datas atuais

`ScheduleCriticalPath.Of(items, dependencies, days)`:

1. **Itens considerados:** só os que têm datas próprias, sem as tarefas-resumo. O fim do projeto, `F`, é o maior fim entre eles.
2. **Último fim de cada item (`LF`)**, em ordem topológica reversa:
   - sem seguintes, `LF = F`;
   - com seguintes, o menor entre os limites que cada seguinte `S` impõe:
     - se `S` é marco, o próprio último início de `S`;
     - se `S` é tarefa, o dia útil anterior ao último início de `S`;
   - é o inverso de `EarliestAfter`: tarefa no dia útil seguinte, marco no mesmo dia.
3. **Último início de cada item (`LS`):** para tarefa, `LF` menos a duração menos 1 dia útil; para marco, o próprio `LF`.
4. **Folga e caminho crítico:** a folga é `CountBusinessDays(End + 1, LF)`, e o item é crítico com folga zero.

O cálculo usa as datas que o servidor já calculou. O empurrar garante que nenhum item começa antes do que as dependências permitem, então a folga nunca é negativa. Uma tarefa concluída continua crítica se as datas a põem no caminho, como no MS Project. A tarefa-resumo não entra porque não tem dependências (ADR-0088): as subtarefas dela entram como qualquer item.

- **Alternativa:** ligar o caminho crítico do próprio Syncfusion (`enableCriticalPath`). Recusada: o ADR-0051 manda calcular no servidor, e o Syncfusion não conhece os feriados nem a regra do marco.

### 4. API

- **Item (`ScheduleItemResponse`):**
  - `critical`;
  - `baselineStart`, `baselineEnd` e `baselineDeviation`, nulos para quem não estava na linha de base.
- **Cronograma (`ScheduleResponse`):** `baseline: { savedOn, start, end, deviation }`, ou nulo sem linha de base.
- **`PUT /api/projects/{id}/schedule/baseline`:**
  - sem corpo, responde 204;
  - recusa conforme a ordem de sempre: 404, 403, 409 e 400;
  - entra na matriz de permissões do cronograma (`ScheduleTests.Actions`).

O `GetSchedule` já lê o calendário para os dias não úteis e os dias de atraso, e o mesmo calendário serve ao caminho crítico e às diferenças.

### 5. Tela

- **Estado de "Exibir":** a página guarda na memória da tela o que está ligado, com tudo ligado no início, como o canvas mostra no Expandir. A Linha do tempo normal e a do Expandir compartilham esse estado.
  - **Barra da aba:** um botão "Exibir" (ícone de olho, seta) abre um menu com duas opções marcáveis (`menuitemcheckbox`) e o `Checkbox` do design system.
  - **Expandir:** as duas caixas ficam direto no cabeçalho.
- **"⋮" (`Linha de base e exportação`):**
  - só aparece para quem edita, porque por enquanto só tem a linha de base;
  - "Salvar linha de base" salva direto;
  - "Atualizar linha de base" pede confirmação, dizendo de que dia é a linha de base que será trocada.
- **`ScheduleGantt`:**
  - **linha de base:** o `ganttRows` passa o início e o fim salvos (`baselineStart` e `baselineEnd`), e o Syncfusion os desenha (`renderBaseline`, com a cor `--gantt-baseline` pelo CSS);
  - **caminho crítico:** a classe `fp-gantt-critical` nas linhas críticas pinta a barra com `--gantt-critical-bar` e a borda com `--gantt-critical-border`, sem mudar a parte concluída;
  - os dois só aparecem quando ligados em "Exibir".
- **Legenda:** ganha "Caminho crítico" e "Linha de base" quando ligados, e a linha de base só quando há uma salva.
- **Resumo:** depois do progresso e dos atrasos, "Linha de base de 16/10/2026" com a diferença do fim do projeto (`DueDate`):
  - fim depois da linha de base: "Fim 2 dias úteis depois", em `soon` (ampulheta e âmbar). É escorregamento contra o plano, não atraso contra hoje, que fica com o vermelho;
  - na data ou antes: "Fim na data da linha de base" ou "Fim 1 dia útil antes", em `met`.
- **Detalhe:** com a linha de base salva, a seção "Linha de base" mostra:
  - o início e o fim salvos (no marco, o dia) e a diferença: "2 dias úteis depois", "Na data da linha de base" ou "1 dia útil antes";
  - para o item incluído depois, "Incluída depois da linha de base".

### 6. Auditoria na tela

O tipo `ScheduleBaseline` entra na lista de tipos do filtro ("Linhas de base"), com:
- as ações "Salvou a linha de base" e "Atualizou a linha de base";
- os campos "Salva em", "Início" e "Fim", com os dias formatados.

### 7. Dados de demonstração

No plano do ERP-FIN:
- `baseline: true` faz a gerente salvar a linha de base depois de montar o cronograma;
- `movedTo` em "Parametrizar regras de ICMS" faz a gerente adiar o início dela em seguida (`PUT .../dates`), o que empurra "Testes integrados" e as seguintes e deixa o fim do projeto depois da linha de base.

## Risks / Trade-offs

- **[Caminho crítico com datas manuais]:** uma tarefa que começa depois do necessário tem folga antes dela, não depois, e não fica crítica mesmo numa cadeia longa. → É a regra do método nas datas atuais; os cenários do spec cobrem a cadeia, a folga com feriado e as subtarefas.
- **[Desenho da linha de base pelo Syncfusion]:** o desenho depende do componente de terceiros. → Teste do `ScheduleGantt` no jsdom, com a linha de base ligada e desligada, e conferência no navegador.
- **[Desempenho]:** o caminho crítico percorre itens e dependências uma vez por leitura. Cronogramas têm de dezenas a poucas centenas de itens, e o cálculo é linear no número de dependências.
