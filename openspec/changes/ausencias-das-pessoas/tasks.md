## 1. Decisão e roadmap

- [x] 1.1 ADR-0141 e GanttB-4 em andamento no roadmap

## 2. Domínio e API

- [x] 2.1 `PersonAbsence` no domínio, com a migração e a auditoria; testes das regras
- [x] 2.2 `ListMemberAbsences`, `AddMemberAbsence` e `RemoveMemberAbsence` e as rotas; testes "Incluir ausência", "Ausência inválida", "Ausência sobreposta", "Apagar ausência", "Permissões nas ausências por papel", "Ausência pelo projeto encerrado" e "Ausência na auditoria"
- [x] 2.3 `nextAbsence` na lista de Membros e `ownerAbsences` no cronograma; testes "Ausente hoje na lista", "Tarefa na ausência do responsável" e "Sem aviso na concluída"
- [x] 2.4 `CleanUpAbsences`, `AbsenceCleanup` e o apagador da anonimização; testes "Ausência apagada depois de um ano" e "Ausências na anonimização"

## 3. Tela

- [x] 3.1 `npm run api:update`; a seção e o diálogo das ausências; testes "Incluir ausência pela tela", "Ausência sobreposta pela tela" e "Ausências de quem só vê"
- [x] 3.2 A ausência na lista de Membros e o aviso no cronograma; testes "Ausente hoje na lista" e "Tarefa na ausência do responsável"
- [x] 3.3 A auditoria na tela e a demonstração; teste "Ausências na demonstração"
- [x] 3.4 Conferência no navegador, no computador e no celular, nos temas claro e escuro

## 4. Documentação

- [x] 4.1 `CLAUDE.md` e `docs/glossario.md` com as ausências
