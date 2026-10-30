# Guia de instalação

Como instalar o FiscoProjects na nuvem de uma empresa e como atualizá-lo. Cada empresa tem a própria instalação, na assinatura do Azure e no tenant do Microsoft 365 dela (ADR-0003), feita a partir do pacote de uma versão publicada, nunca de uma cópia do repositório (ADR-0127). Quem instala é o fornecedor, por padrão, ou a TI da empresa, com os mesmos passos.

As partes "Antes de começar" a "Atualizar para uma versão nova" são o passo a passo. A "Referência", no fim, explica cada requisito com o ADR que o decidiu (os ADRs ficam no repositório do FiscoProjects, em `docs/adr/`). A instalação de Marcelo, publicada pelo pipeline do repositório, está em "Instalação pelo repositório".

## Antes de começar

### O que a empresa providencia

- O tenant do Microsoft 365, com o Entra ID, o Exchange Online e o SharePoint Online. Só as pessoas da empresa (membros do tenant) entram; convidados são recusados (ADR-0007).
- Uma assinatura do Azure, de preferência só para o FiscoProjects, para separar custos e acessos. Os recursos ficam na Brazil South (ADR-0052); os valores estão em [custos estimados](custos-estimados.md).
- O código da instalação, de 2 a 6 letras minúsculas ou números (por exemplo, as iniciais da empresa). Ele entra no nome de todos os recursos e no endereço `https://app-fiscoprojects-<código>-prod.azurewebsites.net`, e precisa ser único entre as instalações do FiscoProjects, porque o endereço, o servidor do banco, o Key Vault e a conta do estado têm nomes únicos no Azure inteiro. O nome do Key Vault fica reservado por 90 dias depois de apagada uma instalação.
- O site do SharePoint dos arquivos e, se quiser, a biblioteca e uma pasta dentro dela (veja SharePoint, na Referência).
- A caixa dos e-mails do sistema: uma que a empresa já tem, como um projetos@ que a equipe lê, ou uma compartilhada que o script do Exchange cria (veja Exchange Online, na Referência).
- Quem será o primeiro administrador do FiscoProjects e o e-mail que recebe os alertas da instalação.
- A chave de licença do Syncfusion, do titular da instalação (veja Licenças de terceiros, na Referência).
- Se os termos de aceite forem assinados pelo sistema, a conta da Autentique, com o plano que couber no volume de termos (veja Assinatura dos termos de aceite, na Referência). Sem ela, a gestão só anexa os termos assinados por fora.

### Ferramentas de quem instala

No computador de quem instala (Windows, macOS ou Linux):

- Azure CLI.
- Terraform 1.16.5 (no Windows, `winget install Hashicorp.Terraform --version 1.16.5`).
- PowerShell 7.
- ASP.NET Core Runtime 10, para as migrações do banco (no Windows, `winget install Microsoft.DotNet.AspNetCore.10`).
- Para os scripts do SharePoint e do Exchange, os módulos do PowerShell `Microsoft.Graph.Authentication`, `Microsoft.Graph.Sites` e `ExchangeOnlineManagement` (o comando de cada um está no passo dele).
- Uma rede que saia pela porta 1433 até o banco do Azure, para as migrações; redes de empresa costumam bloqueá-la.

## Quem instala

### O fornecedor (padrão)

A empresa cria, no próprio tenant, uma conta para o fornecedor (por exemplo, `fornecedor.fiscoprojects@<domínio da empresa>`), com a verificação em duas etapas, e dá a ela os papéis abaixo. O fornecedor instala e atualiza com essa conta, e o acesso pode ser revisto ou retirado a qualquer momento pela empresa.

| Para quê | Papel | Quando |
| --- | --- | --- |
| Criar os recursos do Azure e dar acesso à identidade da aplicação | Proprietário da assinatura do FiscoProjects | Na instalação e nas atualizações |
| Criar o aplicativo e o grupo dos administradores do banco no Entra ID | Administrador de aplicativos e Administrador de grupos | Na instalação |
| Dar o consentimento às permissões do aplicativo (`User.Read.All`, `Sites.Selected`) | Administrador de função privilegiada | Na instalação |
| Dar ao aplicativo o site do SharePoint | Administrador do SharePoint e, se o tenant ainda não consentiu o Microsoft Graph PowerShell com `Sites.FullControl.All`, Administrador de aplicativos | Na instalação |
| Liberar o envio pela caixa de e-mail | Administrador do Exchange e, para bloquear a entrada da caixa que o script criar, Administrador de usuários | Na instalação |

Depois da instalação, a empresa pode retirar os papéis do Entra ID, do SharePoint e do Exchange: quem opera fica dono do aplicativo e do grupo dos administradores do banco, o que basta para as atualizações. Se uma versão nova pedir outra permissão do aplicativo, o plano do Terraform mostra, e o consentimento volta a pedir o papel. Os scripts do SharePoint e do Exchange também podem ser rodados pelos administradores da empresa, com as contas deles, em vez de a conta do fornecedor ganhar esses papéis. Eles precisam só do pacote da versão, do Azure CLI no tenant (`az login --tenant <tenant> --allow-no-subscriptions`) e dos valores que quem opera passa: o id do aplicativo (`terraform -chdir=infra/terraform output -raw entra_client_id`), o site e a caixa (veja os passos 6 e 7).

### A TI da empresa

