## ADDED Requirements

### Requirement: Assinatura pela Autentique na instalação

Com a variável `autentique` do Terraform, a aplicação MUST usar a Autentique, com a chave e o segredo do aviso lidos do Key Vault, criados à mão por quem opera, nunca pelo Terraform. O `dev` MUST usar o sandbox e manter no provedor local os signatários da demonstração, e o guia MUST ensinar a criar a chave, o aviso e os segredos (ADR-0053, ADR-0129, ADR-0130).

#### Scenario: Autentique pelo Terraform
- **WHEN** a instalação é conferida no código do Terraform
- **THEN** a variável `autentique` liga o provedor da Autentique com a chave e o segredo do aviso por referência ao Key Vault, sem nenhum valor de segredo no código

#### Scenario: Sandbox no ambiente dev
- **WHEN** o ambiente `dev` é conferido
- **THEN** ele usa o sandbox da Autentique com o aviso, e os signatários do domínio da demonstração ficam no provedor local

#### Scenario: Autentique no guia
- **WHEN** o guia de instalação é conferido
- **THEN** ele ensina a criar a chave e o segredo do aviso no Key Vault com os nomes que o Terraform lê, e o endereço do aviso a cadastrar na Autentique
