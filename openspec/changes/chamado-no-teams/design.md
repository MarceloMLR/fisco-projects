## Context

A página do Teams (`TeamsPage`, `/teams`, fora da estrutura e sem sessão) recebe o caminho do item no `item` (o `subEntityId` do aviso, como `/tickets/104`) e o tema no `theme`, e só mostra a marca e "Abrir no FiscoProjects" (ADR-0149). O manifesto do Teams (`TeamsTexts`) já declara o `webApplicationInfo` com o cliente do aplicativo da instalação e o recurso `api://{host}/{cliente}`, o host do `Email:SiteUrl`; o aplicativo do Entra (módulo `entra` do Terraform) ainda não expõe essa API: só tem o retorno da entrada, as declarações do token de identidade e as permissões do Graph.

A API reconhece a pessoa só pelo cookie da sessão (`SessionAuthentication`, ADR-0054), que o grupo `/api` exige pela política padrão (`MapApiGroup().RequireAuthorization()`); a entrada pelo Entra (`EntraSignIn`, esquema `entra`) só abre esse cookie, com o usuário achado pelo vínculo `entra` do `oid` (`SignInWithEntra`). Dentro do quadro do Teams o cookie `SameSite=Lax` não vai. O chamado inteiro sai do `GetTicket`, com as regras de quem vê (`IProjectAccess`, ADR-0044). Ver proposal.md (Why) e ADR-0152.

## Goals / Non-Goals

**Goals:**

- A página do Teams lê o chamado do aviso como a pessoa, pela entrada única do Teams, sem sessão e sem nova entrada.
- O token do Teams vale só nas leituras sob `/api/teams`, e o cookie continua valendo só no resto.
- As regras de quem vê o chamado e de quem é a pessoa (convidado, vínculo, ativo) não se repetem.

**Non-Goals:**

- Trocar o token do Teams por um token do Graph (on-behalf-of) ou guardar qualquer token no servidor.
- Rotas de alteração com o token do Teams.

## Decisions

### 1. O esquema `teams` no `Users`

O `Users` ganha o `TeamsSignIn` (Infrastructure), um esquema JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) chamado `teams`, registrado pelo `SessionAuthentication` ao lado do cookie e da entrada pelo Entra:

- **Com a entrada pelo Entra ligada:** a autoridade e o emissor do `EntraSignInOptions.Authority` (v2, o tenant da instalação), a audiência no cliente (`ClientId`, como vem nos tokens v2), `MapInboundClaims = false`, `SaveToken = false` e `IncludeErrorDetails = false`. No `OnTokenValidated`, o escopo (`scp`) tem de ter `access_as_user` e o cliente (`azp`) tem de ser o Teams do computador e do celular (`1fec8e78-bce4-4aaf-ab1b-5451cc387264`) ou o da web (`5e3ce6c0-2b1f-4285-8d4b-75ee78787346`), os identificadores públicos da Microsoft (`TeamsSignIn.TeamsClients`); depois o caso de uso `SignInFromTeams` (Application) acha a pessoa.
- **Com a entrada pelo Entra desligada:** o esquema `teams` é um manipulador que nunca autentica, então as rotas do Teams respondem 401 sem ler token nenhum, e a política continua existindo.

O `SignInFromTeams` lê o `EntraIdentity` do token, recusa o convidado pela mesma regra da entrada (`acct` = 1 ou `idp`, que passa a ficar num lugar só, no `EntraIdentity`), acha o usuário pelo vínculo `entra` do `oid` (`IUserStore`) e recusa quem não tem vínculo e o desativado, sem criar usuário nem gravar recusa na auditoria (ler não é auditado). O principal passa a ser o mesmo da sessão (`UserSession.For(userId)`), para as rotas lerem a pessoa pelo `UserSession.UserIdOf` de sempre. A cada requisição o token é conferido de novo, e a pessoa também.

*Alternativa:* trocar o token por um cookie particionado na primeira leitura. Recusada no ADR-0152: o cookie teria de ser `SameSite=None`, e navegadores e o WebView do Teams podem recusá-lo.

