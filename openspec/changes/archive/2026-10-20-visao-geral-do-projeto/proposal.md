# Proposta: visão geral do projeto (Síntese-2)

## Why

A aba Visão geral do projeto mostra só a descrição e a equipe; para saber se o projeto está no prazo, o gerente abre o cronograma, os chamados e os termos. O ADR-0073 decidiu que a Visão geral responde "está no prazo?":

- **Quatro números:** o andamento real contra o previsto, o prazo final com o desvio contra a linha de base, os chamados abertos com os vencidos e os que aguardam o solicitante, e o próximo marco com os dias úteis que faltam;
- **Fases e marcos** numa linha do tempo menor, que se ajusta ao espaço (ADR-0071);
- **Precisa de atenção:** tarefas atrasadas, chamados perto do prazo ou sem responsável, aceites pendentes e chamados em fornecedores sem retorno;
- **Equipe** por time, com as pessoas e os chamados abertos;
- **Sem cronograma:** "Sem cronograma" nos números de prazo.

## What Changes

- **API:** `GET /api/projects/{id}/overview` no `Projects`, para quem vê o projeto, com:
  - o andamento previsto, calculado no domínio em dias úteis, pela linha de base quando houver (`SchedulePlan`);
  - o prazo final, o fim da linha de base e o desvio;
  - os chamados do projeto por um contrato novo do `Projects.Contracts` (`IProjectTickets`), que o `Tickets` implementa: os abertos, os vencidos, os que aguardam o solicitante, os de cada time, os que precisam de atenção e os chamados em fornecedores parados;
  - os pedidos de aceite pendentes que a pessoa vê, por um método novo do `IProjectAcceptances`, que o `Documents` implementa;
  - o próximo marco, as fases e os marcos da linha do tempo com os dias não úteis, a atenção e a equipe.
- **Tickets:** a regra do prazo que conta agora (primeira resposta, depois a resolução) passa a ficar num lugar só (`TicketDueNow`), usada pela fila do Meu trabalho e pela visão geral.
- **Na tela** (canvas ProjetoVisaoGeral): a aba Visão geral com os quatro números, a linha do tempo menor das fases e dos marcos (desenhada sem o Syncfusion, como a impressão), "Precisa de atenção" e a equipe; no celular, um embaixo do outro. A descrição sai da Visão geral e continua em "Editar projeto".

## Capabilities

### New Capabilities

- `visao-geral-do-projeto`: os números, a linha do tempo menor, a atenção, a equipe, a API e a tela.

### Modified Capabilities

- `projetos`: a aba Visão geral do cabeçalho passa a mostrar a visão geral, no lugar da descrição e da equipe.

## Impact

- **Backend:** o `Projects` (a rota, o caso de uso e o cálculo do previsto), os contratos `IProjectTickets` e `IProjectAcceptances` do `Projects.Contracts`, o `Tickets` e o `Documents`.
- **Frontend:** a aba Visão geral (`ProjectOverview`) e a linha do tempo menor.
- **Documentação:** CLAUDE.md, glossário e roadmap.

## Non-goals

- Gráficos de evolução e indicadores gerenciais (depois, ADR-0073).
- Editar o cronograma pela visão geral: a linha do tempo menor só mostra e leva ao cronograma.
