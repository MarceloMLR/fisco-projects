## ADDED Requirements

### Requirement: Prazos da instalação

Os prazos de primeira resposta e de resolução de cada prioridade MUST vir de uma tabela, com os padrões do ADR-0025 (primeira resposta de 1 dia útil; resolução de 10, 5, 2 e 1 dias úteis). Só o administrador MUST ver e mudar os prazos, de 1 a 365 dias úteis, com a mudança na auditoria; os valores novos MUST valer para os chamados abertos depois (ADR-0025, ADR-0046).

#### Scenario: Prazos padrão
- **WHEN** o administrador abre "Prazos dos chamados" numa instalação nova
- **THEN** a primeira resposta é de 1 dia útil em todas as prioridades, e a resolução é de 10, 5, 2 e 1 dias úteis

#### Scenario: Mudar os prazos
- **WHEN** o administrador muda a resolução da prioridade média para 3 dias úteis
- **THEN** um chamado de prioridade média aberto depois vence em 3 dias úteis, e o aberto antes continua com 5

#### Scenario: Prazo fora do limite
- **WHEN** o administrador grava um prazo de 0 dias úteis
- **THEN** a API responde 400 e nada muda

#### Scenario: Prazos só do administrador
- **WHEN** um gerente de projeto pede os prazos da Administração
- **THEN** a API responde 403

### Requirement: Prazos do chamado

Cada chamado MUST vencer em dias úteis do calendário da instalação, contados da data de abertura no fuso da instalação, pulando fins de semana e feriados: a primeira resposta e a resolução, com os dias da prioridade. A primeira resposta MUST ser cumprida pela primeira mensagem de quem atende o time do chamado, nunca pela do solicitante (ADR-0025, ADR-0068).

#### Scenario: Prazo em dias úteis
- **WHEN** um chamado de prioridade alta é aberto numa sexta-feira
- **THEN** a resolução vence na terça-feira seguinte, e a primeira resposta, na segunda

#### Scenario: Primeira resposta cumprida
- **WHEN** o solicitante e depois um desenvolvedor respondem a um chamado de Desenvolvimento
- **THEN** a primeira resposta fica cumprida na hora da mensagem do desenvolvedor

### Requirement: Prazo pausado

O prazo de resolução MUST parar enquanto o chamado está Aguardando solicitante e, quando o chamado sai desse status, MUST ser estendido pelos dias úteis pausados (ADR-0025).

#### Scenario: Pausa estende o prazo
- **WHEN** um chamado fica Aguardando solicitante de segunda a quarta-feira
- **THEN** enquanto aguarda, o prazo aparece pausado, e depois da resposta a resolução vence dois dias úteis mais tarde

### Requirement: Mudar a prioridade

Quem atende MUST poder mudar a prioridade de um chamado em aberto; os prazos MUST ser recalculados a partir da abertura com os dias da nova prioridade, e a conversa MUST registrar a mudança. Os outros MUST receber 403 (ADR-0025, ADR-0044).

#### Scenario: Subir a prioridade
- **WHEN** o gerente muda a prioridade de um chamado de média para alta
- **THEN** a resolução vence em 2 dias úteis contados da abertura, e a conversa mostra a mudança

#### Scenario: Solicitante não muda a prioridade
- **WHEN** o solicitante tenta mudar a prioridade do próprio chamado
- **THEN** a API responde 403 e nada muda

### Requirement: Prazos pela tela

O cabeçalho do chamado MUST mostrar o prazo de resolução com ícone, cor e texto (em dia, vencendo hoje, vencido ou pausado); o painel MUST mostrar a seção Prazos, com os dias úteis usados, a resolução, a primeira resposta e o tempo pausado; a lista MUST mostrar o prazo embaixo do status; "Mudar prioridade" MUST ficar no "⋮" de quem atende; e a Administração MUST ter "Prazos dos chamados" (ADR-0025, ADR-0065).

#### Scenario: Prazo vencido na lista
- **WHEN** a lista mostra um chamado cuja resolução venceu há dois dias úteis
- **THEN** a linha mostra "Venceu há 2 dias úteis" em vermelho, com o ícone de alerta, embaixo do status

#### Scenario: Prazos no chamado
- **WHEN** alguém abre um chamado Aguardando solicitante
- **THEN** o cabeçalho mostra "Prazo pausado", e o painel mostra a resolução, a primeira resposta e o tempo pausado

#### Scenario: Mudar a prioridade pela tela
- **WHEN** quem atende escolhe "Mudar prioridade" no "⋮", escolhe Alta e confirma
- **THEN** o chamado mostra a prioridade Alta e o novo prazo

#### Scenario: Prazos na Administração
- **WHEN** o administrador muda um prazo em "Prazos dos chamados" e salva
- **THEN** a tela confirma que os prazos foram salvos e valem para os chamados novos
