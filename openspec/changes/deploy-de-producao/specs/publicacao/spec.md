## ADDED Requirements

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

## MODIFIED Requirements

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
