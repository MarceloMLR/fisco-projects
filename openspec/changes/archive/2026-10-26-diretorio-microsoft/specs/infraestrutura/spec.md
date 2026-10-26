## ADDED Requirements

### Requirement: Aplicativo do Entra lê o diretório

O Terraform MUST dar ao aplicativo do Entra da instalação a permissão de aplicativo `User.Read.All` do Microsoft Graph, com o consentimento do administrador, para a busca de pessoas, e nenhuma outra permissão de aplicativo (ADR-0119).

#### Scenario: Permissão para ler o diretório
- **WHEN** o aplicativo do Entra é conferido no código do Terraform
- **THEN** ele pede a permissão de aplicativo `User.Read.All` do Graph, com o consentimento do administrador, e nenhuma outra
