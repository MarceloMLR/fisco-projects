# publicacao Specification

## Purpose
A publicação do ambiente de desenvolvimento e demonstração no Azure a cada merge na `main`, pela identidade federada do pipeline, sem segredos e só com o poder de publicar e conferir, e a conferência da infraestrutura contra o código, sem nada pessoal nos logs públicos (ADR-0055, ADR-0115, ADR-0116).

## Requirements

### Requirement: Publicação a cada merge

Cada merge na `main` MUST publicar a aplicação no ambiente de desenvolvimento e demonstração, montando o pacote com as telas e a API, e MUST esperar o `/health` responder antes de terminar. No ambiente com a entrada local, a publicação MUST carregar em seguida os dados de demonstração que faltam, tentando de novo quando a aplicação reinicia no meio da carga. A lógica MUST ficar nos scripts de `scripts/cd`, chamados pelo workflow, uma publicação por vez (ADR-0055, ADR-0089, ADR-0115).

#### Scenario: Publicação a cada merge
- **WHEN** um PR entra na `main`
- **THEN** o workflow `deploy-dev` monta o pacote, publica no App Service do ambiente `dev` e confere a infraestrutura, nessa ordem

#### Scenario: Demonstração depois da publicação
- **WHEN** a aplicação publicada responde no `/health` num ambiente em `Development`
- **THEN** os dados de demonstração que faltam são carregados pela própria API

#### Scenario: Demonstração depois de um reinício
- **WHEN** a carga dos dados de demonstração falha logo depois da publicação, enquanto a aplicação reinicia
- **THEN** a publicação tenta a carga de novo até três vezes, com 30 segundos de intervalo, e só falha se a última falhar

### Requirement: Pipeline sem segredos e com poder limitado

O pipeline MUST alcançar o Azure por identidade federada, sem segredos, só do environment do GitHub do ambiente (a `main` no `dev`, as tags de versão na produção) e pelo subject imutável do GitHub. A identidade do pipeline MUST só publicar no grupo de recursos, travar e ler o estado do Terraform, ler o Entra ID e, quando a aplicação não administra o banco, administrá-lo para as migrações; ela MUST NOT dar papéis no Azure nem mudar o Entra ID (ADR-0115, ADR-0116, ADR-0125).

#### Scenario: Pipeline sem segredos
- **WHEN** o workflow de publicação é conferido
- **THEN** ele não usa segredos, só pede o token de identidade e a leitura do código, e usa ações fixadas por commit

#### Scenario: Identidade do pipeline com poder limitado
- **WHEN** a identidade do pipeline é criada pelo Terraform
- **THEN** ela só vale para o environment do GitHub do ambiente e tem só os papéis de colaborar no grupo de recursos, gravar no contêiner do estado, ler a conta do estado e ler o diretório e os aplicativos, e entra no grupo dos administradores do banco só quando a aplicação não é administradora

#### Scenario: Federação pelos números do repositório
- **WHEN** o repositório do pipeline é configurado no ambiente
- **THEN** ele é dado pelo dono e pelo repositório com os números de cada um, como no subject do token do GitHub, e um repositório com o mesmo nome e outros números não ganha a identidade

### Requirement: Conferência da infraestrutura

Depois de publicar, o pipeline MUST rodar o `terraform plan` com o estado do ambiente, sem aplicar. Uma diferença MUST aparecer como aviso no resumo da execução, com o que mudaria, sem reprovar a publicação; uma falha do `plan` MUST reprovar. O e-mail dos alertas MUST ficar fora do workflow e dos planos, que aparecem nos logs públicos: o pipeline MUST lê-lo do grupo de ações da instalação e mascará-lo antes de tudo (ADR-0115, ADR-0116).

#### Scenario: Infraestrutura diferente do código
- **WHEN** o `terraform plan` encontra diferenças
- **THEN** a execução avisa que a infraestrutura está diferente do código, mostra o que mudaria e não falha

#### Scenario: Infraestrutura igual ao código
- **WHEN** o `terraform plan` não encontra diferenças
- **THEN** a execução diz que a infraestrutura está igual ao código; se o `plan` falhar, a execução falha

#### Scenario: E-mail dos alertas fora dos logs
- **WHEN** o pipeline confere a infraestrutura
- **THEN** o e-mail dos alertas não está no workflow, é lido do grupo de ações e mascarado antes de qualquer linha do log, e a conferência falha se o grupo não tiver e-mail

### Requirement: Site do SharePoint fora dos logs

O endereço do site do SharePoint MUST ficar fora do workflow, porque traz o nome do tenant: a conferência da infraestrutura MUST lê-lo da configuração da aplicação no ar e mascará-lo antes de conferir (ADR-0116, ADR-0120).

#### Scenario: Site do SharePoint fora dos logs
- **WHEN** o pipeline confere a infraestrutura de um ambiente com o site do SharePoint
- **THEN** o endereço não está no workflow, é lido da aplicação no ar e mascarado, e sem ele a conferência segue sem o site

