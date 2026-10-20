## 1. Domínio

- [x] 1.1 `ScheduleSnapshot`, `ScheduleDiff` e `Schedule.Apply`, com os testes de desfazer a cadeia, a exclusão e o bloqueio no domínio
- [x] 1.2 `ScheduleChange` e `ScheduleAction`

## 2. Banco e API

- [x] 2.1 Tabela `schedule_changes`, configuração fora da auditoria e migração
- [x] 2.2 Registro no `ScheduleEditing.RunAsync`, com o rótulo de cada caso de uso, o limite de 20 e o refazer apagado
- [x] 2.3 `UndoScheduleChange`, `RedoScheduleChange`, as rotas, a matriz de permissões e o `GetSchedule` com `undo` e `redo`; testes da API dos cenários
- [x] 2.4 Contrato OpenAPI e tipos do frontend

## 3. Tela

- [x] 3.1 Botão "Desfazer" na barra e no Expandir, "Refazer" no "⋮" e as mensagens; testes "Desfazer pela tela", "Refazer pela tela", "Desfazer bloqueado pela tela" e "Sem editar, sem desfazer"

## 4. Documentação

- [x] 4.1 ADR-0091, `CLAUDE.md` e roadmap
- [x] 4.2 Build, testes, format, frontend e specs; conferir no navegador, nos temas claro e escuro, no computador e no celular
