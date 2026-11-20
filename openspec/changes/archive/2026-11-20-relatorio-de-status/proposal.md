## Why

Toda semana, o gerente de um projeto de implantação manda ao cliente um status report em PowerPoint. Ele copia à mão, do cronograma e dos chamados, o andamento, os marcos, a previsão do fim e os riscos, e escreve o resumo e os próximos passos. O GerenWare já tem esses números na Visão geral (ADR-0073), no cronograma e nos chamados ligados a ele (ADR-0164), mas não monta o relatório.

É o Status-4 do grupo 18 (Status do projeto), pedido por Marcelo em 19/11/2026. Ele implementa o ADR-0165 só com o PowerPoint e o modelo padrão (o PDF fica para a Status-5, e o modelo da empresa para a Status-6) e o ADR-0166 ("Mostrar horas").

## What Changes

- **Gerar relatório de status** em "Ações do projeto", só para o administrador e o gerente do projeto, em qualquer situação do projeto. É a mesma regra da exportação para o MS Project (ADR-0136): as outras pessoas do projeto recebem 403, e quem está fora, 404.
- **O que quem gera escolhe e escreve:**
  - Os quatro textos: o resumo do período, o foco atual, os próximos passos e o que precisa do cliente.
  - Os chamados em aberto do projeto que entram como riscos, até 10. Vêm sugeridos os que ameaçam marcos (ADR-0164) e os riscos do último relatório que continuam em aberto.
  - Em cada risco, a classificação (Alta, Média ou Baixa) e o ponto de atenção.
  - "Mostrar horas", desmarcado.
  - Os textos e os riscos do último relatório do projeto vêm preenchidos.
- **O que vem do sistema**, pelas mesmas regras da Visão geral e do cronograma, sem uma segunda conta:
  - O cliente e o logo dele, o projeto, a situação, o gerente e a data.
  - O andamento total e de cada fase, contra o previsto.
  - O prazo final e o desvio contra a linha de base.
  - Os próximos marcos e as fases com os períodos.
  - As tarefas atrasadas e o que elas empurram.
  - Os chamados em aberto e os vencidos.
  - Em cada risco: o número, o título, o status, a prioridade, o registro no fornecedor com o status e os marcos que dependem dele.
- **O modelo padrão** é um PowerPoint 16:9 montado pelo próprio GerenWare, com o logo e as cores da instalação (ADR-0146) e o logo do cliente:
  - "Status do projeto"
  - "Cronograma"
  - "Próximos passos"
  - um slide por risco
  - "Horas do projeto", só com "Mostrar horas"

  Os valores entram por campos entre chaves duplas (`{{projeto.nome}}`). As listas vão numa tabela com uma linha-modelo. O slide de risco se repete para cada risco. Os logos e a barra do andamento são formas pelo nome. É o mesmo preenchimento que o modelo da empresa vai usar na Status-6.
- **Horas no relatório (ADR-0166):** com "Mostrar horas", entram as horas contratadas, as adicionais, as apontadas, o saldo e o estado, e as previstas e as apontadas de cada tarefa. As horas de cada pessoa nunca entram. A regra fica no módulo das horas, que entrega ao relatório só os totais.
- **Onde fica:** o relatório gerado é guardado como documento do projeto na nova pasta padrão dos relatórios.
  - A migração cria a pasta como "Relatórios de status", vista de início só pela gestão (ADR-0148).
  - Ela nunca é desativada, como a de termos, e o administrador pode renomeá-la.
  - O projeto que ainda não tem a pasta a recebe no primeiro relatório.
  - O nome do arquivo leva o código do projeto e o dia, e gerar de novo no mesmo dia vira uma nova versão (ADR-0031).
- Depois de gerar, a tela baixa o arquivo e avisa onde ele foi guardado.
- A demonstração gera um relatório do ERP-FIN, para a pasta e o preenchimento aparecerem.

## Capabilities

### New Capabilities

- `relatorio-de-status`: gerar o relatório de status do projeto em PowerPoint, com o que vem do sistema, o que o gerente escreve, os riscos, as horas, o modelo padrão e os campos, pela API e pela tela.

### Modified Capabilities

- `documentos`: a pasta padrão dos relatórios, criada pela migração, nunca desativada e recebida pelo projeto no primeiro relatório.
- `horas-do-projeto`: as horas do projeto e das tarefas para o relatório de status, para quem gera, sem as horas de cada pessoa.
- `dados-de-demonstracao`: um relatório de status gerado no projeto principal.

## Non-goals

- O PDF (Status-5) e o modelo da empresa, com a lista dos campos, a conferência e "Voltar ao padrão" (Status-6).
- Um modelo por cliente, gráficos nativos do PowerPoint, mandar o relatório por e-mail ao cliente e gerá-lo toda semana sozinho (ADR-0165, "depois").
- Ver o saldo das horas fora do relatório (ADR-0166, opção H2) e as horas de cada pessoa no relatório (ADR-0128).
- Fontes embutidas no arquivo e o logo da instalação em SVG para versões antigas do PowerPoint (veja o design).

## Impact

- `src/Modules/Projects`:
  - O relatório no domínio (o andamento das fases, os riscos, os textos e as regras) e os casos de uso `GetStatusReportDraft` e `GenerateStatusReport`.
  - As anotações do último relatório (`projects.status_report_notes`, fora da auditoria, apagadas com o projeto).
  - O modelo padrão e o preenchimento em `Infrastructure` (Open XML SDK, que o projeto já usa no módulo `Credentials`, ADR-0094), com os textos em `pt-BR.json`.
  - As rotas `GET` e `POST /api/projects/{id}/status-report` e o `canGenerateStatusReport` nas permissões do projeto.
- `src/Modules/Projects/GerenWare.Projects.Contracts`, com três contratos novos e a implementação de cada um:
  - `IProjectReportTickets`, os chamados em aberto com o status, a prioridade e o fornecedor (`Tickets`).
  - `IProjectReportHours`, os totais das horas (`TimeTracking`).
  - `IStatusReportDocuments`, guardar o arquivo na pasta dos relatórios (`Documents`).
- `src/Modules/Administration/GerenWare.Administration.Contracts`: o logo e as cores da instalação para outros módulos (`IInstallationBrand`).
- `src/Modules/Documents`: a pasta padrão marcada como a dos relatórios (migração) e a pasta no projeto.
- `web/src/projects`: "Gerar relatório de status" em "Ações do projeto" e o diálogo `StatusReportDialog`. Em `web/src/documents`, a marca da pasta dos relatórios em Pastas padrão.
- `web/scripts/demo`: o relatório da demonstração.
- Textos em `pt-BR.json` (tela) e no `pt-BR.json` do relatório; `CLAUDE.md`, `docs/glossario.md` e o roadmap.
- ADRs: 0003, 0031, 0040, 0043, 0073, 0092, 0128, 0136, 0144, 0146, 0148, 0164, 0165 e 0166.
