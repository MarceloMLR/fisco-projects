# Proposta: projetos (Projetos-3)

## Why

O FiscoProjects reúne o trabalho por projeto: cronograma, chamados, documentos e termos pertencem a um projeto (ADR-0022). Com o calendário e os clientes prontos, falta o próprio projeto: criar com o gerente e o líder técnico (ADR-0072), listar, mudar o status e deixar o projeto encerrado somente leitura (ADR-0016, ADR-0043).

## What Changes

- **Projetos no módulo `Projects`:**
  - código curto e único (como ERP-FIN), nome, descrição e cliente opcional (ADR-0015, ADR-0016);
  - status Planejamento, Em andamento, Pausado, Concluído ou Cancelado, que começa em Planejamento;
  - pessoas do projeto com o papel no projeto: nesta change, o gerente e o líder técnico definidos na criação (ADR-0040, ADR-0072).
- **Quem faz o quê** (ADR-0043):
  - criar: administrador e gerente de projeto (papel no sistema);
  - ver: o administrador e as pessoas do projeto; para os outros, o projeto não existe;
  - alterar código, nome, descrição e cliente: administrador, gerente e líder técnico;
  - mudar o status, inclusive reabrir: administrador e gerente.
- **Criação** (ADR-0072):
  - o gerente vem preenchido com quem cria e pode ser trocado por outra pessoa com o papel Gerente de projeto ou Administrador; com outro gerente, quem cria não entra no projeto;
  - o líder técnico é opcional e entra com o papel Líder técnico.
- **Projeto encerrado** (ADR-0016): concluído ou cancelado, fica somente leitura até ser reaberto e sai da lista padrão, visível pelo filtro.
- **Cliente com projetos** não pode ser excluído, só inativado (ADR-0015).
- **Telas:**
  - Projetos (canvas Projetos): busca, filtro de cliente, situação Ativos, Concluídos e cancelados ou Todos, e "Novo projeto";
  - Novo projeto (canvas NovoProjeto), com "Cadastrar cliente";
  - página do projeto com o cabeçalho do canvas (ProjetoCabecalho), "Ações do projeto" e a aba Visão geral;
  - Meus projetos na barra lateral, como no canvas.
- **Auditoria** (ADR-0039, ADR-0072): a criação registra quem criou, o projeto e quem foi definido como gerente. O cliente, a pessoa e o projeto aparecem pelo nome, por referências guardadas pelo identificador (ADR-0084, proposto nesta change).

## Non-goals

Cada item abaixo entra com a change indicada:

- Seção Cronograma e prévia no Novo projeto, colunas Cronograma e Prazo final com dados, datas no cabeçalho: Cronograma-1 e Cronograma-7. Até lá, o projeto mostra "Sem cronograma" (ADR-0016).
- Números e cartões da Visão geral: Síntese-2. Até lá, a aba mostra a descrição e a equipe, por escolha de Marcelo.
- Coluna Chamados e as regras de encerramento com chamados (concluir só com chamados fechados, cancelar fecha os abertos): Chamados-1 e Chamados-7.
- Incluir, trocar e remover pessoas e escolher pessoas que nunca entraram, pelo diretório: Projetos-4. Nesta change, o gerente e o líder técnico são escolhidos entre os usuários ativos, por escolha de Marcelo.
- Avisos às pessoas incluídas: Avisos-2.
- Excluir projeto: Operação-2 (ADR-0041).
- Filtro de projeto na Auditoria (ADR-0039): fica para quando os módulos do projeto existirem.

## Capabilities

### New Capabilities

- `projetos`: os dados e o status do projeto, quem cria, vê e altera, a criação com gerente e líder técnico, o projeto encerrado somente leitura, as telas Projetos, Novo projeto e a página do projeto, e a auditoria dessas mudanças.

### Modified Capabilities

- `clientes`: o cliente com projetos não pode ser excluído.
- `auditoria`: campos e rótulos que apontam para outro item aparecem pelo nome atual (ADR-0084).

## Impact

- **Backend:**
  - módulo `Projects`: projetos e pessoas do projeto, com as regras de permissão num lugar só; migração `Projects`;
  - `Users`: lista de pessoas para escolher o gerente e o líder técnico (`/api/people`) e os nomes das pessoas para a auditoria;
  - `Persistence` e `Audit`: marcação de referências e nomes pedidos aos módulos donos; migração do `Audit`;
  - `SharedKernel`: o contrato dos nomes de referências;
  - testes do módulo e da API, com a matriz de permissões.
- **Contrato OpenAPI** e tipos do frontend.
- **Frontend:**
  - rotas `/projects`, `/projects/new` e `/projects/:projectId`;
  - Meus projetos na barra lateral e o código do projeto na trilha;
  - textos da auditoria, traduções e testes.
- **Documentação:**
  - `docs/glossario.md`: projeto, código, status, pessoas do projeto, papel no projeto;
  - `CLAUDE.md`: projetos no módulo `Projects` e referências na auditoria;
  - `docs/roadmap.md`.
- **ADRs:**
  - implementa 0016, 0043 (linhas do projeto) e 0072, com 0015, 0039, 0040 e 0047;
  - propõe 0084 (referências na auditoria);
  - segue 0066, 0080 e 0081 nas telas.
