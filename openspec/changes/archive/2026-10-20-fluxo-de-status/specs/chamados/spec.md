## ADDED Requirements

### Requirement: Fluxo de status em tabela

Os status e as transições MUST vir de uma tabela, com o fluxo do MVP: a ação, o status de origem e o de destino, quem pode e se exige mensagem. O servidor MUST conferir a integridade do fluxo lido: todo status, menos Fechado, tem saída, e nenhuma ação se repete no mesmo status. Não existe caminho para reabrir um chamado Fechado (ADR-0024).

#### Scenario: Fluxo padrão
- **WHEN** alguém pede o fluxo de chamados
- **THEN** a API devolve as transições do MVP, de Aberto a Fechado, com quem pode e se exige mensagem, e nenhuma sai de Fechado

#### Scenario: Fluxo quebrado
- **WHEN** o fluxo lido tem um status sem saída, além de Fechado
- **THEN** o fluxo é recusado como inválido

### Requirement: Mudar o status

Cada ação do fluxo MUST ser feita só por quem pode: "quem atende" é o administrador, os gestores e quem atende o time; "solicitante" é quem abriu o chamado; "gestor" é o gerente e o líder técnico. A ação que exige mensagem MUST receber o texto, gravado como mensagem. Ação de quem não pode MUST receber 403, e ação que não sai do status atual, 409 (ADR-0024, ADR-0044).

#### Scenario: Resolver o chamado
- **WHEN** quem atende marca como resolvido um chamado Em andamento
- **THEN** o chamado fica Resolvido, e a conversa registra a mudança

#### Scenario: Pedir informações sem mensagem
- **WHEN** quem atende pede informações sem escrever a mensagem
- **THEN** a API responde 400 e o status não muda

#### Scenario: Ação de quem não pode
- **WHEN** o solicitante tenta marcar o próprio chamado como resolvido
- **THEN** a API responde 403 e o status não muda

#### Scenario: Ação fora do status
- **WHEN** alguém tenta confirmar e fechar um chamado Aberto
- **THEN** a API responde 409 e o status não muda

#### Scenario: Cancelar o chamado
- **WHEN** o solicitante cancela, com uma mensagem, o próprio chamado Aberto
- **THEN** o chamado fica Fechado, e ninguém manda mais mensagens nele

### Requirement: Primeira resposta

A primeira mensagem de alguém que atende o time num chamado sem responsável MUST torná-lo responsável e fazê-lo acompanhar o chamado; num chamado Aberto, o status MUST ir a Em andamento. Respostas do solicitante, do gerente e de quem só acompanha MUST não definir responsável, e uma resposta num chamado com responsável nunca o troca. A resposta do solicitante num chamado Aguardando solicitante MUST levá-lo a Em andamento (ADR-0024, ADR-0068).

#### Scenario: Primeira resposta define o responsável
- **WHEN** um desenvolvedor responde a um chamado Aberto de Desenvolvimento sem responsável
- **THEN** ele passa a ser o responsável e a acompanhar o chamado, e o chamado fica Em andamento

#### Scenario: Resposta que não define responsável
- **WHEN** o gerente e depois o solicitante respondem a um chamado sem responsável
- **THEN** o chamado continua sem responsável e Aberto

#### Scenario: Responsável não muda
- **WHEN** o líder técnico responde a um chamado que já tem um desenvolvedor como responsável
- **THEN** o responsável continua o mesmo

#### Scenario: Resposta do solicitante retoma o chamado
- **WHEN** o solicitante responde a um chamado Aguardando solicitante
- **THEN** o chamado volta a Em andamento

### Requirement: Fechamento automático

Um chamado Resolvido sem retorno do solicitante MUST fechar sozinho depois de 5 dias úteis do calendário da instalação, valor configurável em `Tickets:AutoCloseBusinessDays`, com o sistema como autor da mudança (ADR-0024).

#### Scenario: Fechar resolvidos parados
- **WHEN** passam 5 dias úteis desde que um chamado foi resolvido, sem resposta
- **THEN** o chamado fica Fechado, e a conversa registra o fechamento automático

### Requirement: Histórico na conversa

As mudanças de status e a definição do responsável MUST aparecer na conversa como eventos, com quem fez e quando, numa linha discreta. A conversa MUST ter o filtro "Tudo", "Mensagens" e "Histórico" (ADR-0069).

#### Scenario: Filtrar o histórico
- **WHEN** alguém escolhe "Histórico" num chamado com mensagens e mudanças de status
- **THEN** a conversa mostra só os eventos, e "Mensagens" mostra só as mensagens

### Requirement: Status pela tela

O cabeçalho do chamado MUST mostrar a ação principal que a pessoa pode fazer e as outras no "⋮", só as do status atual. A ação que exige mensagem MUST abrir um diálogo para o texto. A página "Fluxo de chamados" MUST mostrar as transições lidas da tabela (ADR-0024).

#### Scenario: Resolver pela tela
- **WHEN** quem atende escolhe "Marcar como resolvido" no cabeçalho
- **THEN** o chamado mostra Resolvido, e a conversa, o evento

#### Scenario: Pedir informações pela tela
- **WHEN** quem atende escolhe "Pedir informações" no "⋮", digita a mensagem e confirma
- **THEN** o chamado mostra Aguardando solicitante, com a mensagem na conversa

#### Scenario: Página do fluxo
- **WHEN** alguém abre "Fluxo de chamados" na página Chamados
- **THEN** a página mostra cada transição com a ação, quem pode e se exige mensagem

## MODIFIED Requirements

### Requirement: Mensagens do chamado

O administrador, o gerente, o líder técnico, quem atende o time do chamado e o solicitante MUST poder mandar mensagens com negrito, listas e links, visíveis a todos que veem o chamado. Os outros membros MUST receber 403, e ninguém manda mensagem num chamado Fechado (409). Mensagens não se editam nem se apagam, e cada uma atualiza o chamado (ADR-0024, ADR-0026, ADR-0044).

#### Scenario: Mandar mensagem
- **WHEN** o solicitante manda uma mensagem com um trecho em negrito
- **THEN** a mensagem aparece na conversa para todos que veem o chamado, com o negrito, e o chamado fica atualizado

#### Scenario: Leitor não manda mensagem
- **WHEN** um leitor do projeto tenta mandar uma mensagem
- **THEN** a API responde 403 e nada é gravado

#### Scenario: Texto formatado inválido
- **WHEN** alguém manda uma mensagem vazia ou com um elemento fora do negrito, das listas, dos links e das menções
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Chamado fechado sem mensagens
- **WHEN** alguém tenta mandar uma mensagem num chamado Fechado
- **THEN** a API responde 409 e nada é gravado
