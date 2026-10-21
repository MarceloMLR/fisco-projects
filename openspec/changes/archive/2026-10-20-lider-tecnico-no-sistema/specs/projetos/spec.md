## MODIFIED Requirements

### Requirement: Gerente e líder técnico na criação

A criação MUST definir o gerente do projeto: quem cria, por padrão, ou outra pessoa ativa com o papel no sistema Gerente de projeto ou Administrador. Com outro gerente, quem cria MUST NOT entrar no projeto. O líder técnico é opcional, MUST ser uma pessoa ativa diferente do gerente, com o papel no sistema Líder técnico ou Administrador, e entra com o papel Líder técnico (ADR-0072, ADR-0097).

#### Scenario: Quem cria é o gerente
- **WHEN** um gerente de projeto cria um projeto sem trocar o gerente
- **THEN** ele entra no projeto como gerente

#### Scenario: Projeto para outro gerente
- **WHEN** o administrador cria um projeto com Marina Costa como gerente
- **THEN** Marina entra como gerente, e o administrador não entra no projeto

#### Scenario: Gerente sem o papel no sistema
- **WHEN** alguém cria um projeto com um gerente que tem o papel Usuário, está inativo ou não existe
- **THEN** a API responde 400 no campo do gerente

#### Scenario: Líder técnico na criação
- **WHEN** o gerente de projeto cria um projeto com Rafael Souza, que tem o papel no sistema Líder técnico, como líder técnico
- **THEN** Rafael entra no projeto como líder técnico e passa a ver o projeto

#### Scenario: Líder técnico do diretório na criação
- **WHEN** o gerente de projeto cria um projeto com um líder técnico do diretório que nunca entrou
- **THEN** a API responde 400 no campo do líder técnico, porque quem nunca entrou tem o papel Usuário, e nem o projeto nem o usuário são criados

#### Scenario: Líder técnico sem o papel no sistema
- **WHEN** o gerente de projeto cria um projeto com um líder técnico que tem o papel Usuário
- **THEN** a API responde 400 no campo do líder técnico e o projeto não é criado

#### Scenario: Líder técnico inválido
- **WHEN** alguém escolhe como líder técnico o próprio gerente, uma pessoa inativa ou alguém que não existe
- **THEN** a API responde 400 no campo do líder técnico
