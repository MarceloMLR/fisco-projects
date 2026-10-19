# Design: membros do projeto (Projetos-4)

## Context

- **`Project`** (Projetos-3): agregado com as pessoas em `Members` (`ProjectMember`: `UserId`, `Role`, `AddedAt`), criadas só na criação. `ProjectPermissions` tem `CanView`, `CanEditData` e `CanChangeStatus`; a API devolve `canEdit` e `canChangeStatus` já falsos com o projeto encerrado.
- **`Users`:**
  - `IUserAccess` dá o papel e a situação de uma pessoa, guardados durante a requisição;
  - `IUserDirectory` dá os nomes;
  - `/api/people` lista só usuários ativos, com a política `ProjectCreators`;
  - `User.LastSignInAt` é obrigatório, porque todo usuário nasce na primeira entrada;
  - a entrada local (ADR-0078) usa `LocalSignIn:People` como diretório de demonstração, com o provedor `local` e o e-mail normalizado como identificador externo.
- **Auditoria:** numa alteração, só os campos alterados são gravados. Trocar o papel de uma pessoa grava só `Role`, sem o projeto.
- **Canvas ProjetoMembros:** lista agrupada por time, com busca, "Por time / Por nome" e "Incluir pessoa", e um painel fixo à direita com o detalhe. O ADR-0081, posterior ao canvas, troca o painel fixo por uma gaveta.
- **Tela:** a página do projeto é uma rota só, com o cabeçalho, a Visão geral e "Ações do projeto".

## Goals / Non-Goals

**Goals:**

- As regras das pessoas no agregado `Project`, testadas no domínio, com a permissão num lugar só (`ProjectPermissions`).
- Um diretório atrás de uma interface, pronto para o Graph da M365-1 (ADR-0004), sem mudar quem o usa.

**Non-Goals:**

- Pendências de quem sai (chamados e tarefas) e avisos: changes dos grupos 4, 5 e 6.
- Mudar o papel nos projetos quando muda o papel no sistema (ADR-0085).

## Decisions

### 1. Domínio dos projetos

- **`Project`:**
  - `AddMember(userId, role, now)`: recusa quem já está no projeto (`ProjectMemberExistsException`);
  - `ChangeMemberRole(userId, role)` e `RemoveMember(userId)`: recusam deixar o projeto sem gerente (`LastManagerException`);
  - todos recusam o projeto encerrado (`ProjectClosedException`).

  O domínio não conhece o papel no sistema: a regra do ADR-0085 fica no caso de uso, que consulta o `IUserAccess`.
- **`ProjectPermissions`** ganha:
  - `CanManagePeople`: administrador, gerente e líder técnico;
  - `CanManageManagers`: administrador e gerente.

  A regra "o líder técnico não mexe em gerentes" é `CanManagePeople && (papel atual e papel novo não são Manager || CanManageManagers)`, num método só (`MayManage(currentRole, newRole)`).
- A API devolve `canManagePeople` e `canManageManagers` com o projeto, falsos com o projeto encerrado, como `canEdit`.

### 2. Casos de uso e concorrência

- `ListProjectMembers`, `AddProjectMember`, `ChangeProjectMemberRole` e `RemoveProjectMember`, com resultados `NotFound`, `Forbidden`, `Invalid(campo)`, `Closed`, `MemberExists`, `LastManager` e `Done`.
- **Ordem da inclusão:**
  1. projeto e permissões (404, 403);
  2. o papel: gerente só com usuário ativo com o papel no sistema (ADR-0085), e nunca pelo diretório, porque quem ainda não entrou tem o papel Usuário;
  3. a pessoa: usuário ativo, ou pessoa do diretório, criada como usuário;
  4. a gravação.

  Assim, um pedido recusado por permissão ou papel não cria usuário.
- **Último gerente:** a gravação roda numa transação serializável, repetida em deadlock, como a do último administrador no `Users`. Duas remoções ao mesmo tempo dos dois últimos gerentes não passam.
  - **Alternativa descartada:** um token de concorrência no `Project`. Ele exigiria tocar a linha do projeto a cada mudança de pessoa, e a auditoria registraria essas alterações vazias.
- **Na criação do projeto**, `NewProject` ganha `TechLeadDirectoryId`, além de `TechLeadId`. A pessoa do diretório vira usuário só depois de todas as validações, inclusive a do código.

### 3. Diretório da empresa (versão simulada)

- **`IPeopleDirectory`** no `Users.Application`:
  - `SearchAsync(search, limit)` e `FindAsync(provider, externalId)`;
  - devolvem `DirectoryPerson(Provider, ExternalId, Name, Email)`.
- **`LocalPeopleDirectory`** lê `LocalSignIn:People` com a entrada local ligada e não devolve ninguém com ela desligada. Na M365-1, a implementação do Graph entra no lugar, sem mudar quem usa a interface.
- **Identificador do diretório para a API:** `"{provider}:{externalId}"`, opaco para o frontend.
- **Busca de pessoas (`ListPeople`):**
  - une os usuários ativos e as pessoas do diretório sem vínculo com um usuário;
  - uma pessoa do diretório cujo usuário está inativo não aparece;
  - em ordem alfabética, até o limite;
  - com `managers`, só usuários;
  - cada resultado traz `id` (usuário) ou `directoryId`, e `canBeManager`.
  - **Alternativa descartada:** criar os usuários na busca. Isso criaria usuários para todos os resultados, mesmo quem nunca é incluído.
