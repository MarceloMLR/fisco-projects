## Context

Os avisos passam pelo `DeliverNotices`, que aplica as regras de quem recebe e entrega a cada `INoticeChannel` (ADR-0038):

- o `EmailChannel` põe na fila o que é imediato (`EmailChannel.Immediate`) e guarda o resto para o resumo;
- o `BellChannel` grava a notificação do sino.

O e-mail usa a fila no banco com o `EmailDispatcher` (ADR-0036) e sai pelo Graph com o token do aplicativo (`IGraphTokens` do `Users.Contracts`, ADR-0123). O vínculo `entra` de cada pessoa guarda o id do objeto no Entra (ADR-0117). A preferência do resumo diário fica no `User` (`ReceivesDailySummary`), fora da auditoria.

O motivo está na proposta. Este documento cobre o como.

## Goals / Non-Goals

**Goals:**

- Um terceiro canal, sem mudar as regras de quem recebe.
- A escolha do canal de cada pessoa decidida num lugar só, usado pelo e-mail, pelo Teams e pelo perfil.
- Nenhuma permissão nova no Entra ID e nenhuma dependência nova no backend.

**Non-Goals:**

- Autenticar a pessoa dentro do Teams.
- Métricas e alertas próprios da fila do Teams; os da fila de e-mails continuam como estão.

## Decisions

### 1. Canal do Teams e regra do canal num lugar só

- **`NoticeRouting` (Application do `Notifications`):** decide, para cada pessoa, se o aviso na hora vai por e-mail, pelo Teams ou pelos dois. A decisão usa a escolha da pessoa (`ImmediateNotices`), os avisos pelo Teams ligados na instalação e se ela pode receber pelo Teams.
- **O `EmailChannel`** deixa de pôr na fila o e-mail de quem fica só com o Teams.
- **O `TeamsChannel`**, novo `INoticeChannel`, põe na fila do Teams os imediatos de quem tem o Teams. O que vai para o resumo nunca passa por ele.
- **O sino não muda.**

*Alternativa:* cada canal conferir a escolha por conta própria. Recusada, porque a regra ficaria em dois lugares e poderia deixar alguém sem aviso.

### 2. O que o aviso sabe da pessoa

- **`NoticeRecipient`** ganha a escolha do canal e o id do objeto no Entra, quando há.
- **A escolha** fica no próprio `Notifications`, dono dos canais; **o vínculo com o Entra** vem do contrato de pessoas (`IUserDirectory` do `Users.Contracts`).
- **Quem pode receber pelo Teams:**
  - com o envio pelo Graph, quem tem o vínculo `entra`;
  - com a implementação local, qualquer pessoa ativa, para o desenvolvimento e a demonstração.
- **O perfil** lê a escolha e se o Teams está disponível numa rota do `Notifications`, que usa a mesma regra do `NoticeRouting`. Assim o `Users` não depende do `Notifications`, que já depende dele.

### 3. Fila e envio

- **A fila:** `notifications.teams_notices`, com a pessoa, o tipo do aviso, os valores, o nome de quem fez, o caminho do item, o projeto, a situação, as tentativas e a próxima tentativa. É gravada na mesma transação da ação, como os e-mails.
- **O envio:** o `TeamsDispatcher`, em segundo plano a cada 30 segundos, reserva cada aviso pela versão da linha e tenta de novo como o `EmailDispatcher`.
- **A limpeza:** a limpeza de cada hora apaga os avisos enviados ou que falharam há mais de `Email:KeepDays` dias. Diferente do e-mail, não há o que manter.
- **A volta ao e-mail:** quando o aviso não chega de vez a quem escolheu só o Teams, o despachante monta o e-mail com o mesmo texto do `EmailChannel` (`NoticeEmails.ContentOf`) e o põe na fila de e-mails, na mesma gravação. Isso acontece quando o aplicativo não está instalado, quando o Teams recusa o envio ou quando acaba a última tentativa.

