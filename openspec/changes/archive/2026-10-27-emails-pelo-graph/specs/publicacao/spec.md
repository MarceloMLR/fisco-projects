## ADDED Requirements

### Requirement: Caixa de envio fora dos logs

A conferência da infraestrutura MUST ler a caixa de envio da configuração da aplicação no ar e mascará-la nos logs antes de rodar o Terraform, sem que ela vire variável do workflow (ADR-0116, ADR-0123).

#### Scenario: Caixa de envio mascarada
- **WHEN** a conferência da infraestrutura roda com a caixa de envio na configuração da aplicação
- **THEN** a caixa é mascarada nos logs e chega ao Terraform pela variável `email_mailbox`
