## Context

A instalação de Marcelo é criada pelo Terraform a partir do repositório, com os parâmetros em `infra/terraform/environments/` e os valores pessoais no `local.auto.tfvars`, e publicada pelo pipeline do repositório (`deploy-dev` a cada merge, `deploy-prod` à mão na tag, ADR-0115, ADR-0125), que monta o pacote da aplicação a cada publicação. O repositório é público, e os logs do GitHub Actions também (ADR-0116). Os scripts `Prepare-TerraformState.ps1`, `Grant-SharePointSite.ps1` e `Grant-ExchangeMailbox.ps1` leem o `local.auto.tfvars` e o `terraform output` a partir da própria pasta (`infra/scripts/../terraform`).

## Goals / Non-Goals

**Goals:**
- Instalar e atualizar o FiscoProjects na assinatura de um cliente a partir de uma versão publicada, pelo fornecedor ou pela TI do cliente, com as mesmas ferramentas e sem cópia do repositório.

**Non-Goals:**
- Pipeline por cliente, Marketplace e repositório privado (proposta).

## Decisions

- **Pacote da versão.** O workflow `release` roda quando uma versão é publicada no GitHub (`release: published`), monta a aplicação pelo `publish.sh` e chama o `release.sh`, que confere a tag (`vX.Y.Z`), junta pelo `git archive` do commit da tag o `infra/terraform` (sem `environments/`), o `infra/scripts` e os guias (`docs/instalacao.md`, `restauracao.md`, `custos-estimados.md`), põe o `app.zip` e o arquivo `VERSION` e anexa o `fiscoprojects-vX.Y.Z.zip` à versão pelo `gh release upload --clobber`. O `git archive` garante que nada local entra (o `local.auto.tfvars`, o estado, o `.terraform`). O workflow só tem `contents: write`, sem `id-token`: não alcança o Azure.
- **Arquivos privados da instalação.** No pacote, quem opera põe em `infra/terraform/` o `local.auto.tfvars`, agora com todos os parâmetros da instalação (inclusive `installation` e `environment`), e o `backend.hcl` do estado. Os dois ficam guardados numa pasta privada da instalação e são copiados para cada versão nova. Assim os scripts de `infra/scripts` funcionam no pacote como no repositório, e nenhum parâmetro de cliente vai para o repositório.
- **Publicação à mão.** O `Publish-Version.ps1` (PowerShell, como os outros scripts de quem opera) lê `installation`, `environment` e `aspnetcore_environment` do `local.auto.tfvars` e monta os nomes do `main.tf`. Fora do `Development`, abre a regra `publicacao-temporaria` no firewall do banco para o endereço de quem roda (do `api.ipify.org` ou de `-ClientAddress`), descompacta o `app.zip` numa pasta temporária e roda o `migrate` com a mesma configuração do `migrate.sh`, tentando de novo até 3 vezes enquanto a regra nova não vale; num `finally`, apaga a regra e a pasta. Depois publica o `app.zip` e espera o `/health` por até 10 minutos, como o `deploy.sh`.
- **Só a conta do Azure CLI.** A conexão `Active Directory Default` usa o `DefaultAzureCredential`, que, num computador com o Visual Studio, pode pegar outra conta antes da do Azure CLI. O script põe `AZURE_TOKEN_CREDENTIALS=AzureCliCredential`, aceito pelo Azure.Identity a partir da 1.15 (a aplicação usa a 1.17), só no processo do `migrate`.
- **Guia.** O `docs/instalacao.md` ganha, antes da referência que já existia, as partes "Antes de começar" (o que o cliente providencia e as ferramentas), "Quem instala" (o fornecedor, padrão, com uma conta no tenant do cliente e os papéis da instalação e das atualizações; ou a TI do cliente), "Pacote da versão", "Instalar" (estado, parâmetros, Terraform, segredo do Syncfusion, SharePoint, Exchange, publicação, primeira entrada e configuração) e "Atualizar para uma versão nova" (com a volta a uma anterior). A instalação de Marcelo pelo repositório e o pipeline ficam numa parte própria.
- **Testes.** O workflow é lido como texto, como os outros; o `release.sh` roda com um `gh` substituto, e o pacote é aberto pelo próprio teste. Os scripts PowerShell são conferidos como texto, como os outros (o ambiente dos testes não tem o PowerShell). O guia é conferido contra a pasta `infra/scripts` e pela ordem das partes.

## Risks / Trade-offs

- [O pacote é público enquanto o repositório for público] → ele só tem o que já está no repositório; os parâmetros de cada cliente ficam na pasta privada da instalação.
- [A atualização à mão depende de quem opera] → o mesmo script em todas as instalações e o guia com a ordem dos passos; o pipeline por cliente fica para quando o repositório for privado.
- [O endereço público de quem roda vem de um serviço externo] → pode ser passado em `-ClientAddress`, e a regra existe só durante as migrações.