A TI da empresa faz os mesmos passos com as próprias contas e os mesmos papéis, a partir do pacote da versão. O fornecedor envia o pacote de cada versão e acompanha a instalação.

Em qualquer caso, quem opera a instalação entra em `operator_object_ids`: fica dono dos objetos do Entra ID e administrador do banco e do Key Vault. O acesso ao estado do Terraform é à parte: o `Prepare-TerraformState.ps1` o dá a quem o roda, e cada pessoa que passar a operar depois ganha o papel na conta do estado, dado por quem já opera:

```powershell
az role assignment create --assignee <id da pessoa> --role 'Storage Blob Data Contributor' --scope (az storage account show --name stfp<código>tfstate --query id -o tsv)
```

## Pacote da versão

Cada versão do FiscoProjects tem um número (`v1.2.0`) e um pacote, `fiscoprojects-v1.2.0.zip`, anexado à página da versão no GitHub (Releases) ou enviado pelo fornecedor (ADR-0127). O pacote tem:

- `app.zip`: a aplicação, pronta para o App Service.
- `VERSION`: o número da versão.
- `infra/terraform`: a infraestrutura da instalação.
- `infra/scripts`: os scripts de quem opera.
- `docs`: este guia, o [roteiro de restauração](restauracao.md) e os [custos estimados](custos-estimados.md).

Os parâmetros da instalação ficam em dois arquivos privados, que nunca vão para o repositório nem para o pacote: o `local.auto.tfvars`, com todos os valores da instalação, e o `backend.hcl`, que diz onde está o estado do Terraform. Eles trazem o nome do tenant, e-mails e identificadores de pessoas, então ficam numa pasta privada de quem opera, com backup. A cada versão, descompacte o pacote numa pasta nova (por exemplo, `fiscoprojects-v1.2.0`) e copie os dois arquivos para `infra/terraform/` dela. Os comandos deste guia rodam na pasta do pacote, no PowerShell 7. No Windows, desbloqueie antes os scripts baixados da internet, que o PowerShell recusa por não terem assinatura: `Get-ChildItem -Recurse | Unblock-File`, na pasta do pacote.

## Instalar

1. **Entre na assinatura** da empresa: `az login --tenant <domínio ou id do tenant>` e `az account set --subscription <assinatura>`.

2. **Prepare o estado do Terraform**, uma vez por instalação: `./infra/scripts/Prepare-TerraformState.ps1 -Installation <código>`. Depois, crie o `infra/terraform/backend.hcl`:

   ```hcl
   resource_group_name  = "rg-fiscoprojects-<código>-tfstate"
   storage_account_name = "stfp<código>tfstate"
   container_name       = "tfstate"
   key                  = "prod.tfstate"
   ```


3. **Escreva os parâmetros** em `infra/terraform/local.auto.tfvars`. Os valores que não aparecem aqui ficam nos padrões da produção (`infra/terraform/variables.tf`):

   ```hcl
   installation = "<código>"
   environment  = "prod"

   # Quem opera: az ad signed-in-user show --query id -o tsv
   operator_object_ids = ["<id de quem opera>"]
   alert_email         = "<e-mail que recebe os alertas>"
   # Quem vira Administrador na primeira entrada: az ad user show --id <e-mail> --query id -o tsv
   first_administrator_object_id = "<id da pessoa>"

   # Arquivos no SharePoint (veja SharePoint, na Referência).
   sharepoint_site_url = "https://<tenant>.sharepoint.com/sites/<site>"
   # sharepoint_library     = "<biblioteca>"
   # sharepoint_root_folder = "Sistemas/FiscoProjects"

   # E-mails pela caixa da empresa (veja o passo 7); sem ela, apague a linha até o script do Exchange criar uma.
   email_mailbox = "<caixa>@<domínio>"

   # Termos de aceite pela Autentique: a linha entra no passo 5, depois dos segredos no Key Vault.
   # autentique = { webhook = true }

   # O teste de disponibilidade e os alertas ligam no passo 9, depois da publicação; antes, avisariam que o sistema
   # está fora do ar.
   alerts_enabled = false
   ```

4. **Aplique o Terraform**, conferindo o plano antes. A primeira vez pede os papéis do Entra ID de "Quem instala":

   ```bash
   terraform -chdir=infra/terraform init -backend-config=backend.hcl
   terraform -chdir=infra/terraform plan -out=instalacao.tfplan
   terraform -chdir=infra/terraform apply instalacao.tfplan
   ```

   Depois do `apply`, apague o `infra/terraform/instalacao.tfplan`: ele guarda valores sensíveis da instalação.

5. **Guarde os segredos no Key Vault** (o nome sai em `terraform -chdir=infra/terraform output key_vault_name`), sem deixá-los no histórico do terminal: a chave do Syncfusion e, se os termos forem assinados pela Autentique, a chave da API e o segredo do aviso dela (veja Assinatura dos termos de aceite, na Referência, para gerá-los):

   ```powershell
   $chave = Read-Host 'Chave do Syncfusion'
   az keyvault secret set --vault-name <cofre> --name Syncfusion--LicenseKey --value $chave --output none

   # Só com a Autentique:
   $chave = Read-Host 'Chave da API da Autentique'
   az keyvault secret set --vault-name <cofre> --name Signature--Autentique--ApiKey --value $chave --output none
   $segredo = Read-Host 'Segredo do aviso da Autentique'
   az keyvault secret set --vault-name <cofre> --name Signature--Autentique--WebhookSecret --value $segredo --output none
   ```

   Depois, ponha `syncfusion_license_in_key_vault = true` e, com a Autentique, `autentique = { webhook = true }` no `local.auto.tfvars` e aplique de novo (passo 4).

