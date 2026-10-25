---
status: aceita
date: 2026-10-25
decision-makers: Marcelo Ribeiro
---

# ADR-0114: Como o Terraform monta uma instalação no Azure

Complementa o ADR-0053 (Terraform), o ADR-0052 (App Service), o ADR-0048 (Azure SQL), o ADR-0059 (backups) e o ADR-0112 (monitoramento), que decidiram o quê; este decide como o código do Terraform os monta, e substitui duas regras: a do banco no ambiente de desenvolvimento e demonstração (ADR-0048) e a da conexão do Application Insights vinda do Key Vault (ADR-0112).

## Contexto e problema

Com a assinatura do Azure de Marcelo, a Fundação-6 cria a infraestrutura. Faltava decidir como o banco aceita a aplicação sem senha, onde ficam os valores pessoais de cada instalação, como o ambiente de desenvolvimento e demonstração roda antes das integrações com o Microsoft 365 e como apagá-lo sem deixar custos. Ao detalhar o banco, apareceu um problema na regra do ADR-0048: a oferta gratuita do Azure SQL dá 100 mil segundos de vCore por mês, e os processos em segundo plano (a fila de e-mails a cada 30 segundos, os prazos, o resumo diário) não deixam o banco pausar; os segundos acabam em dois ou três dias, e o banco fica parado até o mês seguinte.

## Fatores de decisão

- Nenhuma senha ou segredo no Terraform nem no repositório (ADR-0053).
- Uma instalação reproduzível de um comando, com os parâmetros num `.tfvars`.
- Ambiente de desenvolvimento e demonstração utilizável o mês inteiro, com custo pequeno e que some quando ele é apagado.
- Ambiente no Azure antes das integrações com o Microsoft 365 (M365-1 a M365-4).

## Opções consideradas

Para o banco do ambiente de desenvolvimento e demonstração:

1. Oferta gratuita serverless, como no ADR-0048.
2. Plano Basic (5 DTUs, 2 GB), o mesmo que a produção pode usar no começo.
3. Serverless pago.

Para o acesso da aplicação ao banco:

1. Usuário e senha do SQL no Key Vault.
2. Só Entra ID: o administrador do servidor é um grupo do Entra ID, e a aplicação entra com a identidade gerenciada.

## Resultado da decisão

Banco do ambiente de desenvolvimento e demonstração: opção 2, o Basic. Cerca de US$ 6 por mês, sempre ligado; a oferta gratuita só serve para testes curtos, e o serverless pago custaria mais que a produção (ADR-0048). Acesso ao banco: opção 2, só Entra ID.

