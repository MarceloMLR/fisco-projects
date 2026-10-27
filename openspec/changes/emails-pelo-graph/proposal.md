## Why

Os e-mails do FiscoProjects ficam numa fila no banco desde a Avisos-1, mas só a implementação local existe: nada sai para ninguém. O ADR-0036 decidiu o envio pelo Microsoft Graph a partir de uma caixa compartilhada, com a permissão do aplicativo restrita a ela, e a M365-4 do roadmap liga esse envio, a última integração com o Microsoft 365 da v1.

## What Changes

- ADR-0123: como a mensagem vai ao Graph (JSON com HTML), as falhas e o limite de chamadas pela fila, a configuração com a caixa fora do Git, o script do Exchange e os e-mails da demonstração na pasta local.
- `Notifications`: o `GraphEmailSender`, escolhido por `Email:Provider` = `Graph`, envia pelo `sendMail` da caixa de `Email:Mailbox` com o token do aplicativo (`IGraphTokens` do `Users.Contracts`); recusa e limite viram falha da tentativa, com o código do Graph e sem o destinatário. Fora da produção, os domínios de `Email:Graph:LocalDomains` ficam na pasta local.
- Configuração conferida ao subir: o Graph pede a caixa de envio; os domínios locais pedem a pasta local e não valem em produção.
- Terraform: a variável `email_mailbox` (no `local.auto.tfvars`) liga o envio pelo Graph na aplicação; o ambiente `dev` mantém os e-mails da demonstração na pasta.
- `infra/scripts/Grant-ExchangeMailbox.ps1`: cria a caixa compartilhada, libera a personalização do Exchange num tenant novo, registra o aplicativo e dá o papel "Application Mail.Send" só na caixa, conferindo no fim. Nada de Mail.Send no Entra ID.
- Publicação: a conferência da infraestrutura lê a caixa da configuração da aplicação no ar e a mascara nos logs.
- `docs/instalacao.md`: o passo do Exchange.

## Capabilities

### New Capabilities

### Modified Capabilities
- `avisos`: o envio pelo Microsoft Graph, a configuração conferida e os e-mails da demonstração na pasta local.
- `infraestrutura`: a caixa de envio pelo Terraform e o script do Exchange.
- `publicacao`: a caixa de envio fora dos logs.

## Non-goals

- Enviar como a pessoa que fez a ação, ou de outra caixa por módulo.
- Ler e-mails da caixa (respostas que viram mensagens de chamado ficam para depois).
- Anexos nos e-mails.
- Mudar as telas: a caixa de envio e o teste continuam nas Configurações gerais.

## Impact

- `Notifications` (infraestrutura e opções), `Users.Contracts` (só o uso do `IGraphTokens`), testes do módulo e da API, `FiscoProjects.Testing` (o Graph simulado recebe os envios).
- `infra/terraform` (variável, configuração da aplicação, `environments/dev.tfvars`), `infra/scripts`, `scripts/cd/plan.sh` e os testes da infraestrutura e da publicação.
- `docs/adr`, `docs/instalacao.md`, `docs/glossario.md`, `CLAUDE.md` e `docs/roadmap.md`.
- ADRs: 0004, 0036, 0056, 0116, 0119, 0120.
