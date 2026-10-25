# Design

## Context

A sessão é o cookie do BFF (`SessionAuthentication`, ADR-0054), aberta pela entrada local (`SignInLocally`) a partir do caso de uso comum `SignInWithIdentity`, que cria o usuário e o vínculo com o provedor (ADR-0009, ADR-0078). O aplicativo do Entra já existe no Terraform (módulo `entra`), sem entrada, sem permissões e sem credencial. A aplicação roda no App Service com identidade gerenciada, atrás de um proxy que termina o TLS.

## Goals / Non-Goals

**Goals:**

- Entrar pelo Entra com o mesmo caminho da entrada local a partir da identidade, a mesma sessão e o mesmo "Sair".
- Nenhum segredo, certificado ou token guardado, e o e-mail fora dos endereços e dos logs do FiscoProjects.
- Testar o fluxo inteiro sem rede: o Entra simulado no processo.

**Non-Goals:**

- Graph, SharePoint e e-mail (M365-2 a M365-4).
- Entrada pelo Entra fora do Azure.

## Decisions

- **Opções `EntraSignIn`** (Users, Application): `Enabled`, `Instance` (padrão `https://login.microsoftonline.com/`), `TenantId` e `ClientId`, conferidos ao subir quando ligada. O `GET /api/session/sign-in-options` passa a dizer `entraSignIn` além de `localSignIn`.
- **Começo** (`POST /api/session/entra`, aberta como as outras da sessão e com o `X-CSRF`): recebe o e-mail, a tela pedida e o usuário da sessão que expirou. A rota chama o desafio do esquema `entra` e transforma o redirecionamento do handler em `{ location }`, mantendo os cookies de correlação e de `nonce` que ele grava. O e-mail vai na dica (`login_hint`) só no endereço da Microsoft. A tela pedida só é aceita se for um caminho do próprio site (`/...`, sem `//` nem `/\`); senão vira `/`.
- **Handler** (host, `EntraAuthentication`): `AddOpenIdConnect("entra")` só com a entrada ligada; autoridade `{Instance}{TenantId}/v2.0`, código com PKCE, escopos `openid profile email`, `SaveTokens` desligado, `MapInboundClaims` desligado, PAR desligado, retorno em `/signin-oidc` (fora de `/api`, porque o Entra volta por um formulário sem o `X-CSRF`) e entrada no esquema do cookie.
- **Prova do aplicativo** (`IEntraClientAssertion`): no `OnAuthorizationCodeReceived`, a aplicação põe na troca do código o `client_assertion` do tipo `jwt-bearer`, com o token da identidade gerenciada para `api://AzureADTokenExchange` (`ManagedIdentityCredential`, do Azure.Identity, com cache até perto de vencer). Nos testes, uma prova falsa.
- **Identidade** (`SignInWithEntra`, Users, Application): recebe as declarações do token validado. Convidado (`acct` = 1 ou `idp` presente) → recusa `Guest`, sem usuário; sem `oid` → falha. Senão, `SignInWithIdentity` com `entra`, `oid`, `name` e `email` ou `preferred_username`; desativado → `Inactive`. No `OnTokenValidated`, a entrada aceita troca a identidade pelo `UserSession.For(id)` e escolhe a volta: a tela pedida, ou `/` quando o usuário da sessão expirada é outro.
- **Recusas**: o handler leva à página de entrada com `?refused=` (`guest`, `notAssigned`, `inactive`, `failed`). `notAssigned` sai do `OnAccessDenied` e do `OnRemoteFailure` quando a descrição do erro do Entra traz `AADSTS50105`; qualquer outra falha é `failed`. A página lê o parâmetro, mostra a mensagem e o tira do endereço. A recusa de convidado é um evento de segurança novo da auditoria (`SignInRefusal.Guest`); a de quem não tem atribuição não chega à aplicação como identidade, então não tem e-mail para registrar.
- **Entrada local junto**: com o Entra ligado, o `SignInLocally` devolve o e-mail fora do diretório sem registrar recusa, e a página segue para o Entra. Assim, o ambiente do Azure mantém a demonstração e testa o Entra.
- **Terraform**: no módulo `entra`, `web.redirect_uris` com `https://<host>/signin-oidc`, `optional_claims.id_token` com `acct` e `email`, `required_resource_access` do Graph com `openid`, `profile` e `email` e o `azuread_service_principal_delegated_permission_grant` desses três para todos, `app_role_assignment_required` pela variável `entra_assignment_required` (padrão `true`) e o `azuread_application_federated_identity_credential` com o emissor do tenant e o `principal_id` da identidade gerenciada. No módulo `app`, os `EntraSignIn__*` e o `ASPNETCORE_FORWARDEDHEADERS_ENABLED`, com o primeiro administrador do Entra pelo `first_administrator_object_id` do `local.auto.tfvars`, quando há.

## Risks / Trade-offs

- [O Entra pode mostrar a própria página de erro de quem não tem atribuição] → a página do FiscoProjects explica quando o erro volta; o guia de instalação diz como atribuir.
- [Transformar o redirecionamento do handler em JSON depende de ele responder com 302 e `Location`] → teste que chama a rota e confere o endereço, os cookies e a dica.
- [Sem `acct` configurado, o convidado não seria reconhecido] → a regra também olha o `idp`, e o teste cobre os dois.
- [Entrada local pública no ambiente do Azure] → já era assim (ADR-0114), só com dados fictícios; em produção ela não sobe.

## Migration Plan

1. Quem opera aplica o Terraform do `dev`: o aplicativo do Entra fica pronto, e a aplicação recebe o tenant e o cliente; sem o código novo, as configurações ficam sem efeito.
2. O merge publica o código; a entrada local continua, e a do Entra passa a valer para os e-mails fora da demonstração.
3. Marcelo cria as pessoas de teste no tenant, as atribui ao aplicativo empresarial e testa a entrada, o convidado e a falta de atribuição no endereço do `dev`.
