## 1. Domínio e dados

- [ ] 1.1 `ContractedHours`, `AdditionalHours` e `HoursBalance` (estados e ordem) no domínio do `TimeTracking`, com os testes: "Horas contratadas inválidas", "Horas adicionais sem motivo", "Projeto acima das horas contratadas", "Projeto perto do limite", "Projeto encerrado perto do limite" e "Projeto sem horas contratadas"
- [ ] 1.2 Configuração das tabelas `contracted_hours` e `additional_hours` com a auditoria (rótulo, referências e contexto) e a migração

## 2. Contratos

- [ ] 2.1 `IProjectCatalog` no `Projects.Contracts`, implementado no `Projects` (`ProjectCatalog`), com a situação e o nome do cliente

## 3. Casos de uso e API

- [ ] 3.1 `ListProjectHours`, `GetProjectHours`, `ChangeContractedHours`, `AddAdditionalHours`, `ChangeAdditionalHours` e `DeleteAdditionalHours`, com os endpoints em `/api/admin/project-hours`
- [ ] 3.2 `ListTimeEntries` com `projectId` só do administrador e a pessoa em cada apontamento
- [ ] 3.3 Exclusão do projeto com as contratadas e as adicionais, e o aquecimento com a lista
- [ ] 3.4 Testes da API com o nome de cada cenário: "Cadastrar as horas contratadas", "Tirar as horas contratadas", "Horas contratadas inválidas", "Incluir horas adicionais", "Corrigir e apagar horas adicionais", "Horas adicionais sem motivo", "Lista das horas dos projetos", "Busca nas horas dos projetos", "Detalhe do projeto", "Pessoa que saiu no detalhe", "Apontamentos do projeto", "Apontamentos do projeto sem ser administrador", "Permissões no saldo por papel", "Horas contratadas e adicionais na auditoria" e "Excluir projeto"

## 4. Tela

- [ ] 4.1 `npm run api:update`, as consultas (`projectHoursQuery`, `projectHoursDetailQuery`, `projectEntriesQuery`) e os simuladores do MSW (`web/src/test/demoProjectHours.ts`)
- [ ] 4.2 `ProgressBar` com o tom e o rótulo do design system, com os testes
- [ ] 4.3 `projectHours.ts` (texto, tom e ícone do saldo, meta) e `projectHoursReport.ts` (planilha), com os testes
- [ ] 4.4 Seção Acompanhamento no `AdminLayout`, rota `/admin/project-hours`, `ProjectHoursPage` e textos em `pt-BR.json`
- [ ] 4.5 `ProjectHoursDrawer`, `ContractedHoursDialog`, `AdditionalHoursDialog` e `ProjectEntriesDialog`
- [ ] 4.6 "Horas contratadas" e "Horas adicionais" na Auditoria (`describeAuditEntry`)
- [ ] 4.7 Testes de tela: "Horas dos projetos pela tela", "Baixar a planilha das horas", "Incluir horas adicionais pela tela", "Alterar as horas contratadas pela tela" e "Ver apontamentos pela tela"

## 5. Demonstração

- [ ] 5.1 `contractedHours` e `additionalHours` no plano (`plan.ts`) e na carga (`load.ts`), com os testes "Saldo na demonstração"

## 6. Documentação e conferência

- [ ] 6.1 `CLAUDE.md` (o saldo na seção 3 e a tela na seção 6), glossário e roadmap
- [ ] 6.2 Capturas de Horas dos projetos, do detalhe, do diálogo das adicionais e dos apontamentos do projeto, claro e escuro, para o PR
- [ ] 6.3 Build sem avisos, testes, `dotnet format`, lint, typecheck e `openspec validate --all --strict`
