---
status: aceita
date: 2026-10-27
decision-makers: Marcelo Ribeiro
---

# ADR-0123: Como os e-mails saem pelo Microsoft Graph

Complementa o ADR-0036 (e-mails pelo Microsoft Graph a partir de uma caixa compartilhada, com fila no banco), o ADR-0004 (Microsoft 365 atrás de interfaces) e o ADR-0119 (o Microsoft Graph com a identidade do aplicativo).

## Contexto e problema

A fila de e-mails existe desde a Avisos-1, com a implementação local, e o ADR-0036 decidiu o envio pelo Graph a partir de uma caixa compartilhada, com a permissão do aplicativo restrita a ela pelo RBAC para aplicações do Exchange Online, deixando os comandos exatos para a implementação. Faltava decidir como a mensagem vai ao Graph, o que fazer quando ele limita as chamadas, como a caixa e a permissão são preparadas e o que acontece com os e-mails das pessoas fictícias da demonstração num ambiente que envia de verdade.

## Fatores de decisão

- Nenhum segredo: o aplicativo já prova quem é pela identidade gerenciada do App Service (ADR-0117, ADR-0119).
- O aplicativo envia só pela caixa configurada (ADR-0036).
- Nada pessoal no repositório nem nos logs públicos (ADR-0116).
- Um ambiente de demonstração não manda e-mails para endereços que não existem: cada um volta como aviso de não entrega para a caixa e pode levar o Exchange a bloquear o envio do tenant.

## Opções consideradas

A mensagem:

1. O `sendMail` do Graph com a mensagem em JSON, o corpo em HTML.
2. O `sendMail` com a mensagem em MIME, com as partes de texto e HTML, o que pede montar o MIME (outra dependência).

Os e-mails para as pessoas da demonstração, num ambiente que envia pelo Graph:

1. Os endereços dos domínios configurados como locais ficam na pasta local, como `.eml`; os outros saem pelo Graph.
2. Enviar todos, e deixar os avisos de não entrega chegarem à caixa.
3. Manter o ambiente de demonstração só com a implementação local, sem testar o envio de ponta a ponta.

## Resultado da decisão

O JSON com HTML e a pasta local para os domínios da demonstração.

- **Mensagem:** `POST /users/{caixa}/sendMail` com o assunto, o corpo em HTML do `EmailRenderer`, o destinatário com o nome e `saveToSentItems`, para os enviados ficarem na caixa. O texto simples continua na fila e no `.eml` local.
- **Token:** o mesmo `IGraphTokens` do `Users.Contracts` dos arquivos e da busca de pessoas, sem nenhuma permissão nova no Entra ID: o papel "Application Mail.Send" é dado pelo Exchange, só para a caixa.
- **Falhas:** um envio recusado ou limitado (429, 503, 504) vira falha da tentativa, e a fila tenta de novo no tempo dela (1 min, 5 min, 15 min, 1 h e 4 h). A falha registra o código do Graph, nunca o destinatário nem o conteúdo.
- **Configuração:** `Email:Provider` = `Graph` e `Email:Mailbox`, a caixa de envio. O endereço da caixa traz o domínio do tenant, então vem do `local.auto.tfvars`, fora do Git, e a conferência da infraestrutura no pipeline o lê da configuração da aplicação no ar e o mascara, como o site do SharePoint (ADR-0120).
- **Demonstração:** fora da produção, `Email:Graph:LocalDomains` lista os domínios cujos e-mails ficam na pasta local (`Email:Local:Folder`); no ambiente `dev`, o `empresa.example` do diretório de demonstração. A aplicação não sobe em produção com domínios locais.
- **Caixa e permissão:** o `infra/scripts/Grant-ExchangeMailbox.ps1`, rodado por quem administra o Exchange, usa a caixa que a empresa já tem, compartilhada ou de uma pessoa (como um projetos@ que a equipe já lê), sem mudar nada nela; grupos e listas de distribuição não servem, porque o aplicativo não envia por eles. Sem a caixa, cria uma compartilhada e deixa a conta dela sem entrada (fora da busca de pessoas, ADR-0119). Depois, libera a personalização do Exchange num tenant novo (`Enable-OrganizationCustomization`, uma vez, sem volta), registra o aplicativo no Exchange, cria o escopo só com a caixa e dá o papel "Application Mail.Send" nesse escopo, conferindo no fim com `Test-ServicePrincipalAuthorization` que o aplicativo envia pela caixa e não pela de quem roda o script.
- **Testes:** um Graph simulado no processo recebe os envios, nunca o Microsoft 365 real (ADR-0056).

### Consequências

- Boa, porque o aplicativo envia só pela caixa, sem segredo e sem permissão ampla no Entra ID.
- Boa, porque o ambiente de demonstração testa o envio de verdade para as pessoas do tenant sem mandar nada para endereços fictícios.
- Ruim, porque a parte em texto simples do `EmailRenderer` não vai junto: o e-mail sai só em HTML.
- Ruim, porque preparar a caixa é um passo à mão no Exchange, e a permissão leva de 30 minutos a 2 horas para valer.