- **Usuário criado pelo diretório:** contrato `IUserProvisioning.UserFromDirectoryAsync(directoryId)` no `Users.Contracts`:
  - devolve o usuário vinculado, se existe e está ativo;
  - ou cria `User.FromDirectory(name, email)` com o vínculo `IdentityLink`;
  - ou devolve nada, se a pessoa não está no diretório.

  A primeira entrada encontra o vínculo e usa o mesmo usuário, como já faz `SignInWithIdentity`.
- **`User.LastSignInAt` passa a opcional** (migração `UsersNeverSignedIn`). A tela Usuários mostra "Nunca entrou".
- **`/api/people` fica aberta a qualquer pessoa com sessão**, porque o líder técnico inclui pessoas e não tem o papel de criar projetos. Os nomes e e-mails da empresa já são visíveis a quem é da empresa (ADR-0007).

### 4. Dados das pessoas para a aba Membros

- `IUserDirectory` ganha `PeopleAsync(ids)`, que devolve nome, e-mail, último acesso, situação e se pode ser gerente.
- `GET /api/projects/{id}/members` devolve cada pessoa com `id`, `name`, `email`, `role`, `addedAt`, `lastSignInAt` e `canBeManager`, em ordem alfabética.
- O número de pessoas do cabeçalho vem de `members` do projeto, que a página já lê.

### 5. API

| Rota | Respostas |
| --- | --- |
| `GET /api/projects/{id}/members` | 200, 401, 404 |
| `POST /api/projects/{id}/members` `{ userId, directoryId, role }` | 201, 400 (`person`, `role`), 403, 404, 409 (`project-member-exists`, `project-closed`) |
| `PUT /api/projects/{id}/members/{userId}/role` `{ role }` | 204, 400 (`role`), 403, 404, 409 (`project-last-manager`, `project-closed`) |
| `DELETE /api/projects/{id}/members/{userId}` | 204, 403, 404, 409 (`project-last-manager`, `project-closed`) |

`POST /api/projects` ganha `techLeadDirectoryId`, opcional e exclusivo com `techLeadId`.

### 6. Auditoria: o projeto nas mudanças de papel

- **Marcação `IsAuditContext()`** no `Persistence`: o campo é gravado em toda criação, alteração e exclusão, mesmo sem mudar, com o valor antes e depois iguais. Ela marca o `ProjectId` das pessoas do projeto.
- **Na tela:** um campo com o antes igual ao depois não entra na tabela de mudanças, mas o texto do registro o usa ("Diego Torres · Colaborador · Implantação do ERP financeiro"). O mesmo campo serve depois ao filtro de projeto da Auditoria (ADR-0039).
- **Alternativa descartada:** pôr o projeto no rótulo do item. A pessoa deixaria de ser o rótulo, e o rótulo é o que a busca e a lista mostram primeiro.

### 7. Telas

- **Rotas:**
  - `ProjectLayout` em `/projects/:projectId`: lê o projeto e mostra o cabeçalho, "Ações do projeto" e os diálogos de dados e de status;
  - as abas são rotas filhas: o índice é a Visão geral, e `members` são os Membros.
  - as abas do cabeçalho viram links com `role="tab"` e `aria-selected`, e a aba Membros mostra o número de pessoas.
- **Membros:**
  - busca e "Por time / Por nome" na própria tela, sobre a lista lida da API, porque um projeto tem dezenas de pessoas, não milhares;
  - a lista usa o `DataTable` com linhas de grupo: a nova propriedade `isGroupRow` faz a linha ocupar todas as colunas (`colSpan`), com o estilo `fp-group-row`, mais baixa e sem escolha;
  - as páginas têm 100 linhas, o limite do Data Grid gratuito.
- **Detalhe:** no `DetailsDrawer` (ADR-0081), com o nome, o e-mail, o papel (um `Select` que salva ao trocar), o time, "No projeto desde", o último acesso e "Remover do projeto", com confirmação.
  - quem remove a si mesmo volta para Projetos;
  - as consultas dos projetos são invalidadas, e a barra lateral perde o projeto.
- **"Incluir pessoa":** um `Dialog` com o `PersonField`, que busca no sistema e no diretório e esconde quem já está no projeto, e o papel.
  - a opção Gerente de projeto não aparece para o líder técnico e fica desabilitada, com a explicação, para quem não pode ser gerente.
- **`PersonField`:** passa a escolher `{ id, directoryId, name }`, marca "(no diretório)" quem ainda não entrou e aceita `exclude`. No Novo projeto, o líder técnico manda `techLeadId` ou `techLeadDirectoryId`.

## Risks / Trade-offs

- [Usuário criado e inclusão recusada em seguida, por exemplo com o projeto encerrado no meio] → o usuário fica criado, ativo e sem projeto, como alguém que entrou e não está em nenhum projeto; nada fica inconsistente.
- [Diretório de demonstração só com a entrada local ligada] → em produção, até a M365-1, a busca traz só quem já entrou, como antes desta change.
- [`/api/people` aberta a todos com sessão] → devolve só nome, e-mail e se pode ser gerente, dados que a empresa já compartilha; nada de papel, situação ou último acesso.
- [Lista dos Membros sem paginação no servidor] → limitada pelo tamanho de um projeto; o Data Grid pagina de 100 em 100.

## Migration Plan

- Migração do `Users` (`LastSignInAt` opcional), sem perda: os usuários existentes mantêm o valor.
- Nenhuma migração no `Projects`: as pessoas já têm a tabela da Projetos-3.
