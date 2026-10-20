## 1. Domínio

- [x] 1.1 `ScheduleCriticalPath` e os testes "Cadeia mais longa é crítica", "Folga em dias úteis" e "Subtarefas no caminho crítico"
- [x] 1.2 `ScheduleBaseline` e `ScheduleBaselineItem` no agregado, `SaveBaseline` e a diferença; testes "Salvar linha de base", "Atualizar linha de base", "Linha de base sem tarefas", "Tarefa depois da linha de base", "Tarefa antes da linha de base", "Item incluído depois da linha de base" e "Fim do projeto contra a linha de base"

## 2. Banco, API e auditoria

- [x] 2.1 Configuração das duas tabelas, auditoria da linha de base e migração
- [x] 2.2 `SaveScheduleBaseline`, a rota `PUT .../schedule/baseline` com a matriz de permissões, o `GetSchedule` e as respostas; testes da API, "Linha de base num projeto encerrado" e "Linha de base na auditoria"
- [x] 2.3 Contrato OpenAPI e tipos do frontend

## 3. Tela

- [x] 3.1 "Exibir" na barra e no Expandir; `ScheduleGantt` com a linha de base e o caminho crítico, e a legenda; testes "Caminho crítico na linha do tempo" e "Exibir sem linha de base e caminho crítico"
- [x] 3.2 Menu "⋮" com salvar e atualizar a linha de base, o resumo e a seção Linha de base no detalhe; testes "Salvar linha de base pela tela", "Atualizar linha de base pela tela", "Linha de base no detalhe" e "Sem editar, sem salvar a linha de base"
- [x] 3.3 Tipo "Linhas de base" na Auditoria, com ações e campos

## 4. Dados de demonstração e documentação

- [x] 4.1 Linha de base e a tarefa adiada no plano do ERP-FIN; teste "Linha de base na demonstração"
- [x] 4.2 `CLAUDE.md`, glossário e roadmap
- [x] 4.3 Build, testes, format, frontend e specs; conferir no navegador, nos temas claro e escuro, no computador e no celular
