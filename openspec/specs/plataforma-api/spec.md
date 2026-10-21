# plataforma-api Specification

## Purpose

Define o comportamento comum da API HTTP do FiscoProjects, que vale para todos os módulos: como saber se a aplicação está no ar, em que formato os erros chegam, onde fica o contrato da API e sob qual caminho ficam as rotas (ADR-0057, ADR-0061).

## Requirements

### Requirement: Saúde da aplicação

A aplicação MUST responder em `GET /health`, sem exigir autenticação, se está no ar e se consegue acessar o banco da instalação. A resposta MUST NOT trazer detalhes do erro nem da conexão. A verificação do Microsoft Graph entra nesse mesmo endereço quando a integração existir.

#### Scenario: Aplicação no ar
- **WHEN** alguém faz `GET /health` com a aplicação funcionando e o banco acessível
- **THEN** a resposta tem status 200 e informa que a aplicação está saudável

#### Scenario: Banco fora do ar
- **WHEN** alguém faz `GET /health` e o banco não responde
- **THEN** a resposta tem status 503 e informa que a aplicação não está saudável, sem detalhes da conexão

#### Scenario: Banco não configurado
- **WHEN** alguém faz `GET /health` e a connection string do banco não está configurada
- **THEN** a resposta tem status 503 e informa que a aplicação não está saudável

### Requirement: Erros no formato Problem Details

Toda resposta de erro da API (status 400 a 599) sem corpo próprio MUST ser enviada no formato Problem Details (RFC 9457), com o tipo de conteúdo `application/problem+json`, o status e um título.

#### Scenario: Rota inexistente
- **WHEN** alguém faz uma requisição a um caminho sob `/api` que não existe
- **THEN** a resposta tem status 404, tipo de conteúdo `application/problem+json` e corpo com `status` 404 e um `title`

### Requirement: Erro inesperado sem detalhes internos

Quando ocorre um erro não tratado, a API MUST responder com status 500 em Problem Details, com um identificador de rastreamento para localizar o erro nos registros, e MUST NOT expor a mensagem da exceção, o tipo da exceção ou a pilha de chamadas.

#### Scenario: Erro inesperado numa rota
- **WHEN** uma rota lança uma exceção não tratada
- **THEN** a resposta tem status 500, tipo de conteúdo `application/problem+json`, corpo com `status` 500 e um `traceId`
- **AND** o corpo não contém a mensagem, o tipo nem a pilha de chamadas da exceção

### Requirement: Contrato OpenAPI no desenvolvimento

O contrato da API MUST ser gerado a partir do próprio código, no formato OpenAPI 3.1, e publicado em `GET /openapi/v1.json` apenas no ambiente de desenvolvimento. Fora dele, o endereço MUST NOT existir.

#### Scenario: Contrato no ambiente de desenvolvimento
- **WHEN** alguém faz `GET /openapi/v1.json` com a aplicação no ambiente de desenvolvimento
- **THEN** a resposta tem status 200 e um documento OpenAPI na versão 3.1

#### Scenario: Contrato fora do desenvolvimento
- **WHEN** alguém faz `GET /openapi/v1.json` com a aplicação em produção
- **THEN** a resposta tem status 404 em Problem Details

### Requirement: Rotas dos módulos sob /api

As rotas que os módulos expõem MUST ficar sob o caminho `/api`. O endereço de saúde fica fora desse caminho.

#### Scenario: Rota de um módulo
- **WHEN** um módulo registra uma rota `/example` no grupo da API
- **THEN** a rota responde em `/api/example` e não responde em `/example`

### Requirement: Aquecimento depois de subir

Depois de subir, a API MUST rodar uma vez, em segundo plano, as leituras principais das telas de cada módulo, com uma amostra dos próprios dados, para a primeira visita não pagar a primeira execução das consultas. O aquecimento MUST NOT alterar dados nem atrasar as respostas, e uma falha MUST ir só para o log, sem impedir os outros módulos. A configuração `WarmUp:Enabled` MUST poder desligá-lo (ADR-0106).

#### Scenario: Leituras principais aquecidas
- **WHEN** a instalação tem um projeto com cronograma, um chamado, um acesso e notificações, e o aquecimento roda
- **THEN** cada módulo com leituras de tela roda as dele até o fim, e a auditoria, as pastas e as notificações não lidas continuam iguais

#### Scenario: Aquecimento depois de subir
- **WHEN** a API sobe com o aquecimento ligado e um módulo falha ao aquecer
- **THEN** a API responde enquanto aquece, e os módulos seguintes aquecem mesmo assim
