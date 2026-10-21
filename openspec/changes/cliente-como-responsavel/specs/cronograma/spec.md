## ADDED Requirements

### Requirement: Cliente como responsável

Uma tarefa de um projeto com cliente MUST poder ter o cliente do projeto como responsável, no lugar de uma pessoa; nunca os dois, e nunca um marco. Sem cliente no projeto, o cliente como responsável MUST receber 400. O progresso de uma tarefa do cliente MUST ser atualizado só por quem edita o cronograma. A exportação para o MS Project MUST trazer o cliente como recurso (ADR-0102).

#### Scenario: Cliente como responsável
- **WHEN** o gerente escolhe o cliente como responsável dos testes do cliente
- **THEN** a tarefa fica com o cliente e sem pessoa, só o gerente atualiza o progresso, e escolher uma pessoa depois tira o cliente

#### Scenario: Cliente como responsável sem cliente
- **WHEN** o gerente escolhe o cliente como responsável num projeto sem cliente, junto de uma pessoa ou num marco
- **THEN** a API responde 400 no responsável e nada muda

#### Scenario: Desfazer o cliente como responsável
- **WHEN** o gerente desfaz a escolha do cliente como responsável
- **THEN** a tarefa volta a não ter o cliente

#### Scenario: Cliente como responsável pela API
- **WHEN** a tarefa tem o cliente como responsável
- **THEN** o cronograma traz o cliente na tarefa, a auditoria registra a troca e a exportação para o MS Project traz o cliente como recurso

#### Scenario: Cliente como responsável pela tela
- **WHEN** o gerente escolhe "{cliente} (cliente)" no responsável de uma tarefa e salva
- **THEN** a Lista mostra o cliente como responsável da tarefa

#### Scenario: Cliente como responsável sem cliente pela tela
- **WHEN** o projeto não tem cliente
- **THEN** o responsável não oferece o cliente