### 2. O grupo `/api/teams` no host

O `ApiSetup` ganha o `MapTeamsApiGroup()`: `/api/teams`, fora do `MapApiGroup` (cuja política padrão é a do cookie), com `RequireAuthorization(TeamsAccess.Policy)`. A política (`TeamsAccess.Policy`, no `Users.Contracts`) só aceita o esquema `teams` e pede a pessoa autenticada; o cookie não é lido. Como as outras rotas usam a política padrão, do cookie, o token do Teams não vale nelas. O grupo só recebe `GET`; o `CsrfHeaderCheck` não muda. Como o cookie, o token não é descrito no OpenAPI: as rotas do Teams aparecem com o 401 em Problem Details, e a tela manda o cabeçalho.

### 3. A rota do chamado no `Tickets`

`GET /api/teams/tickets/{number}` (`GetTeamsTicket`, nos `TicketEndpoints`, recebendo o grupo do Teams) monta o `TicketViewer` como as outras rotas (o usuário da sessão e o papel de administrador pelo `IUserAccess`), chama o `GetTicket` e transforma o resultado pelo `TeamsTicketView.Of` (Application), sem consulta nova:

- **A conversa:** a descrição (pelo solicitante, na abertura, com os anexos dela), as mensagens e os eventos, na ordem da conversa da página do chamado; as últimas `TeamsTicketView.RecentInteractions` (5) e `earlier`, quantas vieram antes. Os anexos vão pelo nome e pelo tamanho, sem id.
- **As pessoas:** só as do cabeçalho e das interações mostradas, com o id, o nome e o papel na conversa (`TicketAccess.RoleIn`, já nos participantes do `GetTicket`), sem e-mail; o `viewerId` para a página dizer "(você)".
- **Os prazos:** os mesmos números da página do chamado (`TicketDeadlineFigures`), com o estado de hoje.
- **Fora:** as permissões, quem acompanha, os chamados em fornecedores e as opções de troca.

A resposta sai com `Cache-Control: no-store`. Quem não vê, e o número que não existe, recebem 404 pelo `null` do `GetTicket`.

### 4. A página do Teams

A `TeamsPage` decide pelo caminho do item (`ticketNumberOf`, só `/tickets/{número}`, depois do `safeItemPath`):

- **Outro item ou sem item:** a página de hoje, sem carregar a biblioteca do Teams.
- **Um chamado:** o `teamsHost.ts`, o único arquivo que importa `@microsoft/teams-js` (a regra do lint reprova o import, também o dinâmico, em outro lugar), carrega a biblioteca sob demanda, chama `app.initialize()` com um limite de 3 segundos (fora do Teams, ninguém responde, e a página volta à de hoje sem pedir token; no Teams do celular a página não fica num quadro, então só a resposta do Teams diz onde ela está), segue o tema do Teams (`app.getContext` e `registerOnThemeChangeHandler`, além do `theme` do endereço) e entrega o `getAuthToken()`.
- **A leitura:** o `useTeamsTicket` (`teamsTicket.ts`) chama a rota por um cliente próprio (`teamsApi`, sem o cookie e sem o aviso de sessão encerrada, porque um 401 ali é o token recusado) com o cabeçalho `Authorization: Bearer` e guarda o chamado só no estado da página, fora do cache das consultas: o token é da página, e não de uma sessão, e o fim de uma sessão limpa esse cache (`SessionProvider`); um 401 pede o token de novo uma vez.
- **Os estados:** carregando; o chamado (`TeamsTicket.tsx`, desenhado como o canvas "Chamado no Teams"); "Você não vê este chamado" no 404 (`TeamsNoAccess`); e, no 401 repetido, no fim do limite ou na falha da biblioteca, a página de hoje, com a frase de que não foi possível mostrar o chamado quando a leitura falhou por outro erro.

