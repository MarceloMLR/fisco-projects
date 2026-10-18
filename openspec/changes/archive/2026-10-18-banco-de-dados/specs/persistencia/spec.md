# Spec Delta

## Purpose

Define como os módulos do FiscoProjects guardam dados no banco da instalação: cada módulo no próprio schema, com migrações próprias aplicadas de forma controlada, datas em UTC e identificadores gerados pela aplicação (ADR-0047, ADR-0048, ADR-0013, ADR-0061).

## ADDED Requirements

### Requirement: Cada módulo no próprio schema

Todos os módulos MUST usar o mesmo banco da instalação, cada um com as próprias tabelas e o próprio histórico de migrações dentro de um schema com o nome do módulo em minúsculas.

#### Scenario: Módulo com schema próprio
- **WHEN** as migrações de um módulo são aplicadas
- **THEN** as tabelas do módulo e o histórico de migrações dele ficam no schema do módulo

#### Scenario: Dois módulos no mesmo banco
- **WHEN** as migrações de dois módulos são aplicadas no mesmo banco
- **THEN** cada módulo tem as tabelas e o histórico de migrações no próprio schema, sem misturar com o outro

### Requirement: Migrações de todos os módulos

A aplicação MUST conseguir aplicar, de uma vez, as migrações pendentes de todos os módulos registrados, e aplicar de novo MUST NOT alterar nada nem falhar.

#### Scenario: Banco vazio
- **WHEN** as migrações de todos os módulos são aplicadas num banco vazio
- **THEN** o banco é criado e cada módulo fica com o schema e as tabelas da última migração

#### Scenario: Migrações já aplicadas
- **WHEN** as migrações de todos os módulos são aplicadas num banco já atualizado
- **THEN** nenhuma migração é aplicada de novo e nada falha

### Requirement: Migração automática só no desenvolvimento

Ao subir no ambiente de desenvolvimento, a API MUST criar o banco, se ainda não existir, e aplicar as migrações pendentes de todos os módulos. Fora do desenvolvimento, a API MUST NOT criar nem alterar o banco ao subir; o banco vem da infraestrutura e as migrações, da publicação.

#### Scenario: API sobe no desenvolvimento
- **WHEN** a API sobe no ambiente de desenvolvimento com o banco acessível e migrações pendentes
- **THEN** as migrações são aplicadas antes de a API atender requisições

#### Scenario: Banco ainda não criado no desenvolvimento
- **WHEN** a API sobe no ambiente de desenvolvimento e o banco configurado ainda não existe
- **THEN** o banco é criado antes de a API atender requisições

#### Scenario: API sobe em produção
- **WHEN** a API sobe em produção com migrações pendentes
- **THEN** nenhuma migração é aplicada

### Requirement: Datas e horas em UTC

Datas com hora MUST ser gravadas em UTC e lidas de volta marcadas como UTC, representando o mesmo instante (ADR-0013).

#### Scenario: Data e hora em UTC
- **WHEN** uma entidade com uma data e hora em UTC é gravada e lida de novo
- **THEN** a data lida está marcada como UTC e representa o mesmo instante

#### Scenario: Data e hora local
- **WHEN** uma entidade com uma data e hora local é gravada e lida de novo
- **THEN** a data lida está marcada como UTC e representa o mesmo instante da data local

### Requirement: Identificador gerado pela aplicação

O identificador de uma entidade MUST ser o GUID versão 7 gerado pela aplicação na criação; o banco MUST NOT gerar nem trocar o identificador.

#### Scenario: Entidade gravada
- **WHEN** uma entidade nova é gravada e lida de novo
- **THEN** o identificador lido é o mesmo que a entidade recebeu ao ser criada
