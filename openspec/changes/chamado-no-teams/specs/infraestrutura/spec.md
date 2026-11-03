## ADDED Requirements

### Requirement: Aplicativo do Entra pronto para o Teams

O Terraform MUST expor a API do aplicativo do Entra da instalação para a entrada única do Teams: o identificador `api://{endereço do site}/{cliente}`, o mesmo do manifesto do Teams; o escopo `access_as_user`, com o consentimento do administrador; os tokens de acesso v2; o Teams do computador e do celular e o da web como clientes pré-autorizados; e a declaração de conta convidada no token de acesso. Nenhuma permissão do Graph MUST mudar (ADR-0117, ADR-0152).

#### Scenario: Aplicativo do Entra para o Teams
- **WHEN** o aplicativo do Entra é conferido no código do Terraform
- **THEN** ele expõe o identificador com o endereço do site e o cliente, o escopo `access_as_user`, os tokens v2, os dois clientes do Teams pré-autorizados nesse escopo e a declaração de conta convidada no token de acesso, e continua pedindo só as permissões do Graph de antes
