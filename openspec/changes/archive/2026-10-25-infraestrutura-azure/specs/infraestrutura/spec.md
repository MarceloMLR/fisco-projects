## ADDED Requirements

### Requirement: Instalação pelo Terraform

O Terraform de `infra/terraform` MUST criar uma instalação inteira num grupo de recursos: o App Service Linux com identidade gerenciada, o Azure SQL, o Key Vault, o Log Analytics com o Application Insights, o teste de disponibilidade, os alertas e o aplicativo no Entra ID, com os nomes tirados do código da instalação e do ambiente. As versões do Terraform e dos provedores MUST ficar fixadas, com as somas dos provedores no arquivo de travas (ADR-0053, ADR-0114).

#### Scenario: Ambiente criado pelo Terraform
- **WHEN** quem opera a instalação aplica o Terraform com os parâmetros de um ambiente
- **THEN** a instalação inteira é criada no grupo de recursos daquele ambiente, com o endereço `https://app-fiscoprojects-<instalação>-<ambiente>.azurewebsites.net`

### Requirement: Instalação sem senhas

O banco MUST aceitar só identidades do Entra ID, com um grupo de administradores, e a aplicação MUST entrar nele e no Key Vault pela identidade gerenciada, sem senha. O Terraform MUST NOT criar segredos, certificados ou chaves: eles são criados à mão no Key Vault e lidos pela aplicação por referência. Os valores pessoais da instalação (quem opera e o e-mail dos alertas) e o estado MUST ficar fora do repositório (ADR-0052, ADR-0053, ADR-0114).

#### Scenario: Banco sem senha
- **WHEN** a aplicação se conecta ao banco
- **THEN** ela entra com a identidade gerenciada, e o servidor não tem usuário nem senha do SQL

#### Scenario: Segredos fora do Terraform
- **WHEN** o código do Terraform e os parâmetros dos ambientes são conferidos
- **THEN** nenhum cria segredo, certificado ou chave, nenhum guarda os valores pessoais da instalação, e o estado e os valores pessoais são ignorados pelo Git

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
