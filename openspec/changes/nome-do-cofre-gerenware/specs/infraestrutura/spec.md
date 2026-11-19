## MODIFIED Requirements

### Requirement: Instalação pelo Terraform

O Terraform de `infra/terraform` MUST criar uma instalação inteira num grupo de recursos: o App Service Linux com identidade gerenciada, o Azure SQL, o Key Vault, o Log Analytics com o Application Insights, o teste de disponibilidade, os alertas e o aplicativo no Entra ID, com os nomes tirados do código da instalação e do ambiente. As versões do Terraform e dos provedores MUST ficar fixadas, com as somas dos provedores no arquivo de travas (ADR-0053, ADR-0114).

#### Scenario: Ambiente criado pelo Terraform
- **WHEN** quem opera a instalação aplica o Terraform com os parâmetros de um ambiente
- **THEN** a instalação inteira é criada no grupo de recursos daquele ambiente, com o endereço `https://app-gerenware-<instalação>-<ambiente>.azurewebsites.net` e o cofre `kv-gw<instalação><ambiente>`
