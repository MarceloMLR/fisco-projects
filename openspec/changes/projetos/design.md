# Design: projetos (Projetos-3)

## Context

- **Módulo `Projects`:** criado na Projetos-2 com os clientes (schema `projects`, tabela `clients`). Os projetos entram no mesmo módulo, como previsto no design dos clientes.
- **Usuários:** o `Users` oferece `IUserAccess` (papel e situação de uma pessoa) e `IUserDirectory` (nomes). A lista `/api/users` é só do administrador.
- **Políticas:** `SystemPolicies.Administrator` e `SystemPolicies.ProjectCreators` (administrador e gerente de projeto).
- **Auditoria:** cada campo é gravado como texto; a lista resolve só o nome do autor, pelo `IUserDirectory`. Não existe forma de mostrar um campo que aponta para outro item.
- **Canvas:**
  - Projetos: busca, filtro de cliente, situação e as colunas Projeto, Status, Cronograma, Prazo final, Gerente e Chamados;
  - NovoProjeto: Dados do projeto, Cronograma e a prévia;
  - ProjetoCabecalho: logo ou iniciais do cliente, nome, status, código, cliente, gerente, líder técnico, datas, "Ações do projeto" e as abas;
  - a barra lateral com Meus projetos.
- **Trilha:** a barra superior monta a trilha só com títulos fixos das rotas.

## Goals / Non-Goals

**Goals:**

- O projeto e as pessoas do projeto num agregado só, com a regra de permissões num lugar só, pronta para a Projetos-4 e para os outros módulos.
- Um mecanismo único para a auditoria mostrar referências pelo nome (ADR-0084).

**Non-Goals:**

- Contrato do `Projects` para outros módulos: nenhum módulo lê projetos nesta change. Ele nasce com o primeiro módulo que precisar (Cronograma-1 ou Chamados-1).
- Incluir e remover pessoas depois da criação (Projetos-4).

## Decisions

### 1. Domínio

- **`Project`:**
  - `Code`, `Name`, `Description`, `ClientId` e `Status`;
  - `CreatedAt`, fora da auditoria;
  - as pessoas em `Members`, criadas junto com o projeto.

  Métodos:
  - `Create`, com o gerente e o líder técnico;
  - `Change`, que recusa o projeto encerrado;
  - `ChangeStatus`.
- **`ProjectCode`:**
  - de 2 a 15 caracteres: letras, algarismos e hífens, começando por letra, sem hífen no fim nem dois seguidos;
  - normalizado em maiúsculas.
- **`ProjectStatus`:** `Planning`, `InProgress`, `Paused`, `Completed` e `Cancelled`. Concluído e cancelado são encerrados.
- **`ProjectMember`:**
  - `ProjectId`, `UserId` e `Role`, com um papel por pessoa (índice único);
  - `AddedAt`, fora da auditoria.
- **`ProjectRole`:** `Manager`, `TechLead`, `Contributor`, `Developer` e `Viewer` (ADR-0040). Nesta change só os dois primeiros são criados; os outros servem à matriz e à Projetos-4.
- **`ProjectPermissions`:** a matriz do ADR-0043 para o projeto, a partir do papel no sistema e do papel no projeto:
  - `CanView`;
  - `CanEdit`: administrador, gerente e líder técnico, com o projeto aberto;
  - `CanChangeStatus`: administrador e gerente.

  A API devolve essas permissões com o projeto, e a tela mostra só o que a pessoa pode fazer.

### 2. Aplicação

- **Leitura:**
  - `ListProjects`: busca por código ou nome, cliente, situação (`Active`, `Closed`, `All`), "só os meus" e página;
  - `GetProject`: devolve o projeto e as permissões, ou nada quando a pessoa não vê o projeto.

  O administrador vê todos os projetos; as outras pessoas, só os projetos em que estão.
- **`CreateProject`:**
  - confere os campos, o código único e o cliente ativo;
  - confere o gerente pelo `IUserAccess`: ativo, com o papel Gerente de projeto ou Administrador;
  - confere o líder técnico: ativo e diferente do gerente.