6. **Dê o site do SharePoint ao aplicativo**, com uma conta de Administrador do SharePoint ou Administrador global. Na primeira vez no tenant, o Microsoft Graph PowerShell pede o consentimento para `Sites.FullControl.All`, que só um Administrador de aplicativos ou global dá:

   ```powershell
   Install-Module Microsoft.Graph.Authentication, Microsoft.Graph.Sites -Scope CurrentUser   # uma vez por computador
   ./infra/scripts/Grant-SharePointSite.ps1
   ```

   Um administrador da empresa sem o Terraform da instalação roda `./infra/scripts/Grant-SharePointSite.ps1 -ClientId <id do aplicativo> -SiteUrl <site>`.

7. **Prepare a caixa dos e-mails**, com uma conta de Administrador do Exchange ou Administrador global:

   ```powershell
   Install-Module ExchangeOnlineManagement -Scope CurrentUser   # uma vez por computador
   ./infra/scripts/Grant-ExchangeMailbox.ps1                     # -DisableWAM se a entrada do Windows falhar
   ```

   Um administrador da empresa sem o Terraform da instalação passa `-ClientId <id do aplicativo> -Mailbox <caixa>`. Quando o script cria a caixa, ele bloqueia a entrada da conta dela, o que pede também o papel de Administrador de usuários; sem ele, o script avisa, e a entrada é bloqueada à mão no centro de administração do Microsoft 365 (Usuários ativos, Bloquear entrada). Se a caixa foi criada pelo script, ponha o endereço dela em `email_mailbox` e aplique de novo (passo 4). A permissão do Exchange leva de 30 minutos a 2 horas para valer; até lá, os e-mails esperam na fila.

8. **Publique a versão**: `./infra/scripts/Publish-Version.ps1`. O script aplica as migrações do banco com a sua conta do Azure CLI, abrindo o firewall do banco só para o seu endereço e só enquanto elas rodam, publica o `app.zip` e espera a aplicação responder em `/health`. A sua rede precisa sair pela porta 1433 até o banco; se ela sai por outro endereço público, passe-o em `-ClientAddress`.

9. **Ligue os alertas**: apague a linha `alerts_enabled = false` do `local.auto.tfvars` (o padrão é ligado) e aplique de novo (passo 4).

10. **Libere a entrada das pessoas**: no Microsoft Entra, em Aplicativos empresariais → FiscoProjects (<código> prod) → Usuários e grupos, atribua o primeiro administrador e as pessoas que vão usar o sistema (veja Entra ID, na Referência).

11. **Configure o FiscoProjects**: o primeiro administrador entra no endereço da instalação e, na Administração, revê:
    - Configurações gerais: nome, logos, cor principal, fuso, pasta dos projetos, horário do resumo diário e prazos de guarda das notificações e da auditoria; na seção Microsoft 365, "Enviar teste".
    - Calendário e feriados (os feriados locais e os pontos facultativos da empresa), Prazos dos chamados, Pastas padrão, Categorias de links, Fornecedores e Clientes.
    - Usuários: quem é gerente de projeto ou líder técnico, depois que as pessoas entram pela primeira vez.

12. **Combine a rotina**: o backup dos dois arquivos privados, o teste de restauração a cada 3 meses ([roteiro de restauração](restauracao.md)) e quem recebe os alertas.

## Atualizar para uma versão nova

1. Leia as notas da versão no GitHub, principalmente o que muda na infraestrutura ou nas permissões.
2. Descompacte o pacote da versão nova numa pasta nova e copie para `infra/terraform/` dela o `local.auto.tfvars` e o `backend.hcl` da instalação.
3. Entre na assinatura (`az login` e `az account set`), inicie o Terraform e confira o plano. Se houver mudanças, revise e aplique, e apague o plano depois:

   ```bash
   terraform -chdir=infra/terraform init -backend-config=backend.hcl
   terraform -chdir=infra/terraform plan -out=instalacao.tfplan
   terraform -chdir=infra/terraform apply instalacao.tfplan
   ```

4. Publique: `./infra/scripts/Publish-Version.ps1`. A aplicação reinicia com a versão nova e fica alguns minutos fora do ar: prefira um horário sem uso.

Para voltar a uma versão anterior, rode o `Publish-Version.ps1` na pasta do pacote dela, com o `local.auto.tfvars` da instalação em `infra/terraform/`: as migrações só acrescentam, e a versão anterior roda no banco novo (ADR-0125). Não aplique o Terraform da versão anterior sem conferir o plano.

## Apagar uma instalação

Na pasta do pacote, com os arquivos da instalação e os papéis do Entra ID de "Quem instala" de volta (apagar as permissões do aplicativo pede o Administrador de aplicativos e o de função privilegiada):

```bash
terraform -chdir=infra/terraform init -backend-config=backend.hcl
terraform -chdir=infra/terraform destroy
```

O Key Vault fica recuperável pelo prazo da exclusão reversível (90 dias na produção), sem custo parado, e os backups mensais do banco continuam cobrados até vencerem. A conta do estado sobra e custa centavos por mês; apagado o último ambiente desta instalação, saia com `az group delete --name rg-fiscoprojects-<código>-tfstate`.

