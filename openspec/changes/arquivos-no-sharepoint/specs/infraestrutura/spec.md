## MODIFIED Requirements

### Requirement: Aplicativo do Entra lê o diretório

O Terraform MUST dar ao aplicativo do Entra da instalação as permissões de aplicativo `User.Read.All` do Microsoft Graph, para a busca de pessoas, e `Sites.Selected`, para os arquivos só nos sites dados a ele, as duas com o consentimento do administrador, e nenhuma outra permissão de aplicativo (ADR-0119, ADR-0120).

#### Scenario: Permissão para ler o diretório
- **WHEN** o aplicativo do Entra é conferido no código do Terraform
- **THEN** ele pede as permissões de aplicativo `User.Read.All` e `Sites.Selected` do Graph, com o consentimento do administrador, e nenhuma outra

## ADDED Requirements

### Requirement: Arquivos no site do SharePoint

Com o endereço do site no `local.auto.tfvars`, fora do Git, o Terraform MUST ligar o armazenamento do SharePoint na aplicação, com a biblioteca e a pasta raiz do ambiente. O script do administrador MUST dar o site ao aplicativo só com o papel de escrita (ADR-0030, ADR-0120).

#### Scenario: Arquivos no site do SharePoint
- **WHEN** a instalação é conferida no código do Terraform e dos scripts
- **THEN** o endereço do site fica fora do repositório, liga o armazenamento do SharePoint na aplicação, e o script dá o site só com o papel de escrita