- **Estrutura**: `infra/terraform`, com os módulos `app`, `database`, `key-vault`, `monitoring`, `alerts` e `entra`, e os parâmetros de cada ambiente em `environments/<ambiente>.tfvars` e `<ambiente>.backend.hcl`. Os padrões das variáveis são os da produção (S0, 14 dias de restauração, um backup mensal por 12 meses, redundância por zona, `Production`). Os nomes saem do código da instalação e do ambiente (`app-fiscoprojects-<instalação>-<ambiente>`).
- **Valores pessoais fora do repositório**: quem opera a instalação (os identificadores no Entra ID) e o e-mail dos alertas vão num `local.auto.tfvars`, ignorado pelo Git, ou em variáveis `TF_VAR_*` do pipeline.
- **Estado**: numa conta de armazenamento por instalação, criada pelo `infra/scripts/Prepare-TerraformState.ps1`, com versionamento, exclusão reversível, sem acesso público e sem chaves compartilhadas; o Terraform entra nela pelo Entra ID.
- **Banco sem senha**: o servidor aceita só o Entra ID, com um grupo de administradores do banco (dono e membros: quem opera a instalação). A aplicação entra com a identidade gerenciada, pela conexão `Authentication=Active Directory Managed Identity`. Só o Azure alcança o servidor; quem precisar entrar de fora inclui o próprio endereço e o tira depois.
- **Ambiente de desenvolvimento e demonstração**: roda com `ASPNETCORE_ENVIRONMENT=Development` até as integrações com o Microsoft 365. Vale a entrada local, com as pessoas de demonstração, e os arquivos e e-mails ficam na pasta persistente do App Service (`/home/data`). A identidade da aplicação entra no grupo de administradores do banco, para a aplicação aplicar as migrações ao subir, como na máquina. Na produção, a identidade não é administradora, e as migrações ficam com o pipeline (ADR-0055).
- **Região do ambiente de desenvolvimento**: a assinatura de avaliação de Marcelo não tem cota de App Service na Brazil South, nem para o B1 nem para o gratuito, e tem na West US 3. A variável `app_location` leva a aplicação e o banco para outra região, juntos por causa da latência das consultas, só no ambiente de desenvolvimento, que tem só dados fictícios; o resto fica na Brazil South. A produção continua na Brazil South (ADR-0052), e o desenvolvimento volta para lá quando houver cota.
- **Apagar sem custos**: o ambiente de desenvolvimento não guarda backups mensais, que continuariam cobrados depois de o banco ser apagado. `terraform destroy` apaga tudo, menos o cofre, que fica recuperável por 7 dias e não tem custo parado, e a conta do estado, que custa centavos e sai pelo portal ou pelo `az group delete`.
- **Segredos**: criados à mão no Key Vault (ADR-0053), como a chave do Syncfusion (`Syncfusion--LicenseKey`). A aplicação a lê por referência ao Key Vault, ligada no `.tfvars` (`syncfusion_license_in_key_vault`) depois de criada.
- **Application Insights**: a conexão vai direto nas configurações do App Service, no lugar do Key Vault (ADR-0112). Ela não é credencial: só deixa a telemetria entrar, e já está no estado do Terraform junto com o recurso. Guardá-la no Key Vault obrigaria o Terraform a criar um segredo. O espaço de logs tem um limite diário de 1 GB.
- **Alertas**: um grupo de ações com o e-mail do administrador. O teste de disponibilidade no `/health` alimenta o alerta de sistema fora do ar; os outros três são requisições com falha acima de um limite em 15 minutos, fila de e-mails parada (o mais antigo esperando há mais de 15 minutos, numa consulta a cada 15 minutos) e banco acima de 80% do tamanho. Cada execução do teste é cobrada, por isso o desenvolvimento roda de 15 em 15 minutos, de um lugar só. Os alertas ficam desligados enquanto a aplicação não está publicada.
- **Aplicativo no Entra ID**: o registro, de um único tenant, e o principal de serviço; a entrada, as permissões do Microsoft Graph e a credencial chegam com as integrações (M365-1 a M365-4).
- **Verificação**: o PR roda `terraform fmt`, `init` sem o estado e `validate` com o Terraform fixado (`scripts/ci/infra.sh`); o `plan` com o estado da instalação chega com a identidade do pipeline (Fundação-7).

### Consequências

- Boa, porque nenhuma senha existe: o banco e o Key Vault aceitam só identidades do Entra ID.
- Boa, porque o ambiente de demonstração fica no ar o mês inteiro por poucos dólares e desaparece com um comando.
- Ruim, porque o ambiente de desenvolvimento roda como `Development` até as integrações, com a entrada local num endereço público, só com dados fictícios.
- Ruim, porque quem opera precisa estar no Entra ID da instalação, e uma conta pessoal da Microsoft entra como convidada.
- Ruim, porque, enquanto a assinatura não tiver cota na Brazil South, o ambiente de desenvolvimento fica nos Estados Unidos, com as telas um pouco mais lentas no Brasil.
- Neutra, porque mudar a produção para o Basic, o desenvolvimento de volta para a oferta gratuita ou para a Brazil South é mudar o `.tfvars`.
