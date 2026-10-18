# Spec Delta

## MODIFIED Requirements

### Requirement: Erros no formato Problem Details

Toda resposta de erro da API (status 400 a 599) sem corpo próprio MUST ser enviada no formato Problem Details (RFC 9457), com o tipo de conteúdo `application/problem+json`, o status e um título.

#### Scenario: Rota inexistente
- **WHEN** alguém faz uma requisição a um caminho sob `/api` que não existe
- **THEN** a resposta tem status 404, tipo de conteúdo `application/problem+json` e corpo com `status` 404 e um `title`
