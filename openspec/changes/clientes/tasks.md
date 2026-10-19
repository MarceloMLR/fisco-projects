# Tasks

## 1. Backend

- [x] 1.1 Criar o módulo `Projects` e o `Contracts` com:
  - `Client`, `Cnpj` e `ClientLogo`;
  - contexto, configuração, migração `Initial` e registro no host;
  - os casos de uso dos clientes e do logo;
  - a política `SystemPolicies.ProjectCreators` no `Users`.

  Testar com `dotnet test`:
  - CNPJ numérico e alfanumérico, inválido e formatado;
  - CNPJ repetido, inclusive de cliente inativo;
  - nome e observação inválidos;
  - logo PNG e JPG, formato e tamanho recusados, troca de formato e falha ao registrar;
  - inativar, reativar e excluir com o logo;
  - busca sem acentos e por CNPJ;
  - testes de arquitetura com o módulo novo.
- [x] 1.2 Criar a API `/api/clients`.

  Testar com `dotnet test`:
  - a matriz de permissões (administrador, gerente de projeto, usuário e sem sessão);
  - os 400, 404 e 409;
  - o logo servido sem scripts e com cache;
  - a auditoria (incluir pelo gerente, alterar CNPJ e logo, inativar).

  Atualizar o contrato com `npm run api:update`.

## 2. Frontend

- [ ] 2.1 Criar a tela Clientes, como no canvas:
  - seção Cadastros com o total;
  - lista com logo ou iniciais, CNPJ formatado, situação e observação;
  - busca e filtro de situação;
  - rodapé com o total e os ativos;
  - menu com Editar, Inativar ou Reativar e Excluir com confirmação.

  Testar com `npm test` "Clientes na Administração", "Buscar cliente", "Filtrar por situação", "Excluir cliente pela tela", inativar e reativar.
- [ ] 2.2 Criar o `ClientDialog` (novo e editar), com:
  - máscara de CNPJ;
  - logo para o administrador;
  - CNPJ repetido no campo.

  Também os textos da Auditoria e o tipo Clientes no filtro. Testar com `npm test` e `npm run lint`:
  - "Novo cliente pela tela" e "Editar cliente pela tela";
  - CNPJ inválido e repetido;
  - logo recusado;
  - os títulos da Auditoria.

## 3. Verificação final

- [ ] 3.1 Conferir no navegador, com o build de produção:
  - a tela Clientes, o diálogo com logo, a exclusão e a Auditoria;
  - nos temas claro e escuro, no computador e no celular.

  Mandar as capturas para Marcelo.
- [ ] 3.2 Documentação e verificações:
  - glossário, `CLAUDE.md` (módulo `Projects`) e roadmap;
  - rodar todos os scripts de `scripts/ci` sem erro;
  - abrir o PR e confirmar a verificação `verify`.

## Workflow follow-up

- Marcar a Projetos-2 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo:
  - `/opsx:archive` na mesma branch;
  - roadmap como "Concluída";
  - merge.
