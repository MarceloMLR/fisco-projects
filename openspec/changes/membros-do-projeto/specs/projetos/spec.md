# Spec Delta

## MODIFIED Requirements

### Requirement: Gerente e líder técnico na criação

A criação MUST definir o gerente do projeto: quem cria, por padrão, ou outra pessoa ativa com o papel no sistema Gerente de projeto ou Administrador. Com outro gerente, quem cria MUST NOT entrar no projeto. O líder técnico é opcional, MUST ser uma pessoa ativa diferente do gerente e entra com o papel Líder técnico; pode ser um usuário do sistema ou uma pessoa do diretório que ainda não entrou, que vira usuário com o papel Usuário (ADR-0005, ADR-0072).

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
- **WHEN** o gerente de projeto cria um projeto com Rafael Souza como líder técnico
- **THEN** Rafael entra no projeto como líder técnico e passa a ver o projeto

#### Scenario: Líder técnico do diretório na criação
- **WHEN** o gerente de projeto cria um projeto com um líder técnico do diretório que nunca entrou
- **THEN** o sistema cria o usuário dessa pessoa, com o papel Usuário, e ela entra no projeto como líder técnico

#### Scenario: Líder técnico inválido
- **WHEN** alguém escolhe como líder técnico o próprio gerente, uma pessoa inativa ou alguém que não está no diretório
- **THEN** a API responde 400 no campo do líder técnico

### Requirement: Cabeçalho do projeto

A página do projeto MUST ter o cabeçalho do canvas ProjetoCabecalho: o logo do cliente ou as iniciais, o nome, o status, o código, o cliente, o gerente, o líder técnico e "Sem cronograma", e as abas Visão geral e Membros, esta com o número de pessoas. A aba Visão geral MUST mostrar a descrição e a equipe por time: Gestão, Funcional e Desenvolvimento (ADR-0016, ADR-0072).

#### Scenario: Cabeçalho do projeto
- **WHEN** uma pessoa do projeto abre a página do projeto
- **THEN** o cabeçalho mostra o nome, o status, o código, o cliente, o gerente e o líder técnico, e a trilha mostra Projetos e o código

#### Scenario: Abas do projeto
- **WHEN** uma pessoa do projeto escolhe a aba Membros no cabeçalho
- **THEN** a aba Membros fica marcada, mostra o número de pessoas e abre as pessoas do projeto, e Visão geral volta à descrição e à equipe