O `destroy` não alcança o que os scripts fizeram fora do Azure, que os administradores da empresa apagam: no Exchange Online, a atribuição do papel "Application Mail.Send", o escopo `FiscoProjects - caixa de envio` e a entidade de serviço do aplicativo (`Remove-ManagementRoleAssignment`, `Remove-ManagementScope` e `Remove-ServicePrincipal`) e a caixa, se foi criada pelo script; no SharePoint, a permissão do aplicativo no site, que deixa de valer com o aplicativo apagado, e os arquivos, que ficam no site.

## Instalação pelo repositório

A instalação de Marcelo é criada a partir do repositório e publicada pelo pipeline dele (ADR-0055, ADR-0115, ADR-0125). Os parâmetros dos ambientes ficam em `infra/terraform/environments/<ambiente>.tfvars` e `<ambiente>.backend.hcl`, e os valores pessoais no `infra/terraform/local.auto.tfvars`, fora do Git.

### Criar ou atualizar a infraestrutura

Com as ferramentas de "Antes de começar" (ADR-0053, ADR-0114):

1. Entre na conta e escolha a assinatura: `az login` e `az account set --subscription <assinatura>`.
2. Uma vez por instalação, prepare o estado: `./infra/scripts/Prepare-TerraformState.ps1 -Installation <código>`. Os parâmetros do ambiente ficam em `infra/terraform/environments/<ambiente>.tfvars` e `<ambiente>.backend.hcl`, como os do `dev`.
3. Ponha os valores pessoais em `infra/terraform/local.auto.tfvars`:

   ```hcl
   operator_object_ids = ["<identificador seu no Entra ID: az ad signed-in-user show --query id -o tsv>"]
   alert_email         = "<e-mail que recebe os alertas>"
   # Opcional: quem vira Administrador na primeira entrada pelo Entra (ADR-0006).
   first_administrator_object_id = "<oid da pessoa: az ad user show --id <e-mail> --query id -o tsv>"
   # Opcional: o site do SharePoint dos arquivos (ADR-0120); veja SharePoint, na Referência.
   sharepoint_site_url = "https://<tenant>.sharepoint.com/sites/<site>"
   # Opcional: a caixa compartilhada dos e-mails (ADR-0123); veja Exchange Online, na Referência.
   email_mailbox = "<caixa>@<domínio do tenant>"
   ```

4. Aplique, conferindo o plano antes:

   ```bash
   terraform -chdir=infra/terraform init -backend-config=environments/<ambiente>.backend.hcl
   terraform -chdir=infra/terraform plan -var-file=environments/<ambiente>.tfvars -out=<ambiente>.tfplan
   terraform -chdir=infra/terraform apply <ambiente>.tfplan
   ```

5. Crie a chave do Syncfusion no Key Vault, como no passo 5 de "Instalar", ponha `syncfusion_license_in_key_vault = true` no `.tfvars` do ambiente e aplique de novo.
6. A Autentique: o `dev.tfvars` já a liga, com o sandbox e o aviso (ADR-0130). Antes de aplicá-lo, crie os dois segredos dela no Key Vault, como no passo 5 de "Instalar", com o aviso cadastrado no endereço do `dev`, e confira que a versão publicada no `dev` já conhece a Autentique: a anterior não sobe com `Signature:Provider` = `Autentique`. Num ambiente sem a Autentique, tire o bloco `autentique` do `.tfvars`.

Para apagar um ambiente: `terraform -chdir=infra/terraform destroy -var-file=environments/<ambiente>.tfvars`. O Key Vault fica recuperável pelo prazo da exclusão reversível (7 dias no `dev`).

### CI/CD

- Identidade federada (OIDC) entre o GitHub Actions e a assinatura Azure da instalação (ADR-0055), criada pelo Terraform quando o `.tfvars` do ambiente tem `github_repository`. Ela vale só para o environment do GitHub de mesmo nome do ambiente, publica no grupo de recursos, trava e lê o estado e lê o Entra ID, sem dar papéis nem mudar o Entra ID (ADR-0115, ADR-0116).
- `github_repository` vai como o GitHub põe no subject dos tokens, com os números do dono e do repositório: `<dono>@<id do dono>/<repositório>@<id do repositório>`. Os números saem de `gh api repos/<dono>/<repositório> --jq '"\(.owner.id) \(.id)"'`. Assim, outro repositório com o mesmo nome não ganha a identidade (ADR-0116).
- Depois do `apply`, prepare o environment do GitHub com `./infra/scripts/Set-GitHubEnvironment.ps1 -Environment <ambiente>`, com o GitHub CLI na conta dona do repositório e o Terraform iniciado no estado do ambiente. O script grava as variáveis `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` e `OPERATOR_OBJECT_IDS`, nenhuma segredo. No `dev`, o environment aceita só a `main`; no `prod`, só as tags das versões (`v*`), e cada publicação espera a aprovação de quem rodou o script (ADR-0125).
- Os logs do GitHub Actions são públicos e mostram as variáveis do workflow: nada pessoal vai nelas. O e-mail dos alertas fica só no `local.auto.tfvars` de quem opera; o pipeline o lê do grupo de ações da instalação e o mascara (ADR-0116). Por isso, as instalações das empresas não são publicadas por este pipeline enquanto o repositório for público (ADR-0127).
- A cada merge na `main`, o workflow `deploy-dev` monta o pacote, publica no App Service, espera o `/health`, carrega os dados de demonstração que faltam (no ambiente em `Development`) e roda o `terraform plan`. Uma diferença na infraestrutura aparece como aviso no resumo da execução: aplique o plano à mão, como em "Criar ou atualizar a infraestrutura" (ADR-0115). Para publicar de novo sem merge, rode o workflow pela aba Actions.
- Versões (ADR-0127): marque a versão na `main` e publique-a no GitHub, por exemplo com `gh release create v1.0.0 --target main --generate-notes`. O workflow `release` monta o pacote `fiscoprojects-v1.0.0.zip` e o anexa à versão, para as instalações das empresas.
- Produção (ADR-0125): o workflow `deploy-prod` só roda à mão. Para publicar uma versão:
  1. Publique a versão no GitHub, como acima.
  2. Em Actions, abra `deploy-prod`, escolha a tag em "Use workflow from" e rode. Rodado fora de uma tag de versão, ele para.
  3. Aprove a publicação no environment `prod`.

  O workflow confere a infraestrutura e para se ela estiver diferente do código: aplique o plano, como em "Criar ou atualizar a infraestrutura", e rode de novo. Depois monta o pacote, aplica as migrações e publica, esperando o `/health`. As migrações rodam pelo comando `migrate` do pacote, com a identidade do pipeline da produção, que o Terraform põe no grupo dos administradores do banco porque a aplicação não é administradora ali (`app_is_database_admin = false`). O mesmo comando cria no banco o usuário da identidade gerenciada da aplicação e dá a ele só a leitura e a gravação dos dados. Para voltar a uma versão anterior, rode o workflow na tag dela: as migrações só acrescentam.