*Alternativa:* enviar na hora, sem fila. Recusada, porque a ação passaria a esperar o Teams e não haveria nova tentativa.

### 4. Envio pelo Graph

`ITeamsSender` tem duas implementações, escolhidas por `Teams:Provider`.

**`GraphTeamsSender`:** usa o `HttpClient` `graph-teams` e o token do `IGraphTokens`. Para cada aviso:

1. **Confere se o aplicativo está instalado para a pessoa**, com `GET /users/{id}/teamwork/installedApps?$expand=teamsApp&$filter=teamsApp/externalId eq '{id do manifesto}'`. A permissão vem do próprio aplicativo (`TeamsAppInstallation.Read.User`). Uma lista vazia é "não instalado".
   - O `teamsApp.id` da resposta é o id do aplicativo no catálogo da empresa, que o link para a página precisa. Ele fica guardado em memória.
2. **Envia** com `POST /users/{id}/teamwork/sendActivityNotification`:
   - o tipo de aviso do manifesto;
   - os parâmetros do texto (quem fez e o item);
   - a prévia com o código do projeto e o título;
   - o tópico em texto, com o link `https://teams.microsoft.com/l/entity/{id no catálogo}/home?context={"subEntityId":"<caminho>"}`.
3. **Classifica a resposta:**
   - 2xx: Enviado;
   - 429, 5xx e falha de rede: nova tentativa;
   - 400, 403 e 404: recusa de vez.

O erro guardado traz o código HTTP e o `error.code` do Graph, sem a pessoa.

**`LocalTeamsSender`:** grava um `.json` por aviso em `Teams:Local:Folder` (`App_Data/teams` no desenvolvimento), com quem recebe, o tipo, os textos e o link.

*Alternativas:*
- Pedir ao Graph o id do catálogo com `AppCatalog.Read.All`. Recusada, porque é permissão nova no Entra ID.
- Pedir ao administrador que digite o id. Recusada, porque é um passo a mais e fácil de errar.

### 5. O pacote do aplicativo

**O manifesto**, montado pelo servidor (`TeamsAppManifest`, com `System.Text.Json`), servido em `POST /api/admin/teams-app/package` (só do administrador; 409 `teams-unavailable` sem `Teams:Provider`). Ele traz:

- o id do aplicativo da instalação (`TeamsApp.ManifestId`, um GUID criado na primeira leitura e que não muda);
- a versão `1.0.{n}`;
- os nomes da instalação, cortados no limite do Teams;
- as descrições e os textos dos tipos de aviso, tirados dos arquivos de tradução do `Notifications`;
- a página pessoal `/teams?item={page.subPageId}&theme={app.theme}`;
- o domínio das telas;
- o `webApplicationInfo` com o cliente do Entra;
- as permissões do aplicativo `TeamsActivity.Send.User` e `TeamsAppInstallation.Read.User`;
- um tipo de aviso para cada `EmailChannel.Immediate`.

A versão do esquema do manifesto fica fixada no código, a estável mais recente na implementação, conferida na documentação.

**A versão:** sobe quando muda o conteúdo que vai no pacote: o manifesto, o logo ou a cor principal. A tela manda, no pedido do pacote, o que desenha nos ícones (o endereço do logo, com a versão dele, ou a cor principal). O servidor guarda o resumo (hash) do manifesto com isso e o número (`TeamsApp.PackageHash`, `TeamsApp.PackageVersion`), fora da auditoria.

**Os ícones e o `.zip`:** montados na tela, como a planilha da auditoria:

- o ícone colorido (192×192) é o logo da instalação sobre branco ou, sem logo, o selo na cor principal com o ícone do cronograma;
- o ícone de contorno (32×32) é o ícone do cronograma em branco sobre transparente;
- tudo é desenhado num `canvas` e compactado pelo `zip` de `web/src/format/xlsx.ts`, e o `saveFile` salva.

*Alternativa:* desenhar os ícones no servidor. Recusada, porque pediria uma biblioteca de imagens no backend, e as gratuitas para isso têm licença restrita ou dependência nativa.

