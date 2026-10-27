# infraestrutura Specification

## Purpose
Define como cada instalação do FiscoProjects é criada no Azure pelo Terraform: os recursos, o acesso sem senhas, os padrões da produção, o ambiente de desenvolvimento, o monitoramento e o estado protegido (ADR-0052, ADR-0053, ADR-0114).

## Requirements

### Requirement: Instalação pelo Terraform

O Terraform de `infra/terraform` MUST criar uma instalação inteira num grupo de recursos: o App Service Linux com identidade gerenciada, o Azure SQL, o Key Vault, o Log Analytics com o Application Insights, o teste de disponibilidade, os alertas e o aplicativo no Entra ID, com os nomes tirados do código da instalação e do ambiente. As versões do Terraform e dos provedores MUST ficar fixadas, com as somas dos provedores no arquivo de travas (ADR-0053, ADR-0114).

#### Scenario: Ambiente criado pelo Terraform
- **WHEN** quem opera a instalação aplica o Terraform com os parâmetros de um ambiente
- **THEN** a instalação inteira é criada no grupo de recursos daquele ambiente, com o endereço `https://app-fiscoprojects-<instalação>-<ambiente>.azurewebsites.net`

### Requirement: Instalação sem senhas

O banco MUST aceitar só identidades do Entra ID, com um grupo de administradores; a aplicação MUST entrar nele e no Key Vault pela identidade gerenciada, sem senha, e o aplicativo do Entra MUST confiar nela, sem segredo nem certificado. O Terraform MUST NOT criar segredos, certificados ou chaves, que são criados à mão no Key Vault. Os valores pessoais da instalação e o estado MUST ficar fora do repositório (ADR-0052, ADR-0053, ADR-0114, ADR-0117).

#### Scenario: Banco sem senha
- **WHEN** a aplicação se conecta ao banco
- **THEN** ela entra com a identidade gerenciada, e o servidor não tem usuário nem senha do SQL

#### Scenario: Segredos fora do Terraform
- **WHEN** o código do Terraform e os parâmetros dos ambientes são conferidos
- **THEN** nenhum cria segredo, certificado ou chave, nenhum guarda os valores pessoais da instalação, e o estado e os valores pessoais são ignorados pelo Git

#### Scenario: Aplicativo do Entra sem segredo
- **WHEN** o aplicativo do Entra é conferido no código do Terraform
- **THEN** a única credencial dele é a confiança na identidade gerenciada da aplicação, sem senha nem certificado

### Requirement: Padrões da produção

Os padrões das variáveis MUST ser os da produção: banco S0 com 14 dias de restauração, um backup por mês guardado por 12 meses e redundância por zona, `Production`, Brazil South, Key Vault com proteção contra exclusão definitiva e só HTTPS (ADR-0048, ADR-0052, ADR-0059).

#### Scenario: Backups da produção
- **WHEN** um ambiente não muda os padrões
- **THEN** ele sai com os backups, a região e as proteções da produção

### Requirement: Ambiente de desenvolvimento no Azure

O ambiente de desenvolvimento e demonstração MUST usar o banco Basic, sem backups mensais, para ficar no ar o mês inteiro com custo pequeno e não deixar cobrança depois de apagado. Até as integrações com o Microsoft 365, ele MUST rodar como `Development`, com a entrada local e os arquivos e e-mails na pasta persistente do App Service, e a aplicação aplica as próprias migrações (ADR-0114).

#### Scenario: Ambiente de desenvolvimento apagado sem custos
- **WHEN** o ambiente de desenvolvimento é apagado com `terraform destroy`
- **THEN** nada dele continua cobrado, porque ele não guarda backups mensais

### Requirement: Monitoramento e alertas da instalação

A instalação MUST mandar a telemetria ao Application Insights, com limite diário, e MUST avisar o administrador por e-mail quando o `/health` falhar no teste de disponibilidade, quando as requisições com falha passarem do limite em 15 minutos, quando um e-mail esperar envio há mais de 15 minutos e quando o banco passar de 80% do tamanho. Os alertas MUST poder ficar desligados enquanto a aplicação não está publicada (ADR-0057, ADR-0112, ADR-0114).

#### Scenario: Alertas da instalação
- **WHEN** a aplicação fica fora do ar, os erros aumentam, a fila de e-mails para ou o banco enche
- **THEN** o administrador recebe o alerta por e-mail

