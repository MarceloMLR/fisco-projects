# Tasks

## 1. Papéis no módulo de usuários

- [x] 1.1 Acrescentar o papel no sistema (`SystemRole` no contrato, `Role` no usuário, migração `SystemRoles`), o papel Usuário na primeira entrada e o primeiro administrador pela configuração (`Users:FirstAdministrator`, validada ao subir), com Paulo Nogueira no `appsettings.Development.json`; trocar `IUserAccess.IsActiveAsync` por `FindAsync`, guardado durante a requisição; testar "Papel na primeira entrada", "Primeiro administrador configurado", "Configuração depois da primeira entrada" e "Troca feita na tela respeitada"; verificar com `dotnet build -warnaserror` e `dotnet test`
- [x] 1.2 Criar os casos de uso de trocar o papel, desativar e reativar, com a proteção do último administrador ativo numa transação `Serializable`; testar "Trocar o papel do último administrador", "Desativar o último administrador", "Outro administrador ativo" e duas trocas simultâneas entre os dois últimos administradores, com SQL Server; verificar com `dotnet test`

## 2. API da administração de usuários

- [x] 2.1 Criar a política `Administrator` e as rotas de lista (busca sem acentos, filtros, paginação), troca de papel, desativar e reativar, com o papel na rota da sessão e os enums em texto no JSON; testar a matriz de permissões gerada de uma tabela ("Administrador na administração de usuários", "Gerente de projeto na administração de usuários" e "Usuário na administração de usuários"), "Troca de papel na sessão aberta", "Desativar acesso", "Reativar acesso", a busca e os filtros; atualizar o contrato e os tipos com `npm run api:update`; verificar com `dotnet test` e `npm test`

## 3. Administração nas telas

- [ ] 3.1 Acrescentar o papel na sessão do frontend, o item Administração só para o administrador (barra lateral e menu do celular), as rotas `/admin` com `RequireAdministrator` e a trilha com os níveis acima da página; testar "Administração na barra lateral", "Endereço da Administração sem ser administrador" e "Trilha de uma página dentro de uma área"; verificar com `npm test`, `npm run lint` e `npm run typecheck`
- [ ] 3.2 Criar os componentes `DataTable` (MUI X Data Grid em pt-BR, visual do `fp-gt`), `Select` e `StatusBadge`, o estilo de menus e Snackbar no tema e a área de Administração com a navegação das seções; testar a tabela (paginação e linha esmaecida) e os componentes; verificar com `npm test`
- [ ] 3.3 Criar a tela Usuários do canvas, com a busca, os filtros, o menu de papel e acesso e os avisos; testar "Lista de usuários", "Busca por nome ou e-mail", "Filtro por papel e situação", "Trocar o papel na tela", "Desativar e reativar na tela" e "Recusa do último administrador na tela"; verificar com `npm test`

## 4. Verificação final

- [ ] 4.1 Conferir no Chromium, com a API e o banco local, a Administração de ponta a ponta (Paulo como administrador, troca de papel, desativar e reativar, recusa do último administrador e a visão de quem não é administrador), no computador e no celular, nos temas claro e escuro, contra o canvas
- [ ] 4.2 Atualizar o glossário, o guia de instalação (primeiro administrador) e o `CLAUDE.md`, rodar todos os scripts de `scripts/ci` sem erro, abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions

## Workflow follow-up

- Marcar a Acesso-2 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: `/opsx:archive` na mesma branch, roadmap como "Concluída" e merge.
