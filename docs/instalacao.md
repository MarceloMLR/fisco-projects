# Requisitos de instalação

O que precisa ser configurado fora do código em cada instalação do FiscoProjects. Esta lista é o ponto de partida do guia de instalação e vira critério de aceite das mudanças de setup no OpenSpec. Cada item cita o ADR que o decidiu (`docs/adr/`).

Os comandos exatos (Exchange Online, SharePoint, Terraform) são confirmados na documentação atual durante a implementação e entram aqui, junto com os scripts de `infra/scripts`.

## Entra ID

- Registrar o aplicativo do FiscoProjects como aplicativo de um único tenant (ADR-0003, ADR-0004), com credencial por certificado guardado no Key Vault (ADR-0052); o registro é criado pelo Terraform (ADR-0053).
- Recomendado: ligar a atribuição obrigatória no aplicativo empresarial, por pessoa ou por grupo; grupo exige Entra ID P1/P2 (ADR-0005).
- Conceder a permissão do Graph para leitura básica de usuários do diretório, usada na inclusão de pessoas em projetos (ADR-0005; permissão exata a confirmar).
- Não conceder Mail.Send no Entra ID (ADR-0036).
- Opcional: personalizar a tela de login da Microsoft com a marca da empresa (company branding; exige Entra ID P1/P2 ou Office 365) (ADR-0009).

## SharePoint

- Definir o local dos arquivos: site, biblioteca e pasta raiz, que pode ser uma área já existente (ADR-0030).
- Conceder ao aplicativo Sites.Selected com papel de escrita nesse site, limitado à biblioteca quando possível (ADR-0030); confirmar que o papel permite excluir os arquivos criados pelo sistema (ADR-0041). Script em `infra/scripts` (ADR-0053).
- Restringir a área de projetos a quem realmente precisa, porque quem acessa direto no SharePoint vê os arquivos de todos os projetos e chamados ali, inclusive termos de aceite (ADR-0030, ADR-0045).

## Exchange Online (e-mail)

- Criar a caixa compartilhada de envio (por exemplo, projetos@empresa.com.br) (ADR-0036).
- Registrar o aplicativo como service principal no Exchange Online, criar um escopo de gerenciamento só com essa caixa e atribuir o papel "Application Mail.Send" restrito ao escopo (ADR-0036). Script em `infra/scripts` (ADR-0053).
- Testar com Test-ServicePrincipalAuthorization (ADR-0036).

## Azure

- Toda a infraestrutura criada pelo Terraform, a partir de `infra/terraform/` e de um arquivo `.tfvars` da instalação (ADR-0053).
- Conta de armazenamento para o estado do Terraform, criada pelo script de preparação, com versionamento e acesso restrito (ADR-0053).
- Região Brazil South (ADR-0052).
- Produção: App Service Linux B1 com Always On e Azure SQL provisionado (S0, ou Basic para começar) (ADR-0048, ADR-0052, [custos estimados](custos-estimados.md)).
- Desenvolvimento e demonstração (opcional): App Service B1 e Azure SQL Basic, sem backups mensais; a oferta gratuita do Azure SQL acabaria em poucos dias com os processos em segundo plano (ADR-0052, ADR-0114). Uma assinatura de avaliação pode não ter cota de App Service na Brazil South: aí `app_location` leva a aplicação e o banco para uma região com cota, só nesse ambiente, que tem só dados fictícios (ADR-0114).
- Identidade gerenciada do App Service com acesso ao Azure SQL e ao Key Vault (ADR-0052).
- Key Vault com o certificado do aplicativo no Entra e a chave de licença do Syncfusion, com exclusão reversível e proteção contra exclusão definitiva (ADR-0051, ADR-0052, ADR-0059).
- Só HTTPS; domínio próprio opcional com certificado gerenciado (ADR-0052).
- Application Insights, teste de disponibilidade no `/health` e alertas por e-mail para o administrador: sistema fora do ar, aumento de erros, fila de e-mails parada (`fiscoprojects.emails.oldest_waiting` acima de 15 minutos) e banco perto do limite (ADR-0057, ADR-0112). A aplicação envia a telemetria só com a conexão em `APPLICATIONINSIGHTS_CONNECTION_STRING`, posta pelo Terraform nas configurações do App Service (ADR-0114); `Monitoring:TracesPerSecond` muda a amostragem (padrão 5 traces por segundo).
- Backups do Azure SQL: restauração a qualquer ponto em 14 dias (Basic: 7), um backup mensal guardado por 12 meses e redundância por zona dentro da Brazil South (parâmetro da instalação) (ADR-0059).
- Banco só com Entra ID: o administrador do servidor é o grupo "FiscoProjects <instalação> <ambiente> - administradores do banco", e a aplicação entra pela identidade gerenciada, sem senha (ADR-0114).
- Roteiro de restauração em [restauracao.md](restauracao.md), testado a cada 3 meses, com o registro de cada teste na própria página (ADR-0059).