### Requirement: Estado do Terraform protegido

O estado de cada instalação MUST ficar numa conta de armazenamento própria, criada por script antes do primeiro `apply`, com versionamento, exclusão reversível, sem acesso público e sem chaves compartilhadas, e o Terraform MUST entrar nela pelo Entra ID (ADR-0053, ADR-0114).

#### Scenario: Estado do Terraform
- **WHEN** quem opera prepara uma instalação nova
- **THEN** o script cria a conta do estado protegida e dá à pessoa o papel para ler e gravar o estado

### Requirement: Aplicativo do Entra pronto para a entrada

O Terraform MUST configurar o aplicativo do Entra da instalação para a entrada: o endereço de retorno da aplicação, as declarações de conta convidada e de e-mail, o consentimento do administrador só para `openid`, `profile` e `email`, a atribuição obrigatória, ligada por padrão, e a confiança na identidade gerenciada da aplicação. A aplicação MUST receber o tenant e o cliente do aplicativo e montar o endereço de retorno em HTTPS atrás do App Service (ADR-0005, ADR-0117).

#### Scenario: Aplicativo do Entra para a entrada
- **WHEN** quem opera aplica o Terraform de um ambiente
- **THEN** o aplicativo do Entra aceita a volta só no endereço da aplicação, pede só `openid`, `profile` e `email`, exige atribuição e confia na identidade gerenciada da aplicação, que recebe o tenant e o cliente

### Requirement: Aplicativo do Entra lê o diretório

O Terraform MUST dar ao aplicativo do Entra da instalação as permissões de aplicativo `User.Read.All` do Microsoft Graph, para a busca de pessoas, e `Sites.Selected`, para os arquivos só nos sites dados a ele, as duas com o consentimento do administrador, e nenhuma outra permissão de aplicativo (ADR-0119, ADR-0120).

#### Scenario: Permissão para ler o diretório
- **WHEN** o aplicativo do Entra é conferido no código do Terraform
- **THEN** ele pede as permissões de aplicativo `User.Read.All` e `Sites.Selected` do Graph, com o consentimento do administrador, e nenhuma outra

### Requirement: Arquivos no site do SharePoint

Com o endereço do site no `local.auto.tfvars`, fora do Git, o Terraform MUST ligar o armazenamento do SharePoint na aplicação, com a biblioteca e a pasta raiz do ambiente. O script do administrador MUST dar o site ao aplicativo só com o papel de escrita (ADR-0030, ADR-0120).

#### Scenario: Arquivos no site do SharePoint
- **WHEN** a instalação é conferida no código do Terraform e dos scripts
- **THEN** o endereço do site fica fora do repositório, liga o armazenamento do SharePoint na aplicação, e o script dá o site só com o papel de escrita

### Requirement: E-mails pela caixa compartilhada

Com a caixa de envio no `local.auto.tfvars`, fora do Git, o Terraform MUST ligar o envio pelo Microsoft Graph na aplicação, sem conceder Mail.Send no Entra ID, e o ambiente `dev` MUST manter na pasta local os e-mails do domínio da demonstração (ADR-0036, ADR-0123).

#### Scenario: Caixa de envio pelo Terraform
- **WHEN** a instalação é conferida no código do Terraform e dos scripts
- **THEN** a caixa de envio fica fora do repositório e liga o envio pelo Graph, o aplicativo do Entra não pede Mail.Send, e o script dá o papel de envio num escopo só com a caixa e confere o envio pela caixa e a recusa pelas outras

#### Scenario: E-mails da demonstração no ambiente dev
- **WHEN** o ambiente `dev` é conferido
- **THEN** os e-mails do domínio do diretório de demonstração ficam na pasta local do App Service

### Requirement: Caixa de envio preparada pelo script

O script do administrador MUST usar a caixa que a empresa já tem, compartilhada ou de usuário, sem mudá-la, ou criar uma caixa compartilhada com a entrada da conta bloqueada, e MUST dar ao aplicativo o papel "Application Mail.Send" só nessa caixa, pelo RBAC para aplicações do Exchange Online, conferindo que ele não envia pelas outras (ADR-0036, ADR-0123).

#### Scenario: Caixa que a empresa já tem
- **WHEN** o script roda com o endereço de uma caixa compartilhada ou de usuário que já existe
- **THEN** a caixa é usada como está, sem mudar a conta dela, e um grupo ou lista de distribuição é recusado
