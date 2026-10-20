# Proposta: prazos dos chamados (Chamados-4)

## Why

Os chamados andam pelo fluxo, mas ninguém sabe quando vencem. O ADR-0025 já decidiu os prazos:
- **Dois prazos:**
  - de primeira resposta, padrão de 1 dia útil para todas as prioridades;
  - de resolução, padrão de 10, 5, 2 e 1 dias úteis (baixa, média, alta e crítica).
- **Configuração:** os dois prazos são configuráveis por prioridade na Administração, e valem para os chamados novos.
- **Contagem:** em dias úteis, pelo calendário da instalação.
- **Pausa:** o prazo de resolução para enquanto o chamado está Aguardando solicitante e é estendido pelos dias pausados.
- **Prioridade:** quem atende pode mudá-la, com registro na conversa, e a mudança recalcula os prazos a partir da abertura.
- **Na tela:** a barra de prazo na página do chamado, e o destaque dos prazos vencendo e vencidos na lista.

## What Changes

- **Prazos da instalação:**
  - a tabela `ticket_deadlines` nasce pela migração com os valores do ADR-0025;
  - o administrador os vê e muda em "Prazos dos chamados", na Administração (`/api/admin/ticket-deadlines`), de 1 a 365 dias úteis;
  - a mudança entra na auditoria.
- **Prazos do chamado:**
  - na abertura, o chamado guarda os dias da prioridade escolhida e as datas de vencimento da primeira resposta e da resolução, contadas da data de abertura no fuso da instalação;
  - a primeira resposta é cumprida pela primeira mensagem de quem atende o time do chamado (ADR-0068), nunca pela do solicitante.
- **Pausa:**
  - ao entrar em Aguardando solicitante, o prazo de resolução para;
  - ao sair, o prazo é estendido pelos dias úteis pausados;
  - a conversa diz "prazo pausado" e "prazo retomado" na mudança de status.
- **Mudar a prioridade** (`PUT /api/tickets/{number}/priority`):
  - quem atende muda a prioridade de um chamado em aberto;
  - os prazos são recalculados a partir da abertura, com os dias da nova prioridade;
  - a conversa registra a mudança.
- **Na tela:**
  - o cabeçalho do chamado mostra o prazo de resolução ("Vence segunda, 26/10", "Venceu há 2 dias úteis", "Prazo pausado");
  - o painel ganha a seção Prazos: a barra dos dias úteis usados, a resolução, a primeira resposta e o tempo pausado;
  - "Mudar prioridade" entra no "⋮";
  - a lista mostra o status com o prazo embaixo ("Status e prazo");
  - a Administração ganha "Prazos dos chamados", desenhada como "Configurações gerais".
- **Demonstração:** os chamados da carga mostram prazos em dia, vencendo e pausados. Nenhum nasce vencido, porque a carga abre os chamados no dia em que roda.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `chamados`: prazos configuráveis, prazos do chamado, pausa, prioridade e prazos na tela.
- `dados-de-demonstracao`: prazos na demonstração.

## Impact

- **Backend:** módulo `Tickets`, com a tabela `ticket_deadlines`, as colunas de prazo do chamado e a migração `TicketDeadlines`.
- **Frontend:** página do chamado, lista e a nova página da Administração.
- **Fora do escopo:**
  - o filtro "Só prazo vencido" e os avisos de prazo, que ficam para os avisos (grupo 6);
  - prazos em horas e por time, que ficam para depois (ADR-0025).
