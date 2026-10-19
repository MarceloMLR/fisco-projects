# Proposta: membros do projeto (Projetos-4)

## Why

Com a Projetos-3, o projeto nasce só com o gerente e o líder técnico. Para o projeto ter o time que atende os chamados e cuida das tarefas, o gerente e o líder técnico precisam incluir pessoas, trocar o papel delas e remover quem sai, sem nunca deixar o projeto sem gerente (ADR-0040, ADR-0042). Quem ainda não entrou no sistema também precisa poder ser incluído, buscando no diretório da empresa (ADR-0005).

## What Changes

- **Aba Membros** da página do projeto (canvas ProjetoMembros):
  - as pessoas do projeto por time (Gestão, Funcional, Desenvolvimento e Leitores) ou por nome, com o papel e o último acesso, e busca;
  - "Incluir pessoa", com a busca no diretório e o papel no projeto;
  - detalhe da pessoa numa gaveta (ADR-0081), com a troca do papel e "Remover do projeto";
  - a aba no cabeçalho, com o número de pessoas.
- **Regras** (ADR-0042, ADR-0043):
  - administrador e gerente incluem, removem e trocam o papel de qualquer pessoa;
  - o líder técnico faz o mesmo, menos com gerentes: não inclui, não remove, não rebaixa nem promove gerentes;
  - colaborador, desenvolvedor e leitor não mexem nas pessoas;
  - um papel por pessoa em cada projeto, e o projeto sempre tem ao menos um gerente;
  - quem é removido perde o acesso ao projeto na requisição seguinte;
  - projeto encerrado não muda as pessoas (ADR-0016).
- **Gerente só com o papel no sistema** Gerente de projeto ou Administrador, como na criação (ADR-0085, proposto nesta change, por escolha de Marcelo).
- **Diretório da empresa, versão simulada** (ADR-0005, ADR-0078):
  - a busca de pessoas passa a trazer, além de quem já entrou, as pessoas do diretório que ainda não entraram; nesta versão, o diretório é o de demonstração da entrada local;
  - incluir uma dessas pessoas cria o usuário, com o papel Usuário e sem último acesso; na primeira entrada, ela usa esse mesmo usuário;
  - a busca fica aberta a qualquer pessoa com sessão, porque o líder técnico também inclui pessoas;
  - no Novo projeto, o líder técnico também pode vir do diretório, por escolha de Marcelo.
- **Auditoria** (ADR-0039, ADR-0084): incluir, trocar o papel e remover pessoas entram na auditoria, com a pessoa, o papel e o projeto pelo nome; o usuário criado pelo diretório aparece como criado por quem incluiu.

## Non-goals

Cada item abaixo entra com a change indicada:

- Colunas Chamados e Tarefas, as listas de chamados e tarefas da pessoa no detalhe e "Passar pendências": Cronograma e Chamados (grupos 4 e 5), por escolha de Marcelo. Até lá, remover pede só a confirmação.
- Avisos à pessoa incluída ou com o papel trocado (resumo diário, ADR-0037): Avisos-2.
- Diretório real do Entra pelo Microsoft Graph: M365-1. A interface do diretório desta change é a que o Graph vai implementar (ADR-0004).
- Incluir um grupo do Entra inteiro e período de participação: depois (ADR-0042).

## Capabilities

### New Capabilities

- `membros-do-projeto`: a aba Membros, quem inclui, troca o papel e remove pessoas, o último gerente protegido, o gerente com o papel no sistema e a auditoria dessas mudanças.

### Modified Capabilities

- `projetos`: o líder técnico da criação pode vir do diretório, e o cabeçalho ganha a aba Membros.
- `usuarios-e-papeis`: as pessoas para escolher vêm também do diretório, e quem é incluído pelo diretório vira usuário sem último acesso, mostrado na tela Usuários.
- `entrada-e-sessao`: a primeira entrada de quem foi incluído pelo diretório usa o usuário já criado.

## Impact

- **Backend:**
  - `Projects`: incluir, trocar o papel e remover pessoas no `Project`, as permissões das pessoas em `ProjectPermissions` e as rotas `/api/projects/{id}/members`;
  - `Users`: o diretório (`IPeopleDirectory`, com a implementação da entrada local), a busca de pessoas com o diretório, o usuário criado pelo diretório por um contrato do `Users.Contracts`, o último acesso opcional (migração) e os dados das pessoas para a aba Membros;
  - testes do domínio, dos casos de uso e da API, com a matriz de permissões.
- **Contrato OpenAPI** e tipos do frontend.
- **Frontend:**
  - rota `/projects/:projectId/members` e as abas do cabeçalho como links;
  - `DataTable` com linhas de grupo;
  - `PersonField` com as pessoas do diretório;
  - "Nunca entrou" na tela Usuários;
  - textos da auditoria, traduções e testes.
- **Documentação:** glossário, `CLAUDE.md` e roadmap.
- **ADRs:** implementa 0042 e a linha das pessoas do 0043, com 0005, 0040, 0072 e 0078; propõe 0085; segue 0066, 0080 e 0081 nas telas.
