## ADDED Requirements

### Requirement: Site do SharePoint fora dos logs

O endereço do site do SharePoint MUST ficar fora do workflow, porque traz o nome do tenant: a conferência da infraestrutura MUST lê-lo da configuração da aplicação no ar e mascará-lo antes de conferir (ADR-0116, ADR-0120).

#### Scenario: Site do SharePoint fora dos logs
- **WHEN** o pipeline confere a infraestrutura de um ambiente com o site do SharePoint
- **THEN** o endereço não está no workflow, é lido da aplicação no ar e mascarado, e sem ele a conferência segue sem o site