### Criar ou atualizar uma instalação

Com o Azure CLI, o Terraform 1.16 (no Windows, `winget install Hashicorp.Terraform --version 1.16.5`) e o PowerShell 7 (ADR-0053, ADR-0114):

1. Entre na conta e escolha a assinatura: `az login` e `az account set --subscription <assinatura>`.
2. Uma vez por instalação, prepare o estado: `./infra/scripts/Prepare-TerraformState.ps1 -Installation <código>` (de 2 a 6 letras minúsculas ou números). Os parâmetros do ambiente ficam em `infra/terraform/environments/<ambiente>.tfvars` e `<ambiente>.backend.hcl`, como os do `dev`.
3. Ponha os valores pessoais em `infra/terraform/local.auto.tfvars`, que fica fora do Git:

   ```hcl
   operator_object_ids = ["<identificador seu no Entra ID: az ad signed-in-user show --query id -o tsv>"]
   alert_email         = "<e-mail que recebe os alertas>"
   ```

4. Aplique, conferindo o plano antes:

   ```bash
   terraform -chdir=infra/terraform init -backend-config=environments/<ambiente>.backend.hcl
   terraform -chdir=infra/terraform plan -var-file=environments/<ambiente>.tfvars -out=<ambiente>.tfplan
   terraform -chdir=infra/terraform apply <ambiente>.tfplan
   ```

5. Crie os segredos à mão no Key Vault (o nome sai no `key_vault_name` do `terraform output`), sem deixar a chave no histórico do terminal:

   ```powershell
   $chave = Read-Host 'Chave do Syncfusion'
   az keyvault secret set --vault-name <cofre> --name Syncfusion--LicenseKey --value $chave --output none
   ```

   Depois, ponha `syncfusion_license_in_key_vault = true` no `.tfvars` e aplique de novo.

### Apagar um ambiente

`terraform -chdir=infra/terraform destroy -var-file=environments/<ambiente>.tfvars` apaga o ambiente inteiro. O Key Vault fica recuperável pelo prazo da exclusão reversível (7 dias no `dev`), sem custo parado. A conta do estado sobra e custa centavos por mês; apagada a última instalação da assinatura, saia com `az group delete --name rg-fiscoprojects-<código>-tfstate`. Na produção, os backups mensais continuam cobrados até vencerem.

## CI/CD

