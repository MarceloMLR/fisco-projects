## Context

A fila de e-mails (`Notifications`, ADR-0036) envia em segundo plano pelo `IEmailSender`, com novas tentativas, e tem a implementação local (`LocalEmailSender`, `.eml`) e a vazia (`NoEmailSender`, sem envio configurado). O `Users.Contracts` entrega o token do aplicativo ao Graph (`IGraphTokens`, ADR-0119), já usado pelo `Files` no SharePoint (ADR-0120), e o `HttpClient` do Graph vai sempre para `MicrosoftGraph.Address`. O Terraform já liga o SharePoint a partir do `local.auto.tfvars` e o `plan.sh` mascara o endereço do site.

## Goals / Non-Goals

**Goals:**
- Enviar os e-mails da fila pela caixa compartilhada, sem permissão nova no Entra ID.
- Testar o envio de ponta a ponta no ambiente `dev` sem mandar nada para os endereços fictícios.

**Non-Goals:**
- Ler a caixa, anexos, outra caixa por módulo.

## Decisions

- **`GraphEmailSender` (Infrastructure do `Notifications`).** `POST users/{caixa}/sendMail` com `{ message: { subject, body: { contentType: "HTML", content }, toRecipients: [{ emailAddress: { address, name } }] }, saveToSentItems: true }`, pelo `HttpClient` nomeado `graph-mail` e o `IGraphTokens`. Resposta 202 é envio; qualquer outra vira `EmailSendException` com o código HTTP e o `error.code` do Graph, sem o destinatário, que a fila grava como erro da tentativa. Sem repetição dentro do envio: a fila já tenta de novo (1 min, 5 min...), o que cobre o 429 do Graph.
- **Pasta local para a demonstração.** `Email:Graph:LocalDomains`: o `GraphEmailSender` recebe, além do Graph, o `LocalEmailSender` da pasta `Email:Local:Folder`, e um destinatário de um desses domínios (sem diferenciar maiúsculas) vai para a pasta. A regra fica na classe que escolhe (`EmailRouting.IsLocal`), testada sozinha.
- **Opções conferidas ao subir (`EmailOptionsValidation`).** `Graph` é provedor conhecido; pede `Mailbox` com um endereço válido; `LocalDomains` pede a pasta local e reprova em produção; `Local` continua não subindo em produção.
- **Terraform.** `variable "email_mailbox"` (nula por padrão, validada como e-mail); com ela, `email_settings` = `Email__Provider = "Graph"` e `Email__Mailbox`, juntados ao `app_settings` depois dos do ambiente, como os do SharePoint. O `dev.tfvars` ganha `Email__Graph__LocalDomains__0 = "empresa.example"` e mantém o `Email__Local__Folder`.
- **`plan.sh`.** Lê `Email__Mailbox` da configuração do App Service, mascara com `::add-mask::` e exporta `TF_VAR_email_mailbox`, como o site do SharePoint.
- **Script do Exchange.** `infra/scripts/Grant-ExchangeMailbox.ps1` (ExchangeOnlineManagement e Azure CLI): a caixa (do `email_mailbox` do `local.auto.tfvars`, ou `fiscoprojects@` o domínio padrão do tenant), a entrada da conta bloqueada, `Enable-OrganizationCustomization` quando o tenant pede (com espera de até 1 hora para o Exchange aceitar o escopo), `New-ServicePrincipal`, o escopo `FiscoProjects - caixa de envio` (`PrimarySmtpAddress -eq`), a atribuição "Application Mail.Send" com `-CustomResourceScope`, e `Test-ServicePrincipalAuthorization` na caixa e na de quem roda. Repetir não muda nada. `-DisableWAM` para os computadores em que o WAM falha.
- **Testes.** O `FakeGraph` passa a receber `sendMail` só das caixas em `Mailboxes` (as outras dão 403 `ErrorAccessDenied`, como o RBAC do Exchange) e guarda o que recebeu; os testes da API ligam `Email:Provider` = `Graph` com ele no `graph-mail`.

## Risks / Trade-offs

- [A permissão do Exchange leva de 30 min a 2 h para valer] → os e-mails falham e voltam para a fila nesse tempo; o teste das Configurações gerais mostra quando passou.
- [Um envio aceito pelo Graph pode não chegar] → o Graph só aceita (202); a entrega é do Exchange, e o enviado fica na caixa (`saveToSentItems`).
- [A rodada manda até 20 e-mails a cada 30 s e o Exchange limita 30 por minuto por caixa] → o excedente atrasa no Exchange ou falha e volta à fila no tempo dela.

## Migration Plan

- Sem migração. Na instalação: rodar o script do Exchange, pôr `email_mailbox` no `local.auto.tfvars` e aplicar o Terraform (`docs/instalacao.md`). Sem a variável, tudo continua como antes.