- **`ChangeProject`:**
  - confere a permissão e o projeto aberto;
  - confere o código e o cliente: um cliente inativo só vale se já era o do projeto.
- **`ChangeProjectStatus`:** confere a permissão; o mesmo status não muda nada.
- **Código repetido:** conferido antes de salvar e garantido pelo índice único (`IX_projects_Code`), cuja violação vira a mesma resposta 409, como no CNPJ.
- **Nomes:** os nomes do gerente e do líder técnico vêm do `IUserDirectory`.
- **`RemoveClient`:** recusa o cliente com projetos. A chave estrangeira `ClientId` com `Restrict` garante a regra no banco.

### 3. API

Rotas em `/api/projects`:

| Rota | Quem | Respostas |
| --- | --- | --- |
| `GET /?search=&clientId=&situation=&mine=&page=&pageSize=` | sessão | 200 `{ items: [{ id, code, name, client, status, managers }], total }` |
| `POST /` `{ code, name, description, clientId, managerId, techLeadId }` | ProjectCreators | 201 `{ id }`; 400 com os campos; 409 `project-code-in-use` com `projectName` |
| `GET /{id}` | quem vê | 200 `{ id, code, name, description, client, status, members, permissions }`; 404 |
| `PUT /{id}` `{ code, name, description, clientId }` | quem altera | 204; 400; 403; 404; 409 `project-code-in-use` ou `project-closed` |
| `PUT /{id}/status` `{ status }` | quem muda o status | 204; 403; 404 |

Outras rotas:

- **`GET /api/people?search=&managers=&limit=`:** ProjectCreators. Pessoas ativas `{ id, name, email }`, em ordem alfabética, até 100; com `managers=true`, só administradores e gerentes de projeto. Vive no `Users`.
- **`GET /api/clients/choices`:** qualquer sessão. Todos os clientes `{ id, name, logoUrl, status }`, em ordem alfabética, para o campo Cliente e o filtro da lista.
- **`DELETE /api/clients/{id}`:** passa a responder 409 `client-has-projects`.

Detalhes:

- `client` é `{ id, name, logoUrl }` ou `null`.
- `managers` (na lista) e `members` (no projeto) trazem `{ id, name, role }`; a tela tira do `members` o gerente e o líder técnico.
- `permissions` é `{ canEdit, canChangeStatus }`.

### 4. Referências na auditoria (ADR-0084)

- **`Persistence`:**
  - `PropertyBuilder.IsAuditReference(kind)` marca um campo;
  - `EntityTypeBuilder.HasAuditLabelReference(x => x.UserId, kind)` faz do rótulo uma referência;
  - o `ChangeCapture` grava o identificador como texto, o tipo em `AuditedField.Reference` e o tipo do rótulo em `AuditedChange.ItemLabelReference`.
- **`Audit`:**
  - `AuditField.Reference` no JSON dos campos, nulo nos registros antigos;
  - coluna nova `ItemLabelReference`, com migração.
- **`SharedKernel`:** `IAuditReferenceNames`, com `Kind` e `NamesAsync(ids)`.

  Quem oferece os nomes:
  - o `Users`, para `User`;
  - o `Projects`, para `Client` e `Project`.
- **Listagem:** `ListAuditEntries` junta os identificadores de uma página por tipo e pede os nomes de uma vez. Um tipo sem fonte e um item que não existe mais ficam sem nome.
- **API:**
  - `AuditFieldResponse` ganha `reference`, e `before` e `after` trazem os nomes;
  - `itemLabel` traz o nome quando o rótulo é uma referência.
- **Marcações do `Projects`:**

  | Item | Rótulo | Referências | Fora da auditoria |
  | --- | --- | --- | --- |
  | `Project` | `Name` | `ClientId` (Client) | `CreatedAt` |
  | `ProjectMember` | `UserId` (User) | `ProjectId` (Project) e `UserId` (User) | `AddedAt` |