- Identidade federada (OIDC) entre o GitHub Actions e a assinatura Azure da instalação (ADR-0055), criada pelo Terraform quando o `.tfvars` do ambiente tem `github_repository`. Ela vale só para o environment do GitHub de mesmo nome do ambiente, publica no grupo de recursos, trava e lê o estado e lê o diretório do Entra ID, sem dar papéis nem mudar o Entra ID (ADR-0115).
- Depois do `apply`, prepare o environment do GitHub com `./infra/scripts/Set-GitHubEnvironment.ps1 -Environment <ambiente>`, com o GitHub CLI na conta dona do repositório. O script aceita só a `main` no environment e grava as variáveis `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `OPERATOR_OBJECT_IDS` e `ALERT_EMAIL`; nenhuma é segredo.
- A cada merge na `main`, o workflow `deploy-dev` monta o pacote, publica no App Service, espera o `/health`, carrega os dados de demonstração que faltam (no ambiente em `Development`) e roda o `terraform plan`. Uma diferença na infraestrutura aparece como aviso no resumo da execução: aplique o plano à mão, como em "Criar ou atualizar uma instalação" (ADR-0115). Para publicar de novo sem merge, rode o workflow pela aba Actions.
- `main` protegida por uma regra do GitHub (ruleset), recebendo código só por PR com verificações aprovadas, sem exceções e sem poder ser apagada ou reescrita (ADR-0055). Em repositório público a regra é gratuita; em repositório privado, exige GitHub Pro ou superior.
- Dependabot com alertas e correções de segurança (ADR-0060).

## Licenças de terceiros

- Syncfusion: chave de licença válida do titular da instalação (comunitária, se se enquadrar nos critérios, ou paga), configurada fora do repositório (ADR-0051). A aplicação a lê de `Syncfusion:LicenseKey`, vinda do Key Vault em produção, e a entrega à Linha do tempo pelo `GET /api/client-settings`, só com sessão; sem ela, a Linha do tempo mostra o aviso de avaliação do Syncfusion (ADR-0087).
- IBM Plex Sans e IBM Plex Mono: SIL Open Font License, distribuída junto com os arquivos das fontes (ADR-0064).

## Segurança

- Recomendado em produção: cifrar o conjunto de chaves do Data Protection dos acessos com um certificado (ADR-0033). As chaves ficam na tabela `credentials.data_protection_keys`, nunca apagadas; o certificado é dado por `Credentials:KeyCertificate:Path` e `Credentials:KeyCertificate:Password` (um `.pfx`, com a senha vinda do Key Vault) ou por `Credentials:KeyCertificate:Thumbprint` (o certificado instalado na máquina). Configurado e não encontrado, a aplicação não sobe. Guarde o certificado com o backup: sem ele, as chaves cifradas não abrem e as senhas dos acessos se perdem. Sem certificado, o sistema funciona e as chaves ficam sem cifra.
- Certificados e chaves criados direto no Key Vault, nunca pelo Terraform (ADR-0053).

## Configuração da instalação no FiscoProjects

- Primeiro administrador, em `Users:FirstAdministrator`: `Provider` `entra` e, em `ExternalId`, o identificador (`oid`) da pessoa no Entra ID. Ela recebe o papel Administrador na primeira entrada, ou na entrada seguinte se a instalação estiver sem administrador ativo; depois disso, os papéis são trocados na tela Usuários (ADR-0006).
- Nome, logo e cor principal exibidos; a cor principal pinta as ações, o item ativo da barra lateral e o painel da página de entrada, e só é aceita se passar nas verificações de contraste descritas no design system (ADR-0003, ADR-0009, ADR-0050, ADR-0063, ADR-0067).
- Fuso da instalação, padrão America/Sao_Paulo (ADR-0013).
- Tempo de sessão sem uso, padrão 8 horas (ADR-0009, ADR-0054).
- Local dos arquivos no SharePoint (ADR-0030), com o armazenamento escolhido em `FileStorage:Provider`. A implementação local (`Local`) não é aceita em produção: a aplicação não sobe com ela (ADR-0079).
- Endereço da caixa de envio, em `Email:Mailbox`, e o endereço das telas, em `Email:SiteUrl`, base dos links de cada e-mail (ADR-0036). O conteúdo dos e-mails da fila é apagado depois de `Email:KeepDays` dias (30), ficando a data, o tipo e o status (ADR-0058). A implementação local (`Email:Provider` = `Local`) não é aceita em produção: a aplicação não sobe com ela. Depois de configurar, use "Enviar teste" na seção Microsoft 365 das Configurações gerais.
- Tamanho máximo de cada documento enviado, em `Documents:MaxMegabytes`, padrão 25 MB (ADR-0031). As pastas padrão dos projetos novos ficam em "Pastas padrão" na Administração; a migração cria as seis do MVP (ADR-0031, ADR-0093). O mesmo limite vale para os termos de aceite em PDF; os termos para assinar ficam na pasta `{código} - {nome}/Pedidos de aceite` de cada projeto, e os assinados, na pasta de termos dos documentos (ADR-0035, ADR-0095).
- Horário do resumo diário de e-mails, padrão 07:30 no fuso da instalação, em "Horário do resumo diário" das Configurações gerais (ADR-0037).
- Prazo de guarda das notificações do sino, padrão 90 dias (de 1 a 365), em "Guardar notificações por" das Configurações gerais (ADR-0038).
- Prazo de retenção da auditoria, padrão 5 anos, a validar com a assessoria jurídica da empresa (ADR-0058).

## Desenvolvimento

- Usar um tenant Microsoft próprio, nunca o da empresa; sem tenant licenciado, usar as implementações locais de e-mail e arquivos (ADR-0002, ADR-0004).
- Dados de demonstração: com a API rodando, `npm run demo:data` em `web/` (ou `npm run demo:data -- <endereço da API>`) carrega pessoas, clientes, projetos e cronogramas fictícios pela própria API. Só funciona com a entrada local ligada, portanto nunca em produção (ADR-0078, ADR-0089).
- E-mails locais: `Email:Provider` = `Local` e a pasta em `Email:Local:Folder` (no `appsettings.Development.json`, `App_Data/emails`, dentro da pasta da API e fora do Git); cada e-mail vira um `.eml`, que abre no programa de e-mail (ADR-0036).
- Arquivos locais: `FileStorage:Provider` = `Local` e a pasta em `FileStorage:Local:RootPath` (no `appsettings.Development.json`, `App_Data/files`, dentro da pasta da API e fora do Git). Apagar a pasta zera os arquivos de demonstração (ADR-0079).
- SDK do .NET 10 e Node na LTS mais recente (Node 26 a partir de 28/10/2026) (ADR-0060).
- Docker para o SQL Server dos testes de integração (ADR-0048, ADR-0056).
- Licença comunitária do Syncfusion em nome de Marcelo (ADR-0051), nos segredos do usuário: `dotnet user-secrets set "Syncfusion:LicenseKey" "<chave>" --project src/FiscoProjects.Api` (ADR-0087).
- Terraform CLI e Azure CLI instalados (ADR-0053).
