## ADDED Requirements

### Requirement: Aviso de menção num comentário

Quem é mencionado num comentário de uma tarefa ou marco MUST receber na hora o aviso, com quem escreveu, o item e o projeto e o link que abre o item no cronograma: o e-mail e o Teams conforme a escolha da pessoa, e o sino sempre. Quem escreveu MUST NOT receber (ADR-0037, ADR-0151, ADR-0157).

#### Scenario: Menção no comentário
- **WHEN** a colaboradora menciona o desenvolvedor num comentário
- **THEN** o desenvolvedor recebe o e-mail da menção, com o link do item, e a notificação no sino

#### Scenario: Menção no Teams
- **WHEN** a colaboradora menciona uma pessoa que tem o Teams ligado
- **THEN** a pessoa recebe na aba Atividade o aviso da menção

#### Scenario: Quem se menciona não recebe
- **WHEN** a colaboradora menciona a si mesma num comentário
- **THEN** ninguém recebe o aviso da menção

### Requirement: Aviso de comentário ao responsável

O responsável de uma tarefa MUST receber na hora o aviso de cada comentário novo nela, pelos mesmos canais, salvo quando ele mesmo escreve ou é mencionado no comentário, quando recebe só o aviso da menção (ADR-0157).

#### Scenario: Comentário na tarefa do responsável
- **WHEN** a gerente comenta numa tarefa da Carla
- **THEN** a Carla recebe o aviso do comentário

#### Scenario: Responsável mencionado
- **WHEN** a gerente comenta numa tarefa da Carla mencionando a Carla
- **THEN** a Carla recebe só o aviso da menção

#### Scenario: Responsável comenta
- **WHEN** a Carla comenta na própria tarefa
- **THEN** ninguém recebe o aviso do comentário
