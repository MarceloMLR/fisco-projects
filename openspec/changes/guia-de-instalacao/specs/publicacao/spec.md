## ADDED Requirements

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
