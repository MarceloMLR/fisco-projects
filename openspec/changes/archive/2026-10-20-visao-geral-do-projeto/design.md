# Design: visão geral do projeto (Síntese-2)

## Context

- **ADR-0073:** a aba Visão geral responde "está no prazo?": o andamento real contra o previsto (pelo cronograma, ou pela linha de base quando houver), o prazo final com o desvio em dias úteis contra a linha de base, os chamados abertos com os vencidos e os que aguardam o solicitante, o próximo marco com os dias úteis que faltam, as fases e os marcos numa linha do tempo menor, "Precisa de atenção" e a equipe por time; "sem cronograma" sem cronograma.
- **ADR-0071:** a linha do tempo menor da Visão geral também se ajusta ao espaço.
- **ADR-0045:** os termos de aceite só para a gestão e para a pessoa do termo; **ADR-0044:** todos do projeto veem os chamados.
- **ADR-0028:** o chamado no fornecedor parado há 5 dias úteis (`VendorTickets.StaleDaysOf`).
- **O que já existe:**
  - `ScheduleFigures` (datas, progresso, fases, atraso), `ScheduleFigures.LateDaysOf`, `ScheduleCriticalPath`, `ScheduleBaseline.CompareWith` e `DeviationOf`;
  - o resumo do encerramento (`GetProjectClosing`), que já junta os chamados (`IProjectClosing`) e os aceites (`IProjectAcceptances`) pelos contratos do `Projects.Contracts`;
  - `TicketDeadlines.FiguresOf`, `TicketAccess.TeamsOf`, `VendorTickets`;
  - `AcceptanceAccess.Sees`;
  - a aba Visão geral atual (descrição e equipe) e o `Card` da Síntese-1.
- **Canvas:** ProjetoVisaoGeral.

## Goals / Non-Goals

**Goals:** os quatro números, a linha do tempo menor, "Precisa de atenção" e a equipe, calculados no servidor; a tela no computador e no celular.

**Non-Goals:** gráficos de evolução (depois, ADR-0073); editar o cronograma pela visão geral.

## Decisions

### 1. API (`GET /api/projects/{id}/overview`, `GetProjectOverview`)

- No `Projects`, dono do projeto e do cronograma, como o resumo do encerramento: quem vê o projeto recebe a visão geral; os outros, 404.
- **Andamento (`ScheduleProgressPlan`, Domain):**
  - o real é o progresso do projeto (`ScheduleFigures.Progress`);
  - o previsto, a média ponderada pela duração, da parte de cada tarefa com datas próprias (sem a tarefa-resumo, ADR-0088) que deveria estar feita até o fim de hoje: os dias úteis do início até hoje, sobre a duração, entre 0 e 100%;
  - com linha de base, as datas e a duração de cada tarefa na linha de base; as tarefas incluídas depois dela ficam fora do previsto;
  - arredondado como o progresso (`WeightedProgress`).
- **Prazo final:** o fim do cronograma; com linha de base, o fim dela e o desvio (`DeviationOf`).
- **Próximo marco:** o primeiro marco não atingido pelo dia, um atrasado primeiro; os dias úteis que faltam ou os de atraso (`DaysLate`).
- **Linha do tempo:** o período do cronograma, as fases com datas (início, fim, progresso, atrasada, no caminho crítico se algum item dela está, e as datas da linha de base pelos itens dela), os marcos (dia, atingido, atrasado), em ordem de início, e os dias não úteis do período.
- **Chamados (`IProjectTickets`, contrato novo do `Projects.Contracts`, que o `Tickets` implementa):**
  - abertos (fora de Resolvido e Fechado), o total, os vencidos (o prazo que corre, vencido), os que aguardam o solicitante e os abertos de cada time;
  - para a atenção: os abertos com o prazo vencido ou até o próximo dia útil, e os sem responsável, com o número, o título, o time, o responsável, as interações e o prazo;
  - os chamados em fornecedores parados (`VendorTickets.StaleDaysOf`), com o fornecedor, o código, o nosso chamado e os dias úteis sem atualização.
- **Aceites (`IProjectAcceptances.PendingForAsync`, que o `Documents` implementa):** os pedidos pendentes do projeto que a pessoa vê (`AcceptanceAccess.Sees`), com o título, a pessoa, o pedido e o marco.
- **Atenção, na ordem:** as tarefas atrasadas (a mais atrasada primeiro, com o responsável, o progresso e os nomes das tarefas e marcos que dependem dela), os chamados (vencidos, depois pelo prazo, depois os sem responsável), os aceites pendentes (o pedido mais antigo primeiro) e os chamados em fornecedores (o mais parado primeiro).
- **Equipe:** Gestão (gerente e líder técnico), Funcional (colaboradores) e Desenvolvimento (líder técnico e desenvolvedores), com as pessoas e os chamados abertos de cada time que atende.

### 2. Tela

- **Aba Visão geral (`ProjectOverview`):** na altura da janela no computador (ADR-0066): os quatro números numa faixa (fp-kpis); embaixo, à esquerda, "Fases e marcos" e "Equipe"; à direita, "Precisa de atenção", que rola por dentro.
- **Números:** Andamento ("62%", "previsto 70%" e a barra com a parte concluída em verde e a marca do previsto); Prazo final ("13/11", "2026", o desvio pelo `DueDate` e "linha de base 11/11"); Chamados abertos ("7", "de 9", "1 vencido" e "2 aguardando solicitante"); Próximo marco (o nome, "03/11 · em 11 dias úteis"). Sem cronograma, "Sem cronograma".
- **Linha do tempo menor (`OverviewTimeline`):** desenhada pela própria tela, sem o Syncfusion, como a página de impressão: a coluna das fases e dos marcos e, ao lado, o período em proporção da largura, com os fins de semana e feriados sombreados, a linha de hoje, as barras das fases com o concluído em verde, a atrasada e a do caminho crítico nas cores do Gantt, a linha de base sob a barra e os marcos em losango; "Abrir cronograma" leva à aba.
- **Equipe:** os três times, com o resumo e os avatares, e "Membros".
- **Atenção:** o ícone do item, o título como link (a tarefa no cronograma pelo `?item=`, o chamado, o termo pelo `?term=`), o detalhe e o prazo (`DueDate`).
- **Descrição:** sai da Visão geral, como no canvas; continua em "Editar projeto".
- **Celular:** a página rola, com os números em duas colunas, a atenção, a linha do tempo e a equipe.

### 3. Demonstração

- A demonstração já tem o que a visão geral do ERP-FIN mostra: a linha de base com o adiamento, tarefas atrasadas, chamados abertos, sem responsável e aguardando, um aceite pendente e um chamado no fornecedor; o plano ganha o chamado no fornecedor parado, registrado com a data de alguns dias antes, se a API permitir, ou fica como está.

## Risks / Trade-offs

- **[Previsto com cronograma desatualizado]** Sem atualização do progresso, o real fica atrás do previsto. Mitigação: é o que o ADR-0073 pede mostrar; a atenção aponta as tarefas atrasadas.
- **[Linha do tempo própria]** Dois desenhos de linha do tempo (o Syncfusion na aba e o da visão geral). Mitigação: o da visão geral só lê, como a página de impressão, e usa os mesmos tokens.