O `TeamsTicket` reaproveita o que a página do chamado já tem: o `TicketStatusBadge`, o `PriorityTag`, o `TicketDue`, o `RoleTag`, o `PersonAvatar` e o `RichText`, e a linha de evento e a mensagem da `TicketConversation`, que passam a ser exportadas e a aceitar os anexos só pelo nome (sem o `AttachmentChip`, que abre e baixa). No computador, o cabeçalho com "Responder no FiscoProjects", a conversa e a coluna das pessoas, dos prazos e do chamado; até 600px, uma coluna, com o botão no pé. As datas pelo `useDates()`, no fuso da instalação, que vem da marca pública. Os textos em `teamsPage.ticket.*`.

### 5. O aplicativo do Entra no Terraform

O módulo `entra` recebe o `site_host` (o `local.host`, o mesmo do `Email:SiteUrl` e do manifesto) e passa a ter:

- `api { requested_access_token_version = 2 }`, com o escopo `access_as_user` (tipo administrador, ativo, com um id fixo no código, que não muda se o host mudar);
- o identificador `api://{site_host}/{client_id}`, pelo `azuread_application_identifier_uri`, porque o `client_id` só existe depois do aplicativo;
- os dois clientes do Teams pré-autorizados no escopo, pelo `azuread_application_pre_authorized`;
- a declaração `acct` também no `access_token`;
- os `ignore_changes` que o provedor pede para esses recursos separados, conferidos na documentação do `azuread` 3.10.0 na implementação.

Os tokens v2 também liberam o identificador com o host do App Service, que não é um domínio verificado do tenant (as restrições de identificadores isentam os aplicativos v2). A entrada continua pelo token de identidade, que não depende dessa versão. O `InfrastructureTests` confere o identificador, o escopo, a versão, os dois clientes, a declaração e as permissões do Graph de antes.

### 6. Testes

- **API:** o `FakeEntra` passa a emitir o token do Teams (`TeamsTokenAsync`, com a audiência, o emissor, o escopo, o cliente, o `acct`, a assinatura e a validade trocáveis), com as mesmas chaves do emissor simulado; os testes da rota e das recusas seguem os cenários das specs pelo nome. O `MonitoringTests` ganha o caso do token do Teams, aceito e recusado, fora dos registros.
- **Tela:** o `teamsHost` simulado nos testes da `TeamsPage` (dentro e fora do Teams, token, recusa, limite) e a biblioteca simulada nos testes do próprio `teamsHost`; o MSW confere o cabeçalho `Authorization`; o chamado simulado fica em `web/src/test/demoTeamsTicket.ts`.

## Risks / Trade-offs

- **[Um ataque de script na página do Teams lê o token]** → O token só lê os chamados que a pessoa vê, só nas rotas do Teams, pelo tempo de vida dele; a página monta o texto formatado nó por nó, sem HTML, e não guarda o token.
- **[Um tenant com a regra mais rígida dos identificadores sem isenção para v2]** → O `terraform apply` falha com a mensagem do Entra; o guia de instalação diz como dar a isenção ao aplicativo ou usar um domínio verificado no endereço do site.
- **[A biblioteca do Teams fora do ar ou lenta]** → O limite de 3 segundos e a página de hoje; nada do chamado aparece sem a leitura aceita.
- **[O `GetTicket` lê o chamado inteiro para mostrar 5 interações]** → É a mesma leitura da página do chamado, uma por abertura do aviso; uma leitura própria só se ela pesar.
- **[As regras do token num lugar diferente das da sessão]** → A pessoa sai do mesmo vínculo e da mesma regra do convidado da entrada pelo Entra, e o principal é o mesmo da sessão.

## Migration Plan

1. Implementar e publicar no `dev` pelo `deploy-dev`; sem o Terraform aplicado, o Teams não entrega o token e a página continua como hoje.
2. Com a aprovação de Marcelo, `terraform apply` no `dev` (módulo `entra`) e o teste no Teams do computador com um aviso de chamado.
3. Nas instalações das empresas, o `terraform apply` da versão nova, como em toda versão, antes ou depois do `Publish-Version.ps1`; o manifesto do Teams não muda, então não há pacote novo.
4. Para voltar atrás, a versão anterior da aplicação; a API exposta no Entra fica sem uso.