### Requirement: Caixa de envio fora dos logs

A conferência da infraestrutura MUST ler a caixa de envio da configuração da aplicação no ar e mascará-la nos logs antes de rodar o Terraform, sem que ela vire variável do workflow (ADR-0116, ADR-0123).

#### Scenario: Caixa de envio mascarada
- **WHEN** a conferência da infraestrutura roda com a caixa de envio na configuração da aplicação
- **THEN** a caixa é mascarada nos logs e chega ao Terraform pela variável `email_mailbox`

### Requirement: Publicação em produção

A produção MUST ser publicada só pelo workflow `deploy-prod`, acionado à mão na tag da versão (`vX.Y.Z`), no environment `prod`, que aceita só as tags `v*` e pede a aprovação de quem opera. Ele MUST conferir a versão e a infraestrutura, montar o pacote, aplicar as migrações e publicar, esperando o `/health`, nessa ordem, sem dados de demonstração, e MUST parar antes de mudar algo fora de uma tag de versão ou com a infraestrutura diferente do código (ADR-0125).

#### Scenario: Publicação em produção por versão
- **WHEN** quem opera dispara o `deploy-prod` na tag `v1.2.3` e aprova a publicação
- **THEN** o workflow confere a versão e a infraestrutura, monta o pacote, aplica as migrações e publica no App Service da produção, nessa ordem

#### Scenario: Produção só por tag de versão
- **WHEN** o `deploy-prod` roda numa branch ou numa tag que não é `vX.Y.Z`
- **THEN** ele falha antes de conferir a infraestrutura

#### Scenario: Produção com a infraestrutura diferente
- **WHEN** o `terraform plan` da produção encontra diferenças
- **THEN** a publicação para, mostra o que mudaria e pede que quem opera aplique o plano

#### Scenario: Migrações antes da publicação
- **WHEN** a publicação aplica as migrações da produção
- **THEN** o comando `migrate` do pacote roda com a conexão pelo Entra ID da conta do pipeline e com a identidade gerenciada da aplicação, lida do App Service

#### Scenario: Produção sem dados de demonstração
- **WHEN** a aplicação publicada na produção responde no `/health`
- **THEN** nenhum dado de demonstração é carregado

#### Scenario: Produção com aprovação
- **WHEN** o environment `prod` é preparado pelo `Set-GitHubEnvironment.ps1`
- **THEN** ele aceita só as tags `v*` e pede a aprovação de quem o preparou

### Requirement: Pacote da versão

Cada versão publicada no GitHub numa tag `vX.Y.Z` MUST ganhar, pelo workflow `release`, o pacote `fiscoprojects-vX.Y.Z.zip`, anexado à versão, com a aplicação (`app.zip`), o Terraform sem os parâmetros dos ambientes deste repositório, os scripts de `infra/scripts` e os guias, tirados só do Git. O workflow MUST NOT alcançar o Azure, e uma tag fora de `vX.Y.Z` MUST NOT ganhar pacote (ADR-0127).

#### Scenario: Pacote anexado à versão
- **WHEN** Marcelo publica no GitHub a versão `v1.2.3`
- **THEN** o workflow `release` monta a aplicação e anexa à versão o `fiscoprojects-v1.2.3.zip`, com o `app.zip`, o arquivo `VERSION`, o Terraform, os scripts e os guias

#### Scenario: Pacote só de versão
- **WHEN** a versão é publicada numa tag que não é `vX.Y.Z`
- **THEN** o workflow falha e nada é anexado

#### Scenario: Pacote sem os ambientes
- **WHEN** o pacote é montado num repositório com os parâmetros dos ambientes, o `local.auto.tfvars` e o estado do Terraform
- **THEN** nenhum deles entra no pacote

### Requirement: Publicação de uma versão à mão

Numa instalação atualizada fora do pipeline deste repositório, quem opera MUST publicar a versão do pacote pelo `Publish-Version.ps1`, com o Azure CLI na assinatura da instalação: ler a identidade gerenciada da aplicação, aplicar as migrações pelo comando `migrate` do pacote só com a conta do Azure CLI, com o firewall do banco aberto só para quem roda e só durante as migrações, e publicar o `app.zip`, esperando o `/health` (ADR-0125, ADR-0127).

#### Scenario: Publicação à mão
- **WHEN** quem opera roda o `Publish-Version.ps1` na pasta do pacote, depois do `terraform apply`
- **THEN** as migrações rodam pelo comando `migrate` do pacote, com a conta do Azure CLI e a identidade da aplicação, e o `app.zip` é publicado, esperando o `/health`

#### Scenario: Firewall aberto só nas migrações
- **WHEN** as migrações terminam, com ou sem falha
- **THEN** a regra temporária do firewall do banco é apagada

#### Scenario: Publicação sem a identidade da aplicação
- **WHEN** o App Service da instalação não tem identidade gerenciada
- **THEN** o script para antes de mudar o banco, pedindo o `terraform apply`
