## ADDED Requirements

### Requirement: E-mails pela caixa compartilhada

Com a caixa de envio no `local.auto.tfvars`, fora do Git, o Terraform MUST ligar o envio pelo Microsoft Graph na aplicação, sem conceder Mail.Send no Entra ID. O script do administrador MUST criar a caixa compartilhada com a entrada da conta bloqueada e dar ao aplicativo o papel "Application Mail.Send" só nessa caixa, pelo RBAC para aplicações do Exchange Online, e conferir que ele não envia pelas outras (ADR-0036, ADR-0123).

#### Scenario: Caixa de envio pelo Terraform
- **WHEN** a instalação é conferida no código do Terraform e dos scripts
- **THEN** a caixa de envio fica fora do repositório e liga o envio pelo Graph, o aplicativo do Entra não pede Mail.Send, e o script dá o papel de envio num escopo só com a caixa e confere o envio pela caixa e a recusa pelas outras

#### Scenario: E-mails da demonstração no ambiente dev
- **WHEN** o ambiente `dev` é conferido
- **THEN** os e-mails do domínio do diretório de demonstração ficam na pasta local do App Service
