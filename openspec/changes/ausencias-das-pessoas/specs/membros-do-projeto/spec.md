## ADDED Requirements

### Requirement: Ausências das pessoas

Uma pessoa MAY ter ausências: o início e o fim de um período sem trabalho, de 1 a 366 dias corridos, sem se sobrepor a outra ausência dela e sem motivo. A ausência MUST NOT ser editada: apaga-se e cadastra-se de novo (ADR-0058, ADR-0141).

#### Scenario: Incluir ausência
- **WHEN** a gerente inclui uma ausência da Carla de 03/11 a 14/11 pelo projeto dela
- **THEN** a ausência aparece nas ausências da Carla, em todos os projetos de que ela participa

#### Scenario: Ausência inválida
- **WHEN** a gerente inclui uma ausência com o fim antes do início ou de mais de 366 dias
- **THEN** a API recusa com 400 no campo das datas, e nada é incluído

#### Scenario: Ausência sobreposta
- **WHEN** a gerente inclui uma ausência que se cruza com outra da mesma pessoa
- **THEN** a API recusa com 409, e nada é incluído

#### Scenario: Apagar ausência
- **WHEN** a gerente apaga uma ausência da Carla
- **THEN** a ausência sai das ausências da Carla

### Requirement: Quem cadastra as ausências

Incluir e apagar as ausências de uma pessoa de um projeto em aberto MUST ser permitido ao administrador e ao gerente do projeto e negado, com 403, ao líder técnico e às outras pessoas do projeto; quem não está no projeto MUST receber 404, e a pessoa que não está no projeto também (ADR-0043, ADR-0141).

#### Scenario: Permissões nas ausências por papel
- **WHEN** cada papel do projeto tenta ver, incluir e apagar uma ausência de uma pessoa do projeto
- **THEN** o administrador e o gerente conseguem tudo; o líder técnico, o colaborador, o desenvolvedor e o leitor veem e recebem 403 ao incluir e ao apagar; quem está fora do projeto recebe 404

### Requirement: Ausências na lista de Membros

A lista de Membros MUST trazer, de cada pessoa, a ausência atual ou a próxima, e a tela MUST mostrar "Ausente até" com o fim, quando a pessoa está ausente hoje, ou "Ausente de" com o período, quando a ausência começa nos próximos 30 dias (ADR-0141).

#### Scenario: Ausente hoje na lista
- **WHEN** alguém do projeto abre Membros com a Carla ausente até 14/11 e o Diego com férias a partir da semana que vem
- **THEN** a linha da Carla diz "Ausente até 14/11" e a do Diego diz o período das férias dele

### Requirement: Ausências no detalhe da pessoa pela tela

O detalhe da pessoa em Membros MUST ter a seção "Ausências", com cada ausência guardada, e, para quem cadastra, "Incluir ausência", que abre o diálogo com o início e o fim digitados, e o apagar de cada uma, com a confirmação (ADR-0141).

#### Scenario: Incluir ausência pela tela
- **WHEN** a gerente abre o detalhe da Carla, escolhe "Incluir ausência", digita 03/11/2026 e 14/11/2026 e confirma
- **THEN** a ausência aparece na seção, e a tela confirma

#### Scenario: Ausência sobreposta pela tela
- **WHEN** a gerente inclui uma ausência que se cruza com outra
- **THEN** o diálogo continua aberto e diz que a pessoa já tem ausência nesse período

#### Scenario: Ausências de quem só vê
- **WHEN** o líder técnico abre o detalhe da Carla
- **THEN** a seção mostra as ausências, sem "Incluir ausência" nem o apagar

### Requirement: Ausências na auditoria

A inclusão e a exclusão de uma ausência MUST ir para a auditoria, com a pessoa e quem fez pelos nomes atuais, sem as datas (ADR-0039, ADR-0141).

#### Scenario: Ausência na auditoria
- **WHEN** a gerente inclui uma ausência da Carla
- **THEN** a auditoria tem a inclusão, pela gerente, com o nome da Carla e as datas só como alteradas