- `main` protegida por uma regra do GitHub (ruleset), recebendo código só por PR com verificações aprovadas, sem exceções e sem poder ser apagada ou reescrita (ADR-0055). Em repositório público a regra é gratuita; em repositório privado, exige GitHub Pro ou superior.
- Dependabot com alertas e correções de segurança (ADR-0060).

## Referência

O que precisa ser configurado fora do código em cada instalação, com o ADR que o decidiu. Esta lista é critério de aceite das mudanças de setup no OpenSpec.

### Entra ID

- O aplicativo do FiscoProjects, de um único tenant (ADR-0003, ADR-0004), é criado pelo Terraform (ADR-0053), pronto para a entrada (ADR-0117): o endereço de retorno `https://<endereço>/signin-oidc`, as declarações opcionais `acct` e `email`, o consentimento do administrador só para `openid`, `profile` e `email` e a confiança na identidade gerenciada do App Service, a única credencial dele, sem segredo nem certificado. O `apply` pede uma conta que possa criar aplicativos e dar consentimento no tenant.
- A atribuição obrigatória vem ligada (`entra_assignment_required`, ADR-0005): no Microsoft Entra, em Aplicativos empresariais → o aplicativo → Usuários e grupos, atribua cada pessoa (ou grupo, que exige Entra ID P1/P2). Administradores globais entram sem atribuição; teste a recusa com uma pessoa comum. Quem não foi atribuído vê a recusa do Entra, que o FiscoProjects explica quando ela volta para ele.
- Contas de convidado são recusadas na entrada (ADR-0007), inclusive a conta Microsoft pessoal que criou o tenant; teste com usuários do próprio tenant (`nome@<tenant>.onmicrosoft.com`).
- A entrada pelo Entra só funciona numa instalação no Azure, com a identidade gerenciada; ela é ligada pelo Terraform (`entra_sign_in`, ligado por padrão), que passa o tenant e o aplicativo à aplicação (`EntraSignIn:*`).
- A busca de pessoas lê o diretório pelo Microsoft Graph (ADR-0005, ADR-0119): o Terraform dá ao aplicativo a permissão de aplicativo `User.Read.All`, com o consentimento do administrador, e a aplicação pede o token com a mesma prova da identidade gerenciada, sem segredo. O `apply` pede uma conta que possa dar consentimento a permissões de aplicativo (Administrador global ou Administrador de função privilegiada). A busca traz só membros ativos do tenant, sem convidados nem contas desativadas, inclusive quem ainda não foi atribuído ao aplicativo: essa pessoa pode ser incluída num projeto, mas só entra depois de atribuída. Até o `apply`, o Graph recusa a busca, a falha vai para o log e a busca segue com quem já entrou.
- Não conceder Mail.Send no Entra ID (ADR-0036).
- Opcional: personalizar a tela de login da Microsoft com a marca da empresa (company branding; exige Entra ID P1/P2 ou Office 365) (ADR-0009).

### SharePoint

