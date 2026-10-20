# Design: prazos dos chamados (Chamados-4)

## Context

- **ADR-0025:** prazos em dias úteis, com pausa ao aguardar o solicitante e prazo de primeira resposta, configuráveis por prioridade.
- **ADR-0068:** a primeira resposta é de quem atende o time.
- **O que já existe:**
  - o calendário da instalação conta dias úteis (`IBusinessDays.AddBusinessDays` e `CountBusinessDays`);
  - o fluxo (Chamados-3) já passa por Aguardando solicitante;
  - o `DueDate` e o `ProgressBar` já existem no frontend.

## Goals / Non-Goals

**Goals:**
- prazos calculados num lugar só e guardados no chamado;
- lista e página com os prazos;
- prioridade alterável;
- prazos configuráveis na Administração.

**Non-Goals:**
- avisos de prazo (grupo 6);
- filtro "Só prazo vencido";
- prazos em horas ou por time.

## Decisions

### 1. Prazos da instalação na tabela `ticket_deadlines`

- **Tabela:** uma linha por prioridade, com os dias de primeira resposta e de resolução, criadas pela migração com os padrões. A auditoria registra as mudanças, com a prioridade como rótulo.
- **API:** `GET` e `PUT /api/admin/ticket-deadlines`, só do administrador; cada valor vai de 1 a 365 dias úteis.
- **Alternativa descartada:** a linha de configurações do `Administration`. Os prazos são regra dos chamados, e o módulo dono das regras guarda os dados (ADR-0047).

### 2. O chamado guarda os prazos

- **Campos guardados no chamado:**
  - os dias da prioridade, na abertura;
  - as datas de vencimento (`FirstResponseDueOn` e `ResolutionDueOn`);
  - quando a primeira resposta foi cumprida (`FirstRespondedAt`);
  - o início da pausa (`PausedSince`) e os dias úteis já pausados (`PausedBusinessDays`).
- **Por quê:**
  - os valores novos da Administração valem só para chamados novos;
  - a lista ordena e destaca pela data guardada, sem contar dias úteis linha a linha.
- **Cálculo:** fica em `TicketDeadlines`, no Application, com o calendário e o fuso da instalação.
  - Abertura: `vencimento = AddBusinessDays(dia da abertura, dias)`.
  - Retomada: os dias pausados são `CountBusinessDays(dia da pausa + 1, dia da retomada)`, e a resolução vence em `AddBusinessDays(dia da abertura, dias + pausados)`.
  - Durante a pausa, a resolução não tem vencimento (`ResolutionDueOn` nulo).
- **Prioridade:** a troca recalcula a partir da abertura, com os dias atuais da nova prioridade.
- **Primeira resposta:** é cumprida pela primeira mensagem de quem atende o time (`TicketAccess.Serves`), a mesma regra que define o responsável.

### 3. O estado do prazo

O estado é calculado na leitura, contra o dia de hoje da instalação. O frontend recebe o estado e os dias e só escreve o texto.

| Estado | Quando | Texto |
| --- | --- | --- |
| `Met` | resolvido ou fechado; primeira resposta cumprida | "Resolvido" / "Cumprida" |
| `Paused` | Aguardando solicitante | "Prazo pausado" |
| `Overdue` | hoje depois do vencimento | "Venceu há N dias úteis" |
| `Soon` | vence hoje ou no próximo dia útil | "Vence hoje" / "Vence amanhã" |
| `OnTrack` | o resto | "Vence segunda, 26/10" |

- **Dias úteis usados:** para a barra do painel, `CountBusinessDays(dia da abertura + 1, hoje)` menos os dias pausados, entre zero e o total.

### 4. Prioridade (`PUT /api/tickets/{number}/priority`)

- **Quem muda:** quem atende (`TicketActors.Attendant`), num chamado em aberto (antes de Resolvido).
- **Evento:** `PriorityChanged`, com a prioridade de antes e a nova, aparece na conversa.
- **Na tela:** "Mudar prioridade" no "⋮" abre um diálogo com as quatro prioridades.

### 5. Pausa na conversa

O evento de mudança de status que entra ou sai de Aguardando solicitante ganha "· prazo pausado" ou "· prazo retomado", como no canvas. Não há evento novo.

### 6. Tela "Prazos dos chamados"

- **Desenho:** segue "Configurações gerais", porque não há tela desenhada. É um formulário com uma linha por prioridade (a prioridade, a primeira resposta e a resolução, em dias úteis) e "Salvar".
- **Ajuda:** "Os prazos valem para os chamados abertos depois".
- **Navegação:** fica na navegação da Administração, depois de "Calendário e feriados".

## Risks / Trade-offs

- **[Feriado cadastrado depois]** O vencimento guardado não muda com um feriado novo. Mitigação: o prazo de um chamado aberto não muda depois da abertura, como os valores da Administração.
- **[Demonstração sem vencidos]** A carga abre os chamados no dia em que roda. Mitigação: os testes simulam os vencidos.
