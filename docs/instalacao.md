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
- Desenvolvimento e demonstração (opcional): App Service B1 e Azure SQL na oferta gratuita serverless, com "pausar até o mês seguinte" ao atingir o limite (ADR-0048, ADR-0052).
- Identidade gerenciada do App Service com acesso ao Azure SQL e ao Key Vault (ADR-0052).
- Key Vault com o certificado do aplicativo no Entra e a chave de licença do Syncfusion, com exclusão reversível e proteção contra exclusão definitiva (ADR-0051, ADR-0052, ADR-0059).
- Só HTTPS; domínio próprio opcional com certificado gerenciado (ADR-0052).
- Application Insights, teste de disponibilidade e alertas por e-mail para o administrador (ADR-0057).
- Backups do Azure SQL: restauração a qualquer ponto em 14 dias (Basic: 7), um backup mensal guardado por 12 meses e redundância por zona dentro da Brazil South (parâmetro da instalação) (ADR-0059).
- Roteiro de restauração documentado e testado a cada 3 meses (ADR-0059).

## CI/CD

- Identidade federada (OIDC) entre o GitHub Actions e a assinatura Azure da instalação (ADR-0055).
- `main` protegida por uma regra do GitHub (ruleset), recebendo código só por PR com verificações aprovadas, sem exceções e sem poder ser apagada ou reescrita (ADR-0055). Em repositório público a regra é gratuita; em repositório privado, exige GitHub Pro ou superior.
- Dependabot com alertas e correções de segurança (ADR-0060).

## Licenças de terceiros

- Syncfusion: chave de licença válida do titular da instalação (comunitária, se se enquadrar nos critérios, ou paga), configurada fora do repositório (ADR-0051).
- IBM Plex Sans e IBM Plex Mono: SIL Open Font License, distribuída junto com os arquivos das fontes (ADR-0064).

## Segurança

- Recomendado em produção: cifrar o conjunto de chaves do Data Protection com um certificado (ou Key Vault) (ADR-0033).
- Certificados e chaves criados direto no Key Vault, nunca pelo Terraform (ADR-0053).

## Configuração da instalação no FiscoProjects

- Primeiro administrador, em `Users:FirstAdministrator`: `Provider` `entra` e, em `ExternalId`, o identificador (`oid`) da pessoa no Entra ID. Ela recebe o papel Administrador na primeira entrada, ou na entrada seguinte se a instalação estiver sem administrador ativo; depois disso, os papéis são trocados na tela Usuários (ADR-0006).
- Nome, logo e cor principal exibidos; a cor principal pinta as ações, o item ativo da barra lateral e o painel da página de entrada, e só é aceita se passar nas verificações de contraste descritas no design system (ADR-0003, ADR-0009, ADR-0050, ADR-0063, ADR-0067).
- Fuso da instalação, padrão America/Sao_Paulo (ADR-0013).
- Tempo de sessão sem uso, padrão 8 horas (ADR-0009, ADR-0054).
- Local dos arquivos no SharePoint (ADR-0030).
- Endereço da caixa de envio (ADR-0036).
- Horário do resumo diário de e-mails (ADR-0037).
- Prazo de guarda das notificações do sino, padrão 90 dias (ADR-0038).
- Prazo de retenção da auditoria, padrão 5 anos, a validar com a assessoria jurídica da empresa (ADR-0058).

## Desenvolvimento

- Usar um tenant Microsoft próprio, nunca o da empresa; sem tenant licenciado, usar as implementações locais de e-mail e arquivos (ADR-0002, ADR-0004).
- SDK do .NET 10 e Node na LTS mais recente (Node 26 a partir de 28/10/2026) (ADR-0060).
- Docker para o SQL Server dos testes de integração (ADR-0048, ADR-0056).
- Licença comunitária do Syncfusion em nome de Marcelo (ADR-0051).
- Terraform CLI e Azure CLI instalados (ADR-0053).