- Definir o local dos arquivos: o site, a biblioteca e a pasta raiz, que pode ser uma área já existente (ADR-0030). Os três vão no `local.auto.tfvars` numa instalação pelo pacote; na instalação pelo repositório, o endereço do site vai no `local.auto.tfvars` (`sharepoint_site_url`, porque ele traz o nome do tenant), e a biblioteca e a pasta raiz, no `.tfvars` do ambiente (`sharepoint_library`, a padrão do site quando vazia, e `sharepoint_root_folder`, uma pasta dentro da biblioteca, como `Sistemas/FiscoProjects`, quando o aplicativo não deve tocar o resto da biblioteca; vazia, a raiz é a própria biblioteca). Com o site, o Terraform liga o armazenamento do SharePoint (`FileStorage:Provider` = `SharePoint`), e a aplicação cria a pasta raiz na primeira vez (ADR-0120). Dentro dela, os logos ficam na pasta `FiscoProjects`, e cada projeto ganha a pasta `{código} - {nome}` na pasta dos projetos, que o administrador escolhe depois em Configurações gerais, seção Arquivos, como `Projetos` ou `Clientes/Projetos` (vazia, na raiz). A pasta do projeto acompanha o código e o nome dele (ADR-0121).
- O Terraform dá ao aplicativo a permissão de aplicativo `Sites.Selected`, com o consentimento do administrador: sozinha, ela não alcança site nenhum. Depois do `apply`, o `./infra/scripts/Grant-SharePointSite.ps1` dá o site ao aplicativo com o papel de escrita, que lê, cria e exclui arquivos e pastas, sem mexer em permissões (ADR-0030, ADR-0041), rodado por uma conta que possa administrar todos os sites (Administrador global ou do SharePoint).
- Os arquivos removidos pelo FiscoProjects vão para a lixeira do site, como qualquer exclusão no SharePoint.
- Restringir a área de projetos a quem realmente precisa, porque quem acessa direto no SharePoint vê os arquivos de todos os projetos e chamados ali, inclusive termos de aceite (ADR-0030, ADR-0045).

### Exchange Online

- Os e-mails saem pelo Microsoft Graph a partir de uma caixa compartilhada (por exemplo, projetos@empresa.com.br), com o aplicativo podendo enviar só por ela. Nada de Mail.Send no Entra ID: o papel de envio vem do Exchange, num escopo só com a caixa (ADR-0036, ADR-0123).
- Depois do `apply` que criou o aplicativo do Entra, o `./infra/scripts/Grant-ExchangeMailbox.ps1` prepara a caixa, rodado por uma conta de Administrador do Exchange ou Administrador global. Se a empresa já tem uma caixa para isso, como um projetos@ que a equipe lê, ponha o endereço dela em `email_mailbox` antes: o script a usa como está, seja compartilhada ou de usuário, sem mudar nada nela (grupos e listas de distribuição não servem); as respostas aos e-mails do sistema chegam nela. Sem caixa, o script cria uma compartilhada, com a entrada da conta bloqueada (assim ela não aparece na busca de pessoas); num tenant novo, libera uma vez a personalização da organização no Exchange (`Enable-OrganizationCustomization`, sem volta, e o Exchange pode levar até 1 hora para aceitar o escopo, que o script espera); registra o aplicativo no Exchange, cria o escopo `FiscoProjects - caixa de envio` só com a caixa e dá o papel "Application Mail.Send" nele; e confere com `Test-ServicePrincipalAuthorization` que o aplicativo envia pela caixa e não pela de quem roda o script. O endereço sai do `email_mailbox` do `local.auto.tfvars` ou de `-Mailbox`; sem os dois, é `fiscoprojects@` o domínio padrão do tenant. Rodar de novo não muda nada.
- Com a caixa em `email_mailbox`, o Terraform liga o envio pelo Graph (`Email:Provider` = `Graph`, `Email:Mailbox`). A permissão do Exchange leva de 30 minutos a 2 horas para valer; até lá, os e-mails falham e voltam para a fila. Depois, use "Enviar teste" na seção Microsoft 365 das Configurações gerais.
- No ambiente de demonstração, os e-mails para o domínio das pessoas fictícias ficam na pasta local do App Service (`Email:Graph:LocalDomains`, no `.tfvars` do ambiente), e só os das pessoas do tenant saem; a aplicação não sobe em produção com domínios locais (ADR-0123).

### Assinatura dos termos de aceite

- Os termos de aceite são assinados pela Autentique, com a conta da empresa (ADR-0129, ADR-0130). Em 30/10/2026, o plano gratuito tinha 10 documentos por mês pela API, 10 chamadas por minuto e arquivos de até 5 MB, e o Profissional, cerca de US$ 19 por mês, com 60 chamadas por minuto e arquivos de até 20 MB; confira os valores atuais. Sem a Autentique, a gestão só anexa os termos assinados recebidos por fora.
- A chave da API é gerada no painel da Autentique, em Chaves de API (`https://painel.autentique.com.br/perfil/api`). Ela e o segredo do aviso ficam no Key Vault, como `Signature--Autentique--ApiKey` e `Signature--Autentique--WebhookSecret`, criados à mão (passo 5 de "Instalar"), nunca pelo Terraform nem nos arquivos da instalação. Com `autentique` no `local.auto.tfvars`, o Terraform liga o provedor na aplicação (`Signature:Provider` = `Autentique`) com as referências aos dois segredos.
- O aviso (webhook) é cadastrado no painel de desenvolvedor da Autentique, em formato JSON, com o endereço `https://<endereço da instalação>/signature-notices` (o `app_url` do `terraform output`) e os eventos `signature.accepted`, `signature.rejected` e `document.finished`; o segredo que o painel mostra vai para o Key Vault. Com o aviso (`webhook = true`), a situação de cada pedido chega na hora, e a conferência em segundo plano roda a cada hora, só como garantia; sem ele, a conferência roda a cada 15 minutos (`Signature:CheckMinutes`). Cada leitura de documento conta no plano da Autentique.
- O sandbox (`sandbox = true` no bloco `autentique`) cria documentos de teste, sem custo e sem validade, que a Autentique apaga em poucos dias; a aplicação não sobe em produção com ele.
- Cada signatário recebe o link por e-mail da Autentique; o FiscoProjects não manda e-mail a eles. O documento também fica na conta da Autentique, e a exclusão de um projeto pede a exclusão dos documentos dele lá (ADR-0111).
- No ambiente de demonstração, os pedidos com todos os signatários nos domínios fictícios ficam no provedor local, com o link de cada um na gaveta do termo (`Signature:LocalDomains`, no `.tfvars` do ambiente), e só os outros vão à Autentique; a aplicação não sobe em produção com eles (ADR-0130).

