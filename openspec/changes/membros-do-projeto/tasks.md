# Tasks

## 1. Pessoas e diretório no `Users`

- [x] 1.1 Diretório e usuário criado pelo diretório:
  - `IPeopleDirectory` com `LocalPeopleDirectory`;
  - `ListPeople` com as pessoas do diretório, `canBeManager` e `/api/people` para qualquer pessoa com sessão;
  - `IUserProvisioning` e `User.FromDirectory`;
  - `LastSignInAt` opcional, com a migração `UsersNeverSignedIn`;
  - `IUserDirectory.PeopleAsync`.

  Testar com `dotnet test`:
  - a busca com usuários e diretório, sem repetir e sem inativos, e só gerentes;
  - o usuário criado pelo diretório e a primeira entrada dele sem duplicar;
  - a pessoa fora do diretório.

## 2. Pessoas no `Projects`

- [x] 2.1 Domínio e auditoria:
  - `AddMember`, `ChangeMemberRole` e `RemoveMember`, com o último gerente e o projeto encerrado;
  - `CanManagePeople`, `CanManageManagers` e `MayManage`;
  - `IsAuditContext()` no `Persistence`, aplicado ao `ProjectId`.

  Testar com `dotnet test`:
  - as regras no domínio;
  - a matriz das permissões;
  - o contexto gravado na alteração.
- [x] 2.2 Casos de uso e API:
  - `ListProjectMembers`, `AddProjectMember`, `ChangeProjectMemberRole` e `RemoveProjectMember`, numa transação serializável;
  - rotas `/api/projects/{id}/members` e `canManagePeople` e `canManageManagers` no projeto;
  - `techLeadDirectoryId` na criação.

  Testar com `dotnet test`:
  - a matriz de permissões com administrador, gerente, líder técnico, colaborador, desenvolvedor, leitor, pessoa de fora e sem sessão;
  - os 400, 404 e 409;
  - o gerente com o papel no sistema (ADR-0085);
  - quem sai perde o acesso;
  - os pedidos ao mesmo tempo no último gerente;
  - a auditoria.

  Atualizar o contrato com `npm run api:update`.

## 3. Frontend

- [ ] 3.1 Página do projeto com abas:
  - `ProjectLayout`, com as abas Visão geral e Membros como rotas e o número de pessoas;
  - `DataTable` com linhas de grupo;
  - `PersonField` com as pessoas do diretório e `exclude`;
  - o líder técnico do diretório no Novo projeto;
  - "Nunca entrou" na tela Usuários.

  Testar com `npm test`:
  - "Abas do projeto";
  - o líder técnico do diretório no Novo projeto;
  - "Pessoa que nunca entrou".
- [ ] 3.2 Aba Membros:
  - lista por time e por nome, com busca;
  - "Incluir pessoa";
  - detalhe na gaveta, com a troca do papel e "Remover do projeto";
  - textos da Auditoria com o projeto nas mudanças de papel.

  Testar com `npm test`:
  - "Membros pela tela", "Buscar e agrupar por nome", "Incluir pessoa pela tela", "Membros sem gerenciar";
  - "Trocar o papel pela tela", "Último gerente pela tela", "Remover do projeto pela tela", "Remover a si mesmo pela tela";
  - os títulos da Auditoria.

## 4. Verificação final

- [ ] 4.1 Conferir no navegador, com o build de produção:
  - a aba Membros, "Incluir pessoa", o detalhe e a remoção;
  - o Novo projeto com o líder técnico do diretório;
  - Usuários com "Nunca entrou" e a Auditoria;
  - nos temas claro e escuro, no computador e no celular.

  Mandar as capturas para Marcelo.
- [ ] 4.2 Documentação e verificações:
  - glossário, `CLAUDE.md` e roadmap;
  - rodar todos os scripts de `scripts/ci` sem erro;
  - abrir o PR e confirmar a verificação `verify`.

## Workflow follow-up

- Marcar a Projetos-4 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo:
  - `/opsx:archive` na mesma branch, com o ADR-0085 aceito;
  - roadmap "Concluída".
