# Tasks

## 1. Auditoria

- [x] 1.1 Referências na auditoria (ADR-0084):
  - `IsAuditReference` e `HasAuditLabelReference` no `Persistence`;
  - `AuditField.Reference` e a coluna `ItemLabelReference`, com migração;
  - `IAuditReferenceNames` no `SharedKernel`, com a fonte `User` no `Users`;
  - nomes resolvidos na listagem e na API.

  Testar com `dotnet test`:
  - o campo e o rótulo marcados gravados com o identificador e o tipo;
  - o nome atual na lista;
  - o item que não existe mais, sem nome;
  - o tipo sem fonte de nomes.

## 2. Backend dos projetos

- [x] 2.1 Domínio e aplicação no `Projects`:
  - `Project`, `ProjectCode`, `ProjectStatus`, `ProjectMember`, `ProjectRole` e `ProjectPermissions`;
  - casos de uso de listar, abrir, criar, alterar e mudar o status;
  - configuração, migração e as fontes de nomes `Client` e `Project`;
  - `RemoveClient` recusando o cliente com projetos.

  Testar com `dotnet test`:
  - o código válido, inválido, repetido e em maiúsculas;
  - o gerente e o líder técnico na criação;
  - o status, o projeto encerrado e a reabertura;
  - a matriz de permissões;
  - a visibilidade;
  - o cliente com projetos.
- [x] 2.2 API `/api/projects`, `/api/people` e `/api/clients/choices`.

  Testar com `dotnet test`:
  - a matriz de permissões, com administrador, gerente, líder técnico, colaborador, desenvolvedor, leitor, pessoa de fora e sem sessão;
  - os 400, 404 e 409;
  - a auditoria da criação com o gerente pelo nome.

  Atualizar o contrato com `npm run api:update`.

## 3. Frontend

- [x] 3.1 Tela Projetos e Meus projetos na barra lateral.

  Testar com `npm test`:
  - "Projetos na lista", "Projetos encerrados pelo filtro", "Buscar e filtrar projetos";
  - "Usuário sem Novo projeto";
  - "Meus projetos na barra lateral".
- [x] 3.2 Tela Novo projeto, com "Cadastrar cliente" e a troca do gerente.

  Testar com `npm test`:
  - "Novo projeto pela tela", "Cadastrar cliente na criação", "Trocar o gerente na criação";
  - código inválido e repetido.
- [x] 3.3 Página do projeto:
  - cabeçalho, trilha com o código, ações, edição, status com confirmação, somente leitura e reabertura;
  - textos da Auditoria e os tipos no filtro;
  - a exclusão recusada do cliente com projetos na tela Clientes.

  Testar com `npm test`:
  - "Cabeçalho do projeto", "Editar dados pela tela", "Concluir projeto pela tela";
  - os títulos da Auditoria;
  - "Cliente com projetos".

## 4. Verificação final

- [x] 4.1 Conferir no navegador, com o build de produção:
  - Projetos, Novo projeto, a página do projeto e as ações, e a Auditoria;
  - nos temas claro e escuro, no computador e no celular.

  Mandar as capturas para Marcelo.
- [ ] 4.2 Documentação e verificações:
  - glossário, `CLAUDE.md` e roadmap;
  - rodar todos os scripts de `scripts/ci` sem erro;
  - abrir o PR e confirmar a verificação `verify`.

## Workflow follow-up

- Marcar a Projetos-3 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo:
  - `/opsx:archive` na mesma branch;
  - o ADR-0084 como aceito;
  - roadmap como "Concluída";
  - merge.