### Azure

- Toda a infraestrutura criada pelo Terraform, a partir de `infra/terraform/` e dos parâmetros da instalação (ADR-0053).
- Conta de armazenamento para o estado do Terraform, criada pelo `Prepare-TerraformState.ps1`, com versionamento e acesso restrito (ADR-0053).
- Região Brazil South (ADR-0052).
- Produção: App Service Linux B1 com Always On e Azure SQL provisionado (S0, ou Basic para começar) (ADR-0048, ADR-0052, [custos estimados](custos-estimados.md)).
- Desenvolvimento e demonstração (opcional): App Service B1 e Azure SQL Basic, sem backups mensais; a oferta gratuita do Azure SQL acabaria em poucos dias com os processos em segundo plano (ADR-0052, ADR-0114). Uma assinatura de avaliação pode não ter cota de App Service na Brazil South: aí `app_location` leva a aplicação e o banco para uma região com cota, só nesse ambiente, que tem só dados fictícios (ADR-0114).
- Identidade gerenciada do App Service com acesso ao Azure SQL e ao Key Vault (ADR-0052).
- Key Vault com a chave de licença do Syncfusion e, com a Autentique, a chave e o segredo do aviso dela, com exclusão reversível e proteção contra exclusão definitiva (ADR-0051, ADR-0052, ADR-0059). O aplicativo no Entra não tem certificado: ele confia na identidade gerenciada (ADR-0117).
- Só HTTPS; domínio próprio opcional com certificado gerenciado (ADR-0052).
- Application Insights, teste de disponibilidade no `/health` e alertas por e-mail para o administrador: sistema fora do ar, aumento de erros, fila de e-mails parada (`fiscoprojects.emails.oldest_waiting` acima de 15 minutos) e banco perto do limite (ADR-0057, ADR-0112). A aplicação envia a telemetria só com a conexão em `APPLICATIONINSIGHTS_CONNECTION_STRING`, posta pelo Terraform nas configurações do App Service (ADR-0114); `Monitoring:TracesPerSecond` muda a amostragem (padrão 5 traces por segundo).
- Backups do Azure SQL: restauração a qualquer ponto em 14 dias (Basic: 7), um backup mensal guardado por 12 meses e redundância por zona dentro da Brazil South (parâmetro da instalação) (ADR-0059).
- Banco só com Entra ID: o administrador do servidor é o grupo "FiscoProjects <instalação> <ambiente> - administradores do banco", com quem opera, e a aplicação entra pela identidade gerenciada, sem senha (ADR-0114). Na produção, a aplicação só lê e grava os dados; as migrações vêm da publicação, pelo `Publish-Version.ps1` ou pelo `deploy-prod` (ADR-0125, ADR-0127).
- O banco aceita conexões só dos serviços do Azure; a publicação à mão abre o firewall para o endereço de quem roda só durante as migrações (ADR-0127).
- Roteiro de restauração em [restauracao.md](restauracao.md), testado a cada 3 meses, com o registro de cada teste na própria página (ADR-0059).

### Licenças de terceiros

- Syncfusion: chave de licença válida do titular da instalação (comunitária, se se enquadrar nos critérios, ou paga), configurada fora do repositório (ADR-0051). A aplicação a lê de `Syncfusion:LicenseKey`, vinda do Key Vault em produção, e a entrega à Linha do tempo pelo `GET /api/client-settings`, só com sessão; sem ela, a Linha do tempo mostra o aviso de avaliação do Syncfusion (ADR-0087).
- IBM Plex Sans e IBM Plex Mono: SIL Open Font License, distribuída junto com os arquivos das fontes (ADR-0064).

### Segurança

- Recomendado em produção: cifrar o conjunto de chaves do Data Protection dos acessos com um certificado (ADR-0033). As chaves ficam na tabela `credentials.data_protection_keys`, nunca apagadas; o certificado é dado por `Credentials:KeyCertificate:Path` e `Credentials:KeyCertificate:Password` (um `.pfx`, com a senha vinda do Key Vault) ou por `Credentials:KeyCertificate:Thumbprint` (o certificado instalado na máquina). Configurado e não encontrado, a aplicação não sobe. Guarde o certificado com o backup: sem ele, as chaves cifradas não abrem e as senhas dos acessos se perdem. Sem certificado, o sistema funciona e as chaves ficam sem cifra.
- Certificados e chaves criados direto no Key Vault, nunca pelo Terraform (ADR-0053).
- Os arquivos privados da instalação (`local.auto.tfvars` e `backend.hcl`) não têm segredos, mas trazem o nome do tenant, e-mails e identificadores de pessoas: guarde-os só com quem opera (ADR-0127).

### Configuração da instalação no FiscoProjects

