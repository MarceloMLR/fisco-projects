# Design: abertura e lista de chamados (Chamados-1)

## Context

- **ADRs:** o ADR-0022 liga todo chamado a um projeto. O ADR-0029 decide o número único na instalação e a lista. O ADR-0068 deixa só os times Funcional e Desenvolvimento, sem responsável na abertura. O ADR-0044 mostra todos os chamados a todo o projeto.
- **Módulos:** o ADR-0047 prevê um módulo `Tickets`. As regras de quem está em cada projeto ficam no `Projects`, e o `Tickets` não pode ler as tabelas dele.
- **Canvas:** desenhou as telas `Chamados`, `ProjetoChamados`, `AbrirChamado` e `Chamado`. Esta change entrega a parte delas que não depende das changes seguintes do grupo.

## Goals / Non-Goals

**Goals:**

- Abrir chamados e encontrá-los, com as regras de acesso dos ADRs.
- Deixar o módulo `Tickets` pronto para a conversa, o fluxo, os prazos e as trocas.

**Non-Goals:** os da proposta: conversa, editor, anexos, fluxo, prazos, trocas e fornecedores.

## Decisions

### 1. Módulo `Tickets` e o contrato `IProjectAccess`

- **`Tickets`:** módulo novo, com o schema `tickets` e a tabela `tickets`.
- **`IProjectAccess` (`Projects.Contracts`):** o `Tickets` sabe dos projetos só por este contrato.
  - `VisibleAsync`: os projetos que a pessoa vê, com o código, o nome, se está encerrado e o papel dela. O administrador vê todos, com o papel só onde está.
  - `FindAsync`: um projeto como a pessoa o vê, ou nada.
  - `MembersAsync`: as pessoas do projeto e o papel de cada uma.
- **Papel no contrato:** vai num `ProjectMemberRole` próprio, com os mesmos valores do `ProjectRole`, porque o `Contracts` não usa o interior do módulo.
- **Alternativa:** o `Tickets` guardar uma cópia dos membros por eventos. Recusada nesta change: duas fontes da mesma regra, e o volume de uma instalação é pequeno.

### 2. Regras de acesso num lugar só

As regras ficam em `TicketAccess`, no domínio do `Tickets`, a partir do papel no projeto:
- **Ver:** todo membro do projeto e o administrador.
- **Abrir:** o administrador, o gerente, o líder técnico, o colaborador e o desenvolvedor, com o projeto aberto.
- **Times que atendem:**
  - Funcional: os colaboradores;
  - Desenvolvimento: o líder técnico e os desenvolvedores;
  - um time sem ninguém fica fora da abertura.
- **"Meus times", para a visão "Sem responsável nos meus times":**
  - o gerente e o líder técnico contam os dois times dos projetos deles, porque distribuem os chamados (ADR-0068);
  - o colaborador conta o Funcional e o desenvolvedor, o Desenvolvimento;
  - o leitor não conta nenhum, e o administrador, nenhum fora dos projetos em que está.
  - É a leitura do canvas: a gerente vê nessa visão os chamados sem responsável dos dois times.

### 3. Número por sequência do banco

- O número sai da sequência `tickets.ticket_numbers`, pedida na hora de salvar (`NEXT VALUE FOR`, pelo `HasDefaultValueSql`).
- A sequência não repete o número em aberturas simultâneas, sem trava na tabela.
- Uma abertura que falha depois de pegar o número deixa um buraco na sequência. Isso é aceitável: o ADR-0029 pede números únicos e curtos, não contínuos.
- **Alternativa:** `MAX(Number) + 1` numa transação serializável. Recusada: trava a tabela a cada abertura e precisa de novas tentativas.

### 4. Status e descrição

- **Status:** os seis do ADR-0024 já existem no domínio (`TicketStatus`). Nesta change, todo chamado nasce Aberto, e o fluxo vem na Chamados-3.
- **Situações da lista:**
  - "Em aberto": Aberto, Em triagem, Em andamento e Aguardando solicitante;
  - "Encerrados": Resolvido e Fechado;
  - "Todos".
- **Descrição:** texto simples, até 10.000 caracteres, com as quebras de linha. O editor com formatação chega com a conversa (Chamados-2), que converte as descrições existentes.
- **Contagens:** cada visão conta os chamados em aberto. Na aba do projeto, "Todos" conta todos.

### 5. Lista, ordem e página

- **`GET /api/tickets`:**
  - `view`: `All`, `AssignedToMe`, `UnassignedInMyTeams`, `Unassigned` ou `OpenedByMe`. `Unassigned` é a visão "Sem responsável" da aba do projeto.
  - Também `projectId`, `team`, `situation`, `search`, `sort` (`Priority` ou `Number`), `descending`, `page` e `pageSize`.
- **Ordem padrão:**
  - pela prioridade, a mais alta primeiro, e, na mesma prioridade, o mais antigo primeiro;
  - o canvas ordena por "Status e prazo", que depende dos prazos (Chamados-4). Até lá, a prioridade é o que mais se aproxima da urgência.
- **Busca:**
  - um número (com ou sem `#`) acha o chamado pelo número;
  - qualquer texto acha pelo título e pela descrição, sem diferenciar maiúsculas e acentos, como a busca de projetos.
- **Rotas do chamado:**
  - `GET /api/tickets/{number}` e a tela `/tickets/{number}`, porque o número é a referência que as pessoas citam;
  - `GET /api/tickets/choices` traz os projetos para o filtro e para a abertura, com os times que atendem em cada um.

### 6. Telas

- **Página Chamados:**
  - as visões numa coluna ao lado, como no canvas;
  - os filtros de projeto, time e situação e a busca;
  - a tabela pelo `DataTable`, com Nº, Título (com o código, o time e quem abriu), Status, Responsável e Prioridade.
- **Fora desta change:**
  - as caixas de seleção e o "Atribuir" (Chamados-5);
  - o prazo na coluna de status (Chamados-4);
  - a coluna de interações (Chamados-2).
- **Aba Chamados do projeto:** rota filha do `ProjectLayout`, com o `SegmentedControl` das quatro visões, a busca e o time. A contagem da aba é o total de chamados do projeto.
- **Abrir chamado:**
  - em `/tickets/new` e em `/projects/{id}/tickets/new`, com o projeto preenchido;
  - o time e a prioridade em cartões de opção, como no canvas. A prioridade começa em Média, e o time vem escolhido quando só um atende;
  - os anexos ficam para a Chamados-2.
- **Página do chamado:**
  - o cabeçalho do canvas;
  - a descrição como a primeira mensagem do solicitante;
  - o painel "Pessoas", com o projeto e a última atualização. A conversa vem na Chamados-2.
- **Componentes:** `PriorityTag` (ícone, cor e texto) e `TicketStatusBadge`, em `web/src/tickets`, com os ícones do design system.

## Risks / Trade-offs

- **[Lista com todos os projetos do administrador]** O filtro usa a lista de projetos que a pessoa vê. Mitigação: uma instalação tem dezenas de projetos, não milhares. Se crescer, o contrato devolve só os ids.
- **[Sem responsável até a Chamados-3]** Todo chamado fica sem responsável até existir a primeira resposta. Mitigação: as visões e a coluna já tratam o responsável, que passa a aparecer sozinho.
