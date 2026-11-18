## Context

O `infra/terraform/main.tf` monta os nomes de todos os recursos de `fiscoprojects-<instalação>-<ambiente>`. O estado fica num grupo e numa conta próprios, criados pelo `Prepare-TerraformState.ps1` e apontados pelos `environments/*.backend.hcl`. Os scripts de publicação (`scripts/cd`) e o `Publish-Version.ps1` montam os mesmos nomes. O registro do aplicativo no Entra ID e a identidade do deploy são recursos do Terraform; as permissões no SharePoint e no Exchange são dadas por scripts, fora dele.

## Goals / Non-Goals

**Goals:** todo nome novo no código e nos guias; um roteiro curto e na ordem certa para quem opera.

**Non-Goals:** automatizar o `destroy` e o `apply`; preservar dados.

## Decisions

- **Troca só de nomes no código.** Nenhum recurso, variável ou módulo novo: mudam o prefixo em `main.tf`, o banco, o estado, o nome curto dos alertas e o nome do pacote. O prefixo novo é mais curto, então os limites de tamanho do Azure (Key Vault, conta de armazenamento) continuam atendidos.
- **Conta do estado `stgw<instalação>tfstate`.** O nome antigo abreviava o produto em `fp`; o novo abrevia em `gw`.
- **Operação fora do PR.** O `destroy` precisa do código antigo, e o `apply`, do novo. Por isso a operação é um roteiro, feito por quem opera depois do merge.

## Migration Plan

Para cada ambiente (`dev`, depois `prod`), com a instalação `mlr`:

1. **Antes de usar o código novo**, num checkout do commit anterior a esta change: `terraform -chdir=infra/terraform init -reconfigure -backend-config=environments/<ambiente>.backend.hcl` e `terraform -chdir=infra/terraform destroy -var-file=environments/<ambiente>.tfvars`.
2. Tirar o que os scripts deram ao registro antigo: no Exchange Online, a atribuição do papel "Application Mail.Send" e o escopo dela; no SharePoint, a permissão do aplicativo no site; no Teams, o aplicativo antigo do catálogo (seção "Apagar uma instalação" do `docs/instalacao.md`).
3. Com os dois ambientes destruídos, apagar o estado antigo: `az group delete --name rg-fiscoprojects-mlr-tfstate`.
4. Na `main` com esta change: `infra/scripts/Prepare-TerraformState.ps1 -Installation mlr`, que cria `rg-gerenware-mlr-tfstate` e `stgwmlrtfstate`.
5. `terraform init -reconfigure -backend-config=environments/<ambiente>.backend.hcl` e `terraform apply -var-file=environments/<ambiente>.tfvars`.
6. Criar de novo os segredos do Key Vault feitos à mão (`Signature--Autentique--ApiKey` e `Signature--Autentique--WebhookSecret`).
7. `infra/scripts/Grant-SharePointSite.ps1` e `infra/scripts/Grant-ExchangeMailbox.ps1` para o registro novo.
8. `infra/scripts/Set-GitHubEnvironment.ps1`, para as variáveis do environment do GitHub apontarem para os recursos novos.
9. Publicar: rodar de novo o `deploy-dev` (no `dev`, ele carrega a demonstração) ou o `deploy-prod` na tag da versão.
10. No painel da Autentique, trocar o endereço do aviso para `https://app-gerenware-mlr-<ambiente>.azurewebsites.net/signature-notices`.
11. Nas Configurações gerais, baixar o pacote do Teams, subir no centro de administração do Teams e instalar.
12. Conferir: `/health` do endereço novo, a entrada pelo Entra, um arquivo enviado, o teste de envio de e-mail e um aviso pelo Teams.

Volta: o código antigo recria os ambientes antigos pelo mesmo caminho, com os nomes de antes.

## Risks / Trade-offs

- Ambientes fora do ar durante a operação → só há demonstração; fazer o `dev` inteiro antes do `prod`.
- Esquecer um passo manual do Microsoft 365 → o passo 12 confere cada integração.
- Entre o merge da Marca-5 e o passo 5, o `deploy-dev` falha no `/health` → esperado; ele volta a passar no passo 9.