- Primeiro administrador, em `Users:FirstAdministrator`: `Provider` `entra` e, em `ExternalId`, o identificador (`oid`) da pessoa no Entra ID, posto pelo Terraform a partir do `first_administrator_object_id`. Ela recebe o papel Administrador na primeira entrada, ou na entrada seguinte se a instalação estiver sem administrador ativo; depois disso, os papéis são trocados na tela Usuários (ADR-0006).
- Nome, logo e cor principal exibidos; a cor principal pinta as ações, o item ativo da barra lateral e o painel da página de entrada, e só é aceita se passar nas verificações de contraste descritas no design system (ADR-0003, ADR-0009, ADR-0050, ADR-0063, ADR-0067).
- Fuso da instalação, padrão America/Sao_Paulo (ADR-0013).
- Tempo de sessão sem uso, padrão 8 horas (ADR-0009, ADR-0054).
- Local dos arquivos no SharePoint (ADR-0030, ADR-0120), com o armazenamento escolhido em `FileStorage:Provider` (`SharePoint`) e o site, a biblioteca e a pasta raiz em `FileStorage:SharePoint:*`, postos pelo Terraform. O armazenamento do SharePoint usa o aplicativo do Entra e precisa da entrada pelo Entra ligada. A implementação local (`Local`) não é aceita em produção: a aplicação não sobe com ela (ADR-0079).
- O envio dos e-mails, em `Email:Provider` (`Graph`), e o endereço da caixa de envio, em `Email:Mailbox`, postos pelo Terraform a partir do `email_mailbox`, e o endereço das telas, em `Email:SiteUrl`, base dos links de cada e-mail (ADR-0036, ADR-0123). O envio pelo Graph usa o aplicativo do Entra e precisa da entrada pelo Entra ligada. O conteúdo dos e-mails da fila é apagado depois de `Email:KeepDays` dias (30), ficando a data, o tipo e o status (ADR-0058). A implementação local (`Email:Provider` = `Local`) não é aceita em produção: a aplicação não sobe com ela. Depois de configurar, use "Enviar teste" na seção Microsoft 365 das Configurações gerais.
- O provedor de assinatura dos termos de aceite, em `Signature:Provider` (`Autentique`), com a chave, o segredo do aviso e o sandbox em `Signature:Autentique:*`, postos pelo Terraform a partir do bloco `autentique` (ADR-0129, ADR-0130). Sem provedor, os termos só são anexados. A implementação local (`Local`) não é aceita em produção: a aplicação não sobe com ela.
- Tamanho máximo de cada documento enviado, em `Documents:MaxMegabytes`, padrão 25 MB (ADR-0031). As pastas padrão dos projetos novos ficam em "Pastas padrão" na Administração; a migração cria as seis do MVP (ADR-0031, ADR-0093). O mesmo limite vale para os termos de aceite em PDF; os termos para assinar ficam na pasta `{código} - {nome}/Pedidos de aceite` de cada projeto, e os assinados, na pasta de termos dos documentos (ADR-0035, ADR-0095).
- Horário do resumo diário de e-mails, padrão 07:30 no fuso da instalação, em "Horário do resumo diário" das Configurações gerais (ADR-0037).
- Prazo de guarda das notificações do sino, padrão 90 dias (de 1 a 365), em "Guardar notificações por" das Configurações gerais (ADR-0038).
- Prazo de retenção da auditoria, padrão 5 anos, a validar com a assessoria jurídica da empresa (ADR-0058).

## Desenvolvimento

- Usar um tenant Microsoft próprio, nunca o da empresa; sem tenant licenciado, usar as implementações locais de e-mail e arquivos (ADR-0002, ADR-0004).
- Dados de demonstração: com a API rodando, `npm run demo:data` em `web/` (ou `npm run demo:data -- <endereço da API>`) carrega pessoas, clientes, projetos e cronogramas fictícios pela própria API. Só funciona com a entrada local ligada, portanto nunca em produção (ADR-0078, ADR-0089).
- E-mails locais: `Email:Provider` = `Local` e a pasta em `Email:Local:Folder` (no `appsettings.Development.json`, `App_Data/emails`, dentro da pasta da API e fora do Git); cada e-mail vira um `.eml`, que abre no programa de e-mail (ADR-0036).
- Assinatura local dos termos: `Signature:Provider` = `Local` e a pasta em `Signature:Local:Folder` (no `appsettings.Development.json`, `App_Data/signatures`); o link de cada signatário aparece na gaveta do termo e abre a página local de assinatura (ADR-0129). Para testar a Autentique na máquina, use o sandbox, com a chave nos segredos do usuário (`dotnet user-secrets set "Signature:Autentique:ApiKey" "<chave>" --project src/FiscoProjects.Api`), `Signature:Provider` = `Autentique` e `Signature:Autentique:Sandbox` = `true`; o aviso só chega a um endereço público, e a conferência em segundo plano traz a situação (ADR-0130).
- Arquivos locais: `FileStorage:Provider` = `Local` e a pasta em `FileStorage:Local:RootPath` (no `appsettings.Development.json`, `App_Data/files`, dentro da pasta da API e fora do Git). Apagar a pasta zera os arquivos de demonstração (ADR-0079).
- SDK do .NET 10 e Node na LTS mais recente (Node 26 a partir de 28/10/2026) (ADR-0060).
- Docker para o SQL Server dos testes de integração (ADR-0048, ADR-0056).
- Licença comunitária do Syncfusion em nome de Marcelo (ADR-0051), nos segredos do usuário: `dotnet user-secrets set "Syncfusion:LicenseKey" "<chave>" --project src/FiscoProjects.Api` (ADR-0087).
- Terraform CLI e Azure CLI instalados (ADR-0053).