### 6. A página no Teams

- **A rota:** `/teams`, fora da estrutura das telas e sem sessão, como a `/sign/:token`.
- **O que mostra:** a marca da instalação (`GET /api/installation`) e um link `target="_blank"` para o endereço da instalação mais o caminho do item. O caminho só vale se começar com uma única `/`, sem esquema, sem `\` e sem `//` (`safeItemPath`). Fora disso, o link vai para o Início.
- **O tema:** segue o do Teams pelo `theme`, claro para `default` e escuro para `dark` e `contrast`.
- **Sem a biblioteca do Teams:** a página não usa o `@microsoft/teams-js`, porque não precisa do contexto além do que vem no endereço.

### 7. Preferência e configurações

- **No `Notifications`:** a escolha de cada pessoa em `notifications.notice_preferences` (`Email`, `Teams`, `Both`; sem linha, `Both`), fora da auditoria, com `GET` e `PUT /api/profile/immediate-notices` (`channel` e `teamsAvailable`), só da pessoa na sessão. O resumo diário continua no `User`.
- **No `Notifications`:** o `TeamsApp`, uma linha só em `notifications.teams_app`, com "Avisar pelo Teams" (`NoticesOn`, falso de início, na auditoria), o id do manifesto e a versão do pacote. `GET` e `PUT /api/admin/teams-app` trazem e mudam a opção, só do administrador, com 400 em `noticesOn` quando ela é ligada sem `Teams:Provider`. Assim, tudo do Teams fica no módulo dos canais, e o `Administration` não muda.
- **`TeamsOptionsValidation`:** sem `Teams:Provider`, nada sai pelo Teams. A aplicação não sobe com `Graph` sem a entrada pelo Entra, nem com `Local` em produção.
- **O Terraform** põe `Teams__Provider = "Graph"` junto com a entrada pelo Entra (`entra_settings`). A instalação só manda avisos depois que o administrador liga "Avisar pelo Teams".

### 8. Dados pessoais e exclusão

- **Na anonimização,** o `NotificationsPersonalData` apaga os avisos do Teams para a pessoa e troca o nome dela pelo identificador nos avisos de outras pessoas.
- **Na exclusão do projeto,** o `NotificationProjectDeletion` apaga os avisos do Teams do projeto.

## Risks / Trade-offs

- **[A permissão do próprio aplicativo pode não valer com o token do aplicativo, como diz a documentação]** → O envio só sai depois do "Avisar pelo Teams". A conferência no `dev`, com o Teams da avaliação do Microsoft 365 de Marcelo, vem antes de ligar em qualquer empresa. Uma recusa vira e-mail, e ninguém perde o aviso.
- **[O Teams pode não abrir o `target="_blank"` no navegador]** → Conferir no `dev`. Se for preciso, carregar o `@microsoft/teams-js` (MIT) só na página `/teams` e usar `app.openLink`.
- **[O link para a página pede o id do catálogo, que só a consulta de instalação traz]** → Sem ele, o link usa o id do manifesto, que o Teams aceita para o aplicativo enviado pela própria pessoa.
- **[A empresa troca o logo e o ícone do Teams fica o antigo]** → A versão do pacote sobe, e a seção mostra a versão para quem baixa de novo.
- **[O aviso não aparece no celular]** → Limite do Teams para aplicativos próprios (ADR-0149). O e-mail continua para quem escolher Os dois.

## Migration Plan

- **O que as migrações acrescentam:**
  - a tabela da fila no `notifications`;
  - as escolhas do canal no `notifications`, vazias (Os dois para todos);
  - o `TeamsApp` no `notifications`, criado desligado na primeira leitura.
- **Na publicação:** nada muda, porque os avisos pelo Teams começam desligados.
- **Para voltar atrás:** desligar "Avisar pelo Teams". Os avisos na hora voltam a sair só por e-mail, qualquer que seja a escolha das pessoas.
