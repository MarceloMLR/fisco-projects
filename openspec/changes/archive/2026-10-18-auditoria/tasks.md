# Tasks

## 1. Captura das alterações no contexto base

- [x] 1.1 Criar no `FiscoProjects.Persistence` a captura das alterações (`AuditedChange`, valores em texto invariável), as marcações `HasAuditLabel`, `ExcludeFromAudit` e `IsAuditSecret`, a interface `IChangeAuditor` e a gravação com auditoria em `ModuleDbContext.SaveChangesAsync` (mesma transação, reaproveitando a do módulo, dentro da estratégia de novas tentativas; `SaveChanges` síncrono recusado); testar com SQL Server "Criar um item", "Alterar um item", "Excluir um item", "Campo técnico fora da auditoria", "Senha alterada", "Alteração que não é salva" e "Registro que não é gravado"; verificar com `dotnet build -warnaserror` e `dotnet test`

## 2. Módulo de auditoria

- [x] 2.1 Criar o módulo `Audit` (contratos, `AuditEntry`, contexto só de inclusão com a migração `Initial` no schema `audit`, `IChangeAuditor` com o autor da sessão, `ISecurityEvents`, consulta `ListAuditEntries`) e registrá-lo no host; acrescentar `IUserDirectory` ao contrato do `Users`; testar "Gravação só acrescenta", a consulta com filtros, ordem e paginação e a gravação na mesma transação; verificar com `dotnet test` e os testes de arquitetura
- [x] 2.2 Marcar a auditoria no `Users` (rótulo, `LastSignInAt` e `CreatedAt` fora, vínculo fora) e registrar a entrada e as recusas em `SignInWithIdentity` e `SignInLocally`; testar "Autor da alteração", "Alteração sem sessão", "Entrada registrada", "Entrada recusada registrada", "Troca de papel registrada" e "Desativação e reativação registradas"; verificar com `dotnet test`

## 3. API da auditoria

- [x] 3.1 Criar `GET /api/audit` com os filtros, a paginação e o nome do autor, só para o administrador; testar a matriz de permissões gerada de uma tabela ("Administrador na auditoria", "Gerente de projeto na auditoria" e "Usuário na auditoria"), "Sem rota para alterar ou apagar" e os filtros de ponta a ponta; atualizar o contrato e os tipos com `npm run api:update`; verificar com `dotnet test` e `npm test`

## 4. Tela de Auditoria

- [x] 4.1 Acrescentar à `DataTable` a linha selecionada e o texto do rodapé, e o avatar de Sistema; criar a seção Auditoria na Administração (rota `/admin/audit`, trilha, item "Auditoria" em "Pessoas e segurança") e a tela do canvas, com os filtros de pessoa, tipo e período, o painel de detalhes (gaveta abaixo de 1200px) e os textos dos registros num lugar só; testar "Lista da auditoria", "Troca de papel na lista", "Filtro por pessoa", "Filtro por tipo de item", "Filtro por período" e "Detalhes de um registro"; verificar com `npm test`, `npm run lint` e `npm run typecheck`

## 5. Verificação final

- [x] 5.1 Conferir no Chromium, com a API e o banco local, a auditoria de ponta a ponta (entrada, entrada recusada, troca de papel, desativação e reativação aparecendo na tela, filtros e detalhes; seção escondida de quem não é administrador), no computador e no celular, nos temas claro e escuro, contra o canvas
- [x] 5.2 Atualizar o glossário, o guia de módulos (marcações de auditoria e alterações só pelo `SaveChangesAsync`) e o `CLAUDE.md`, rodar todos os scripts de `scripts/ci` sem erro, abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions

## Workflow follow-up

- Marcar a Acesso-3 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: `/opsx:archive` na mesma branch, roadmap como "Concluída" e merge.
