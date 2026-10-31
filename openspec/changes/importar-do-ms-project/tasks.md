## 1. Decisão

- [x] 1.1 ADR-0135 aceito no índice dos ADRs, com o progresso e os responsáveis a pedido de Marcelo; roadmap com a Gantt-3 em andamento

## 2. Domínio e leitura

- [x] 2.1 `SchedulePlanOutline` aplicado pelo `Schedule.ApplyTemplate`, com o progresso, o responsável ou o cliente opcionais, entregue pelo `ProjectTemplate`; os testes dos modelos continuam passando
- [x] 2.2 `MsProjectImport.Read`: XML sem DTD, namespace do MS Project, limites de 5 MB e 2.000 tarefas, as linhas, os recursos de trabalho e as atribuições; testes com arquivos de exemplo, com DTD, sem namespace e grandes demais
- [x] 2.3 `MsProjectImport.Convert`: níveis, tarefas soltas, datas em dias úteis, durações de 1 a 999, dependências e intervalos, progresso, o primeiro recurso de cada tarefa, nomes cortados e os avisos; testes de cada regra do ADR-0135 e a ida e volta com o `MsProjectExport`

## 3. API

- [x] 3.1 `IPeopleLookup.ByEmailsAsync` no `Users.Contracts`, sobre os usuários ativos e o diretório da empresa, com testes
- [x] 3.2 `POST /api/projects/ms-project-preview` (multipart, `ProjectCreators`, `no-store`) com o plano, as datas finais, os recursos com a sugestão e os avisos; testes "Prévia do arquivo do MS Project", "Responsáveis do arquivo", "O que não cabe no FiscoProjects", "Arquivo que não é do MS Project", "Importar sem poder criar projetos" e "Ida e volta pelo MS Project"
- [x] 3.3 `CreateProject` com `importedSchedule` validado: as pessoas incluídas com o papel e o cronograma com os responsáveis e o progresso, na transação da criação; testes "Projeto a partir do MS Project", "Progresso do arquivo" e os de plano inválido (400)

## 4. Tela

- [x] 4.1 `npm run api:update`; a terceira origem no `ScheduleOriginChoice`, o campo do arquivo, o início preenchido, a prévia generalizada com os avisos, os responsáveis do arquivo (pessoa e papel) e o erro no campo; testes de tela "Novo projeto a partir do MS Project" e "Arquivo recusado pela tela", nos temas claro e escuro, no computador e no celular
- [x] 4.2 Conferência no navegador contra as telas aprovadas do "Novo projeto"

## 5. Demonstração e documentação

- [x] 5.1 O projeto importado no plano de `web/scripts/demo`, com os responsáveis ligados às pessoas da demonstração, e o teste "Projeto importado na demonstração"
- [x] 5.2 `CLAUDE.md`, `docs/glossario.md`, `docs/discovery.md` e o contexto do OpenSpec com a importação

## 6. Verificação

- [ ] 6.1 `scripts/ci/build.sh`, `test.sh`, `format.sh`, `frontend.sh` e `specs.sh` passando
