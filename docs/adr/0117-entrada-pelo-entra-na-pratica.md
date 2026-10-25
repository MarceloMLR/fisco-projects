---
status: aceita
date: 2026-10-25
decision-makers: Marcelo Ribeiro
---

# ADR-0117: Como a entrada pelo Entra ID funciona, com a credencial pela identidade gerenciada

Complementa os ADRs 0005, 0007, 0009 e 0054, e substitui, no ADR-0052, o certificado do aplicativo no Entra guardado no Key Vault.

## Contexto e problema

Com o tenant de Marcelo, a M365-1 liga a entrada pelo Entra ID que os ADRs 0005, 0007, 0009 e 0054 descreveram. Faltava decidir como a aplicação prova ao Entra que é ela, com qual biblioteca, como o e-mail digitado chega à Microsoft, como reconhecer um convidado e alguém sem atribuição, e como a entrada pelo Entra convive com a entrada local do ambiente de desenvolvimento e demonstração (ADR-0078, ADR-0114).

O ADR-0052 previa um certificado do aplicativo guardado no Key Vault. Um certificado é criado e renovado à mão, e a chave privada precisa ficar em algum lugar.

## Fatores de decisão

- Nenhum segredo nem certificado para criar, guardar ou renovar.
- Tokens da Microsoft nunca no navegador (ADR-0054).
- O e-mail da pessoa fora dos endereços e dos logs do FiscoProjects.
- Convidados recusados mesmo sem a atribuição obrigatória (ADR-0007).
- Os testes sem o Microsoft 365 real (ADR-0056).

## Opções consideradas

1. A credencial do aplicativo pela identidade gerenciada do App Service (credencial federada), com o handler OpenID Connect do próprio ASP.NET Core.
2. Um certificado no Key Vault (ADR-0052), com a mesma biblioteca.
3. A biblioteca Microsoft.Identity.Web, com qualquer uma das credenciais.

## Resultado da decisão

Opção 1, com a credencial escolhida por Marcelo.

- **Credencial**: o aplicativo do Entra confia na identidade gerenciada do App Service (credencial federada, emissor do próprio tenant, público `api://AzureADTokenExchange`). Para trocar o código pelo token, a aplicação pede à identidade gerenciada um token com esse público e o manda como `client_assertion`. Não existe segredo nem certificado, e nada vence. As chamadas ao Graph das próximas changes (M365-2 a M365-4) usam a mesma prova. Fora do Azure não há identidade gerenciada, então a entrada pelo Entra só vale numa instalação no Azure; na máquina, continua a entrada local.
- **Protocolo**: OpenID Connect com código de autorização e PKCE, pelo handler `Microsoft.AspNetCore.Authentication.OpenIdConnect`, de licença MIT. Os tokens não são guardados (`SaveTokens` desligado): depois da entrada, a sessão é só o cookie do ADR-0054. O Microsoft.Identity.Web traria mais do que a entrada precisa, e o handler do ASP.NET Core já resolve a entrada e a prova pela identidade gerenciada em poucos eventos.
- **Começo da entrada**: a página de entrada envia o e-mail à API, que devolve o endereço da Microsoft com o e-mail como dica (`login_hint`, ADR-0009); o navegador vai para lá. O e-mail não passa por nenhum endereço do FiscoProjects, só pelo da Microsoft.
- **Identidade**: provedor `entra`, com o identificador imutável do objeto (`oid`) como identificador externo (ADR-0009). O nome vem do `name`; o e-mail, do `email` ou, sem ele, do `preferred_username`. O tenant é conferido pelo emissor do token, que só aceita o tenant da instalação.
- **Convidado**: recusado quando o token diz que a conta é convidada (a declaração opcional `acct` igual a 1) ou que veio de outro provedor (`idp`). Nenhum usuário é criado, e a recusa entra na auditoria.
- **Sem atribuição**: o aplicativo empresarial exige atribuição (variável do Terraform, ligada por padrão, ADR-0005). Quando o Entra devolve a recusa à aplicação (`AADSTS50105`), a página de entrada explica; quando o próprio Entra mostra o erro, a pessoa fica na página da Microsoft.
- **Volta**: depois de entrar, a pessoa volta à tela que pediu, só dentro do FiscoProjects, ou ao Início quando outra pessoa entra no lugar de quem teve a sessão expirada, como na entrada local.
- **Ambiente de desenvolvimento e demonstração no Azure**: as duas entradas ficam ligadas. O e-mail de uma pessoa da demonstração entra direto; qualquer outro vai para a Microsoft, sem registro de recusa. Em produção continua só o Entra: a aplicação não sobe com a entrada local ligada (ADR-0078).
- **Terraform**: o aplicativo ganha o endereço de retorno `/signin-oidc`, as declarações opcionais `acct` e `email`, o consentimento do administrador para `openid`, `profile` e `email`, a credencial federada da identidade gerenciada e a atribuição obrigatória. A aplicação recebe o tenant, o cliente e o `ASPNETCORE_FORWARDEDHEADERS_ENABLED`, para montar o endereço de retorno em HTTPS atrás do App Service. O primeiro administrador do Entra (ADR-0006) vem do `oid` de quem opera, no `local.auto.tfvars`.

### Consequências

- Boa, porque não há segredo nem certificado do aplicativo em lugar nenhum, nem data de vencimento.
- Boa, porque o e-mail digitado não aparece nos endereços nem nos logs do FiscoProjects.
- Ruim, porque a entrada pelo Entra não roda fora do Azure; numa máquina, só com a entrada local.
- Ruim, porque o Entra pode mostrar a própria página de erro para quem não tem atribuição, sem a mensagem do FiscoProjects.
- Neutra, porque os testes simulam o Entra (emissor, chaves e token) dentro do processo, sem rede.
