## 1. Decisão

- [ ] 1.1 ADR-0135 aceito no índice dos ADRs, depois da aprovação de Marcelo; roadmap com a Gantt-3 em andamento

## 2. Domínio e leitura

- [ ] 2.1 `SchedulePlanOutline` aplicado pelo `Schedule.ApplyTemplate`, entregue pelo `ProjectTemplate`; os testes dos modelos continuam passando
- [ ] 2.2 `MsProjectImport.Read`: XML sem DTD, namespace do MS Project, limites de 5 MB e 2.000 tarefas, as linhas do arquivo; testes com arquivos de exemplo, com DTD, sem namespace e grandes demais
- [ ] 2.3 `MsProjectImport.Convert`: níveis, tarefas soltas, datas em dias úteis, durações de 1 a 999, dependências e intervalos, nomes cortados e os avisos; testes de cada regra do ADR-0135 e a ida e volta com o `MsProjectExport`

## 3. API

- [ ] 3.1 `POST /api/projects/ms-project-preview` (multipart, `ProjectCreators`, `no-store`) com o plano, as datas finais e os avisos; testes "Prévia do arquivo do MS Project", "O que não cabe no FiscoProjects", "Arquivo que não é do MS Project", "Importar sem poder criar projetos" e "Ida e volta pelo MS Project"
- [ ] 3.2 `CreateProject` com `importedSchedule` validado e aplicado na transação da criação; teste "Projeto a partir do MS Project" e os de plano inválido (400)

## 4. Tela

- [ ] 4.1 `npm run api:update`; a terceira origem no `ScheduleOriginChoice`, o campo do arquivo, o início preenchido, a prévia generalizada com os avisos e o erro no campo; testes de tela "Novo projeto a partir do MS Project" e "Arquivo recusado pela tela", nos temas claro e escuro, no computador e no celular
- [ ] 4.2 Conferência no navegador contra as telas aprovadas do "Novo projeto"

## 5. Demonstração e documentação

- [ ] 5.1 O projeto importado no plano de `web/scripts/demo`, com o teste "Projeto importado na demonstração"
- [ ] 5.2 `CLAUDE.md`, `docs/glossario.md`, `docs/discovery.md` e o contexto do OpenSpec com a importação

## 6. Verificação

- [ ] 6.1 `scripts/ci/build.sh`, `test.sh`, `format.sh`, `frontend.sh` e `specs.sh` passando
