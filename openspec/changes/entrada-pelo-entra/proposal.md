## Why

Com o tenant de Marcelo e o ambiente no Azure (Fundação-6 e Fundação-7), dá para ligar a entrada pelo Entra ID que os ADRs 0005, 0007, 0009 e 0054 descreveram. Até aqui só existe a entrada local da demonstração (ADR-0078). A M365-1 é a primeira das integrações com o Microsoft 365 e abre o caminho da busca de pessoas, dos arquivos e dos e-mails reais.

## What Changes

- Entrada pelo Entra ID no BFF: OpenID Connect com código de autorização e PKCE, pelo handler do ASP.NET Core, com a credencial do aplicativo pela identidade gerenciada do App Service, sem segredo nem certificado (ADR-0117, no lugar do certificado do ADR-0052).
- A página de entrada manda o e-mail à API e vai para a Microsoft com ele como dica; depois, a pessoa volta à tela pedida, ou ao Início quando outra pessoa entra no lugar de quem teve a sessão expirada.
- O usuário nasce no primeiro login com o vínculo `entra` e o identificador do objeto (`oid`), como na entrada local.
- Recusas com mensagem na página de entrada: convidado (sem criar usuário), sem atribuição no Entra e desativado.
- No ambiente de desenvolvimento e demonstração, as duas entradas convivem: a pessoa da demonstração entra direto, e os outros e-mails vão para a Microsoft.
- Terraform do aplicativo do Entra: endereço de retorno, declarações opcionais, consentimento para `openid`, `profile` e `email`, credencial federada da identidade gerenciada e atribuição obrigatória; a aplicação recebe o tenant e o cliente e lê o esquema HTTPS atrás do App Service.

## Non-goals

- Busca de pessoas pelo Graph (M365-2), arquivos no SharePoint (M365-3) e e-mails pelo Graph (M365-4).
- Sair também da conta Microsoft: o "Sair" continua encerrando só a sessão do FiscoProjects (ADR-0009).
- Entrada pelo Entra fora do Azure: sem identidade gerenciada, a máquina de desenvolvimento continua só com a entrada local.
- Convidados com acesso (ADR-0007) e outros provedores de identidade.
- Personalização da página da Microsoft com a marca da empresa (orientação no guia, ADR-0009).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `entrada-e-sessao`: a entrada pelo Entra ID, com a dica do e-mail, a volta à tela pedida e as recusas de convidado e sem atribuição; a entrada local passa a conviver com a do Entra.
- `infraestrutura`: o aplicativo do Entra pronto para a entrada, com a credencial federada da identidade gerenciada e a atribuição obrigatória.

## Impact

- `src/Modules/Users` (caso de uso da entrada pelo Entra, opções, rota que começa a entrada, opções da página), `src/FiscoProjects.Api/Session` (handler OpenID Connect e a prova pela identidade gerenciada) e os testes da API, com o Entra simulado no processo.
- `web/src/pages/SignInPage.tsx`, textos em `pt-BR.json`, contrato e tipos da API.
- `infra/terraform` (módulos `entra` e `app`, variáveis e `dev.tfvars`) e `InfrastructureTests`.
- Pacote novo `Microsoft.AspNetCore.Authentication.OpenIdConnect` (MIT) e `Azure.Identity` (MIT) explícito, que já vinha pelo SqlClient.
- ADR-0117; ADR-0052 com o certificado substituído; `docs/instalacao.md`, `CLAUDE.md`, glossário e roadmap.
- Tenant de Marcelo: o `apply` muda o aplicativo do Entra; as pessoas de teste são criadas e atribuídas por ele.
