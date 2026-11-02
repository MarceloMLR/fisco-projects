## ADDED Requirements

### Requirement: Avisos pelo Teams na instalação

Com a entrada pelo Entra, o Terraform MUST ligar o envio dos avisos pelo Teams pelo Microsoft Graph, sem permissão nova no aplicativo do Entra. O `docs/instalacao.md` MUST ensinar a baixar o pacote, a subi-lo no centro de administração do Teams, a instalá-lo para as pessoas por uma política e só então ligar "Avisar pelo Teams" (ADR-0127, ADR-0149).

#### Scenario: Teams pelo Terraform
- **WHEN** a instalação com a entrada pelo Entra é conferida no código do Terraform
- **THEN** a aplicação recebe o envio pelo Teams pelo Graph, e o aplicativo do Entra não pede nenhuma permissão do Teams

#### Scenario: Passo do Teams no guia
- **WHEN** o guia de instalação é conferido
- **THEN** ele traz o passo do aplicativo do Teams, com quem o faz na empresa, depois do passo do Exchange
