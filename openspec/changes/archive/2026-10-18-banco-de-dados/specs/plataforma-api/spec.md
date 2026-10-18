# Spec Delta

## MODIFIED Requirements

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
