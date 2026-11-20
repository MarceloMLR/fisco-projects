## 1. Roadmap

- [ ] 1.1 Status-4 "Em andamento" no `docs/roadmap.md`

## 2. A pasta dos relatórios

- [ ] 2.1 A pasta padrão e a do projeto marcadas como a dos relatórios (`IsStatusReport`), nunca desativadas. A migração do `Documents` inclui "Relatórios de status", vista só pela gestão. Testes dos cenários "Pasta dos relatórios padrão" e "Pasta dos relatórios sempre ativa", com o "Projeto novo recebe as pastas padrão" atualizado para sete pastas
- [ ] 2.2 O contrato `IStatusReportDocuments` (`Projects.Contracts`) e a implementação no `Documents`: cria a pasta que falta e guarda o arquivo como documento novo ou nova versão, em qualquer situação do projeto. Testes do módulo, com o cenário "Projeto antigo recebe a pasta dos relatórios"
- [ ] 2.3 A marca da pasta dos relatórios em Pastas padrão, sem "Desativar", com o teste de tela

## 3. Os dados dos outros módulos

- [ ] 3.1 O `IInstallationBrand` (`Administration.Contracts`): o nome, o logo e as cores da instalação, com o teste do módulo
- [ ] 3.2 O `IProjectReportTickets` (`Projects.Contracts`) e a implementação no `Tickets`: os chamados em aberto com o status, a prioridade, o registro no fornecedor da lista e os itens ligados, com os testes do módulo
- [ ] 3.3 O `IProjectReportHours` (`Projects.Contracts`) e a implementação no `TimeTracking`, com a regra `ShowsHoursInReport` ao lado do `SeesTaskHours`: só os totais do projeto e das tarefas. Testes dos cenários "Saldo do projeto no relatório pelo gerente" e "Horas do relatório sem as pessoas"

## 4. O relatório no domínio e na aplicação

- [ ] 4.1 As regras no domínio, com os testes dos cenários de "Textos e riscos do relatório" e "Rascunho do relatório" e do andamento previsto de cada fase:
  - `RiskLevel`, `StatusReportLimits` e `StatusReportForm` (com o `StatusReportField`);
  - `StatusReportSuggestions`;
  - `StatusReportNotes` com os riscos.
- [ ] 4.2 As tabelas `status_report_notes` e `status_report_risks` (migração do `Projects`), fora da auditoria e apagadas com o projeto, com o teste da exclusão do projeto
- [ ] 4.3 Os casos de uso, com os testes dos cenários de "O que vem do sistema" sobre os dados:
  - `GetStatusReportDraft` e `GenerateStatusReport`;
  - `StatusReportData`, montado com o `GetProjectOverview`, os contratos e os logos;
  - a interface `IStatusReportRenderer`.

## 5. O PowerPoint

- [ ] 5.1 O `StatusReportFiller`: os campos de texto, também em pedaços, as listas com a linha-modelo e a lista vazia, o slide repetido, o slide condicional, os logos e a barra. Testes dos cenários de "Campos do modelo", com o validador do Open XML SDK
- [ ] 5.2 O `DefaultStatusReportTemplate` e o `pt-BR.json` do relatório:
  - os slides do design D4, a Calibri e as cores do tema pela instalação;
  - o logo SVG pelo `svgBlip`;
  - o nome no lugar do logo que falta.

  Testes dos cenários de "Modelo padrão do relatório", "Riscos no relatório" e "Horas no relatório"
- [ ] 5.3 O `PptxStatusReportRenderer`, que junta o modelo, os valores e os textos formatados (datas, dias úteis, status, horas), registrado no módulo, com o relatório gerado aberto no LibreOffice e conferido nos textos no limite

## 6. A API

- [ ] 6.1 As rotas `GET` e `POST /api/projects/{id}/status-report` e o `canGenerateStatusReport` nas permissões do projeto, com o contrato da API atualizado (`npm run api:update`). Testes da API dos cenários:
  - "Quem gera o relatório", incluindo "Relatório de projeto encerrado";
  - "Relatório guardado nos documentos";
  - "Horas no relatório";
  - os 400 de cada campo.

## 7. A tela

- [ ] 7.1 "Gerar relatório de status" em "Ações do projeto" e o `StatusReportDialog`: o rascunho, os riscos com a classificação e o ponto de atenção, "Mostrar horas", o envio, o download e o aviso, com os textos em `pt-BR.json`. Testes de tela dos cenários de "Gerar o relatório pela tela"
- [ ] 7.2 Telas conferidas nos temas claro e escuro e no celular; `npm run lint`, `npm run typecheck` e `npm test` limpos

## 8. Demonstração, documentação e verificação

- [ ] 8.1 O `statusReport` do plano no ERP-FIN e a carga pela gerente, uma vez só, com os testes de `web/scripts/demo` dos cenários de "Relatório de status na demonstração"
- [ ] 8.2 Documentação e specs:
  - `CLAUDE.md`, nas seções do mapa e da tela;
  - `docs/glossario.md`: relatório de status, risco, classificação e ponto de atenção;
  - `openspec validate --all --strict` limpo.
- [ ] 8.3 Verificação:
  - `dotnet build -warnaserror`, `dotnet test` e `dotnet format`;
  - os testes do frontend;
  - a demonstração carregada, com o relatório aberto e conferido nos slides.

## Workflow follow-up

- PR com a aprovação de Marcelo, archive na mesma branch antes do merge, roadmap "Concluída" e o merge por Marcelo.