### 5. Frontend

- **Projetos (`/projects`):**
  - `DataTable` com as colunas Projeto (nome e "código · cliente"), Status, Cronograma ("Sem cronograma"), Prazo final ("—") e Gerente;
  - escolher a linha abre o projeto;
  - busca (300 ms), `Select` de cliente com os clientes para escolher, `SegmentedControl` de situação (Ativos por padrão) e rodapé "N projetos";
  - "Novo projeto" só para o administrador e o gerente de projeto; no celular, embaixo, como no Calendário.
- **Status:** `ProjectStatusBadge` com os ícones e tons do design system:
  - Planejamento: neutro, `event_note`;
  - Em andamento: informação, `autorenew`;
  - Pausado: alerta, `pause_circle`;
  - Concluído: sucesso, `check_circle`;
  - Cancelado: neutro, `block`.
- **Novo projeto (`/projects/new`):**
  - a seção "Dados do projeto" do canvas, sem número e sem a prévia, até o cronograma existir;
  - Código (200 px) e Nome; Cliente com "Cadastrar cliente"; Gerente e Líder técnico num campo com a aparência do `Select` que busca em `/api/people` enquanto a pessoa digita, como o filtro de pessoa da Auditoria, para servir a qualquer número de pessoas; Descrição em `TextArea` de 4 linhas;
  - barra de ações com Cancelar e "Criar projeto";
  - o `ClientDialog` da Projetos-2 com `canSendLogo` só para o administrador.
- **Página do projeto (`/projects/:projectId`):**
  - `ProjectHeader` (fp-proj), com logo de 44 px e as abas;
  - "Ações do projeto" num menu: Editar dados; Planejamento, Em andamento e Pausado; "Concluir projeto" e "Cancelar projeto", com confirmação; e "Reabrir projeto" no projeto encerrado;
  - `ProjectDialog` para editar os dados;
  - aba Visão geral com os cartões Descrição e Equipe: Gestão com gerente e líder técnico, Funcional com colaboradores, Desenvolvimento com líder técnico e desenvolvedores;
  - aviso de somente leitura no projeto encerrado.
- **Trilha:** um nível da trilha pode ser o projeto da rota, mostrado pelo código e ligado à página do projeto.
- **Barra lateral:** Meus projetos com os projetos ativos em que a pessoa está, por `mine=true`, como no design system (nome e código). O projeto aberto fica marcado.
- **Depois de uma alteração:** invalida `['projects']` e `['audit']`; ao cadastrar um cliente, invalida `['clients']`.

### 6. Auditoria na tela

`auditText.ts` ganha:

- **Projeto:**
  - "Criou um projeto" e "Alterou um projeto";
  - para uma mudança só de status: "Concluiu um projeto", "Cancelou um projeto", "Reabriu um projeto" ou "Mudou o status de um projeto";
  - status em palavras.
- **Pessoas do projeto:**
  - "Incluiu uma pessoa num projeto", "Trocou o papel de uma pessoa num projeto" e "Removeu uma pessoa de um projeto";
  - papel em palavras.
- **Filtro:** os tipos Projetos e Pessoas dos projetos.

## Risks / Trade-offs

- [O papel no projeto é lido a cada requisição] → uma consulta pela chave do projeto e da pessoa, como o papel no sistema.
- [Pessoas que nunca entraram não podem ser gerente nem líder técnico] → a Projetos-4 traz a busca no diretório, que cria o usuário.
- [A Visão geral provisória] → a Síntese-2 troca o corpo da aba, e o cabeçalho fica.
- [Um nome resolvido ao listar muda o histórico de quem foi renomeado] → aceito no ADR-0084.

## Migration Plan

- **`Projects`:** migração com as tabelas `projects` e `project_members`, o índice único do código, o índice único de pessoa por projeto e a chave estrangeira do cliente com `Restrict`. Sem dados.
- **`Audit`:** migração com a coluna `ItemLabelReference`. Os registros antigos ficam sem referências.
