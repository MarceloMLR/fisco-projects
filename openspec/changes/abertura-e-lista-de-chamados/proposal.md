# Proposta: abertura e lista de chamados (Chamados-1)

## Why

O FiscoProjects ainda não tem chamados, a segunda metade do produto. Os ADRs já decidiram como eles nascem e quem os vê:
- todo chamado pertence a um projeto, e projeto encerrado não recebe chamados (ADR-0022);
- o número é sequencial e único na instalação, como #104 (ADR-0029);
- só os times Funcional e Desenvolvimento recebem chamados, sem filas, e o chamado entra sem responsável (ADR-0068);
- todos os membros do projeto veem todos os chamados dele, e o leitor não abre chamados (ADR-0044).

O canvas aprovado desenhou a página Chamados, a aba Chamados do projeto, o "Abrir chamado" e a página do chamado.

## What Changes

- **Módulo `Tickets`**, novo, com o schema `tickets`:
  - o chamado guarda o número, o projeto, o time, a prioridade, o título, a descrição, o status, o solicitante, o responsável, a abertura e a última atualização;
  - o número sai de uma sequência do banco, sem repetir nem com aberturas simultâneas.
- **Abrir chamado** (`POST /api/tickets`):
  - projeto, time, prioridade, título e descrição, sem responsável;
  - o chamado nasce Aberto, com quem abriu como solicitante;
  - abrem o administrador, o gerente, o líder técnico, o colaborador e o desenvolvedor do projeto;
  - o leitor recebe 403, quem está fora do projeto 404 e o projeto concluído ou cancelado 409 (ADR-0022, ADR-0044);
  - só aparecem os times com alguém para atender: Funcional com colaboradores, Desenvolvimento com o líder técnico ou desenvolvedores (ADR-0068).
- **Lista de chamados** (`GET /api/tickets`):
  - o administrador vê todos, e cada pessoa os dos projetos em que está, inclusive como leitora (ADR-0044);
  - visões "Atribuídos a mim", "Sem responsável nos meus times", "Abertos por mim" e "Todos os abertos", com a contagem de cada uma (`GET /api/tickets/counts`);
  - filtros por projeto, time e situação ("Em aberto", "Encerrados", "Todos");
  - busca pelo número, pelo título e pela descrição;
  - ordem pela prioridade, a mais alta primeiro, ou pelo número, com a paginação no servidor.
- **Página do chamado** (`GET /api/tickets/{number}`, tela `/tickets/104`): o cabeçalho, a descrição e as pessoas (solicitante, responsável, time, gerente e líder técnico).
- **Telas:**
  - a página **Chamados**, com as visões ao lado, os filtros e a tabela;
  - a aba **Chamados** do projeto, com "Todos", "Em aberto", "Sem responsável" e "Abertos por mim";
  - o **Abrir chamado**, pela página Chamados e pela aba do projeto, com o projeto já preenchido. Nunca pela tela inicial (ADR-0068).
- **Auditoria:** a abertura entra na auditoria como "Chamado", pelo número e pelo título, no contexto do projeto.
- **Dados de demonstração:** chamados fictícios nos projetos, abertos por pessoas diferentes.

## Non-goals

Cada item fica para a change do roadmap que traz o dado de que ele depende:
- **Chamados-2:**
  - a conversa, o editor com formatação (Tiptap), as menções e os anexos;
  - por isso a descrição é texto simples nesta change;
  - também a visão "Que acompanho", porque quem acompanha nasce das menções, e a contagem de interações na lista.
- **Chamados-3:**
  - o fluxo de status;
  - o responsável definido pela primeira resposta. Até lá, todo chamado está Aberto e sem responsável.
- **Chamados-4:** os prazos, a coluna "Status e prazo" com o prazo e o filtro "Só prazo vencido".
- **Chamados-5:**
  - a troca do responsável e do solicitante, com as caixas de seleção, as ações em lote e o "Atribuir" da linha;
  - a transferência entre times.
- **Chamados-6:** a visão "Com fornecedor".

## Capabilities

### New Capabilities

- `chamados`: o número, a abertura, quem abre e quem vê, os times, a lista com visões e filtros, a página do chamado, as telas e a auditoria.

### Modified Capabilities

- `dados-de-demonstracao`: os chamados de exemplo.

## Impact

- **Backend:**
  - o módulo `Tickets` novo, com a migração, os casos de uso, as rotas e as permissões;
  - no `Projects.Contracts`, o contrato `IProjectAccess`, para o `Tickets` saber os projetos que cada pessoa vê, o papel dela em cada um e quem está em cada time, sem ler as tabelas do `Projects`.
- **Frontend:**
  - a página Chamados no lugar da página vazia de hoje;
  - a aba Chamados do projeto;
  - o "Abrir chamado" e a página do chamado;
  - o tipo "Chamado" na Auditoria.
- **Docs:** `CLAUDE.md`, glossário e roadmap.
