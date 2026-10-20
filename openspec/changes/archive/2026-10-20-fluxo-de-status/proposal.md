# Proposta: fluxo de status (Chamados-3)

## Why

Os chamados nascem Abertos e não saem disso: ninguém assume, resolve ou fecha. Os ADRs já decidiram o fluxo:
- **ADR-0024:**
  - os seis status e as transições do MVP ficam em tabela, com a ação, quem pode e se exige mensagem;
  - a primeira resposta de quem atende num chamado Aberto leva a Em andamento;
  - o chamado Resolvido fecha sozinho depois de 5 dias úteis sem retorno;
  - uma página mostra o fluxo a partir das regras cadastradas.
- **ADR-0068:** a primeira resposta de alguém do time define o responsável, só quando o chamado ainda não tem um. As respostas do solicitante, de quem só acompanha e do gerente não definem.
- **ADR-0044:** diz quem muda o status como quem atende, quem confirma, fecha e reabre, e quem cancela.

## What Changes

- **Fluxo em tabela:**
  - a tabela `ticket_transitions` nasce com o fluxo do MVP, pela migração;
  - cada linha tem o status de origem, o de destino, a ação, quem pode e se exige mensagem;
  - ao ler o fluxo, o servidor confere a integridade: todo status, menos Fechado, tem saída, e não há ação repetida no mesmo status;
  - um fluxo quebrado impede as mudanças e aparece no registro da aplicação.
- **Mudar o status** (`POST /api/tickets/{number}/transitions`):
  - a ação vem do fluxo, e quem pode sai da relação da pessoa com o chamado (ADR-0044):
    - quem atende: o administrador, os gestores e quem atende o time;
    - o solicitante;
    - os gestores e o administrador;
  - a ação que exige mensagem recebe o texto, gravado como mensagem;
  - a ação fora do fluxo ou de quem não pode recebe 409 ou 403.
- **Primeira resposta:**
  - a mensagem de quem atende o time num chamado sem responsável torna essa pessoa responsável, e ela passa a acompanhar o chamado;
  - se o chamado estava Aberto, ele vai a Em andamento;
  - a resposta do solicitante num chamado Aguardando solicitante o leva de volta a Em andamento.
- **Fechamento automático:** um processo em segundo plano fecha os chamados Resolvidos há 5 dias úteis, valor configurável em `Tickets:AutoCloseBusinessDays`, contados pelo calendário da instalação.
- **Mensagens:** ninguém manda mensagem num chamado Fechado.
- **Eventos na conversa:**
  - as mudanças de status e a definição do responsável aparecem na conversa como linhas discretas, com quem fez e quando;
  - a faixa "Conversa" ganha o filtro "Tudo / Mensagens / Histórico" (ADR-0069).
- **Na tela:**
  - o cabeçalho do chamado mostra a ação principal (como "Marcar como resolvido") e as outras no "⋮", só as que a pessoa pode fazer;
  - a ação que exige mensagem abre um diálogo para o texto;
  - a página "Fluxo de chamados" (`/tickets/flow`) mostra a tabela das transições, lida das regras, aberta pela página Chamados.
- **Dados de demonstração:** chamados em vários status, com responsável.

## Non-goals

- O editor de fluxo na Administração e os fluxos por time (ADR-0024: "depois").
- Os prazos e a pausa em Aguardando solicitante (Chamados-4).
- A troca do responsável pelos gestores e a transferência entre times (Chamados-5).
- Reabrir um chamado Fechado (ADR-0024: abre-se um novo).
- O diagrama gráfico do fluxo. A página mostra os status em ordem e a tabela, porque a tela não foi desenhada no canvas.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `chamados`: o fluxo, a primeira resposta, o fechamento automático, os eventos na conversa e a página do fluxo; as mensagens ficam fechadas no chamado Fechado.
- `dados-de-demonstracao`: chamados em vários status.

## Impact

- **Backend (`Tickets`):**
  - as entidades `TicketTransition` e `TicketEvent`, com a migração e o fluxo inicial;
  - `TicketFlow` no domínio;
  - os casos de uso de mudar o status, de ler o fluxo e de fechar os resolvidos, este com o serviço em segundo plano;
  - a primeira resposta em `SendTicketMessage`;
  - a referência ao `Calendar.Contracts` e ao `Administration.Contracts`.
- **Frontend:**
  - as ações no cabeçalho do chamado;
  - os eventos e o filtro na conversa;
  - a página do fluxo.
- **Docs:** `CLAUDE.md`, glossário e roadmap.
