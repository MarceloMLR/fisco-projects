# Proposta: clientes (Projetos-2)

## Why

O projeto tem cliente opcional, escolhido num cadastro, para os filtros e as importações por cliente serem confiáveis (ADR-0015). O cadastro é do administrador, mas o gerente de projeto precisa incluir um cliente novo na criação do projeto, sem depender da Administração (ADR-0046). A Projetos-3 cria os projetos, então o cadastro vem antes.

## What Changes

- **Novo módulo `Projects`** (schema `projects`), que nesta change guarda os clientes e, na Projetos-3, os projetos:
  - nome obrigatório;
  - CNPJ opcional, validado pelos dígitos verificadores, numérico ou alfanumérico (IN RFB nº 2.229/2024), e único;
  - logo PNG ou JPG de até 1 MB;
  - observação;
  - situação Ativo ou Inativo.
- **API `/api/clients`:**
  - listar, com busca por nome ou CNPJ e filtro de situação, e incluir: administrador e gerente de projeto (ADR-0043, ADR-0046);
  - alterar, inativar, reativar, excluir e trocar ou remover o logo: só o administrador;
  - abrir o logo: qualquer pessoa com sessão, porque o logo aparece nas listas de projetos.
- **Tela Clientes** na seção Cadastros da Administração, como no canvas (AdminClientes):
  - lista com o logo (ou as iniciais), o nome, o CNPJ formatado, a situação e a observação;
  - busca e filtro de situação;
  - "Novo cliente" e "Editar cliente" num diálogo, com o logo;
  - menu com Inativar ou Reativar e Excluir, com confirmação.

  O diálogo de cliente fica pronto para o "Cadastrar cliente" da criação do projeto (Projetos-3).
- **Auditoria:** incluir, alterar, inativar, reativar, excluir e trocar o logo entram como mudanças do cliente.

## Non-goals

- A coluna "Projetos ativos" e a recusa de excluir um cliente com projetos, que dependem dos projetos (Projetos-3). Nesta change, todo cliente pode ser excluído.
- O "Cadastrar cliente" no formulário Novo projeto (Projetos-3).
- O logo do cliente nas listas e no cabeçalho do projeto (Projetos-3 e telas de síntese).
- Contatos do cliente (ADR-0015, "depois").

## Capabilities

### New Capabilities

- `clientes`, que cobre:
  - o cadastro de clientes e as suas regras (CNPJ, logo, situação e exclusão);
  - quem lista, inclui e altera;
  - a tela Clientes da Administração;
  - a auditoria dessas mudanças.

### Modified Capabilities

Nenhuma.

## Impact

- **Backend:**
  - módulo novo `FiscoProjects.Projects` e `FiscoProjects.Projects.Contracts`, com a migração inicial;
  - a política `SystemPolicies.ProjectCreators` (administrador e gerente de projeto) no módulo `Users`;
  - logo pelo `IFileStorage`, na pasta do FiscoProjects (ADR-0030);
  - testes do módulo e da API, com a matriz de permissões.
- **Contrato OpenAPI** e tipos do frontend.
- **Frontend:**
  - rota `/admin/clients` e seção Cadastros na coluna da Administração, com o total de clientes;
  - consultas e alterações pelo TanStack Query;
  - diálogo com React Hook Form e Zod, campo de CNPJ com máscara;
  - textos da auditoria, traduções e testes.
- **Documentação:**
  - `docs/glossario.md`: cliente, CNPJ, logo do cliente, situação;
  - `CLAUDE.md`: módulo `Projects`;
  - `docs/roadmap.md`.
- **ADRs:**
  - 0015 (cadastro de clientes) e 0046 (cadastros do administrador, com o cliente novo pelo gerente);
  - 0043 (quem cria projetos), 0030 (logos na pasta do FiscoProjects) e 0039 (auditoria);
  - 0047 (módulo Projects, como no discovery), 0066 e 0081 (estrutura das telas).
