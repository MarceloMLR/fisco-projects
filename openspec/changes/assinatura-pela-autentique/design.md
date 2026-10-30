# Design

## Context

A Assinatura-1 deixou o contrato `ISignatureProvider` (mandar, situação, lembrar, cancelar, PDF assinado e excluir), o provedor local, a conferência em segundo plano (`SignatureCheck`, que roda o `ApplySignatureStatus` de cada pedido aberto) e o `ApplySignatureStatus.ForDocumentAsync`, pensado para o aviso do provedor. O `SignatureProviders` conhece um provedor só, o de `Signature:Provider`.

A API da Autentique é GraphQL (`https://api.autentique.com.br/v2/graphql`), com a chave no cabeçalho `Authorization: Bearer`, conferida na documentação oficial e no esquema publicado em 30/10/2026:

- `createDocument(document, signers, file, sandbox)` recebe o PDF pelo envio de arquivos do GraphQL (`operations`, `map` e o arquivo) e devolve o documento com as assinaturas (`public_id`, `email`, `action`).
- `document(id)` traz cada assinatura com `signed` e `rejected` (data e motivo) e os endereços dos arquivos (`files.signed`).
- `resendSignatures(public_ids)` manda o link de novo; `updateDocument(id, document: { deadline_at })` bloqueia o documento; `deleteDocument(id)` exclui o documento, mas não bloqueia o que já tem assinatura (vai para a lixeira).
- A falha chega como 401 (chave), 429 (limite de chamadas: 10, 60 ou 200 por minuto, conforme o plano) ou 200 com `errors` (os códigos `document_not_found`, `validation` e outros).
- O aviso (webhook) é cadastrado no painel, com o segredo dele; cada evento chega por POST com `X-Autentique-Signature`, a HMAC SHA-256 do corpo cru em hexadecimal. O documento está em `event.data.id` nos eventos `document.*` e em `event.data.document` nos `signature.*`; a documentação mostra também o recurso em `event.data.object`. A Autentique não garante a ordem e pode repetir o evento, e tenta de novo depois de 60, 120 e 300 segundos.

## Goals / Non-Goals

**Goals:**

- O provedor da Autentique atrás do contrato, sem mudar casos de uso nem telas, com os mesmos testes do contrato do provedor local.
- O aviso do provedor aplicando a situação na hora, com a conferência em segundo plano como garantia.
- O `dev` com o sandbox, sem a carga de demonstração mandar e-mails a endereços fictícios.
- A chave e o segredo do aviso só no Key Vault, e a instalação pelo guia.

**Non-Goals:**

- Outros provedores, a troca de provedor com pedidos abertos e a cobrança da Autentique.
- Cadastrar o aviso pela API da Autentique.

## Decisions

### A API escrita à mão, sem SDK

O `AutentiqueSignatureProvider` (Infrastructure do `Documents`) monta as operações GraphQL e o envio do arquivo com o `HttpClient` nomeado `autentique`, como o `GraphEmailSender` e o `SharePointFileStorage`. Os SDKs da comunidade para .NET são pequenos e sem manutenção conhecida, e trariam uma dependência para seis operações (ADR-0061).

- O pedido manda o documento com `name` = título, `message` = mensagem e `refusable: true`, e cada signatário com `email`, `name` e `action: SIGN`; o sandbox vai como argumento (`sandbox: true`) só quando ligado. Os ids de cada signatário saem das assinaturas devolvidas, pelo e-mail (único no pedido), ignorando assinaturas sem ação ou de outro e-mail.
- A situação lê `document(id)`: `rejected` dá Recusado com o motivo e a data, `signed` dá Assinado com a data, e o resto, Pendente.
- O lembrete lê a situação e manda `resendSignatures` aos pendentes; com o documento bloqueado ou recusado, falha.
- O cancelamento põe o prazo (`deadline_at`) no instante atual, que bloqueia o documento; o documento já assinado (`document_signed`) fica como está.
- O PDF assinado só sai com todos os signatários assinados, baixado do endereço `files.signed`, com a chave só quando o endereço é da Autentique (`https` em `*.autentique.com.br`), conferido como PDF e com o tamanho limitado; o nome é o título, em `.pdf`.
- A exclusão chama `deleteDocument`; o documento que a Autentique não tem não é erro.
- Toda falha vira `SignatureProviderException` com a operação, o código HTTP e o código de erro da Autentique, sem a chave, os e-mails ou o conteúdo, e vai para o log como aviso, com o mesmo texto.

Alternativa descartada: cancelar com `deleteDocument`. Com uma assinatura feita, a Autentique só move o documento para a lixeira, e os outros signatários ainda assinariam.

### O aviso fora de `/api`, só disparando a conferência

`POST /signature-notices` fica fora de `/api`, como o retorno da entrada pela Microsoft (ADR-0054): a Autentique não manda sessão nem `X-CSRF`, e a rota não precisa de exceção na conferência do `X-CSRF`. O provedor que avisa implementa `ISignatureNotices` (Application): confere a HMAC do corpo cru em tempo constante e devolve o documento do aviso, lido de `event.data` ou `event.data.object`. A rota lê no máximo 256 KB e:

- sem `ISignatureNotices` (provedor local, ou a Autentique sem o segredo do aviso), responde 404;
- com a assinatura errada ou ausente, responde 401;
- com a assinatura certa, põe o documento na fila em memória (`SignatureNoticeQueue`, até 500 documentos, sem repetir o que já está nela) e responde 200.

A `SignatureCheck` lê a fila ao lado da conferência periódica e roda o `ApplySignatureStatus.ForDocumentAsync` de cada documento num escopo próprio. O conteúdo do aviso nunca é aplicado: a situação vem sempre da Autentique, e o `ApplySignatureStatus` já aplica cada assinatura uma vez só.

Alternativas descartadas: a rota sob `/api` com uma marca que pula o `X-CSRF` (uma exceção na proteção de todas as alterações por uma rota só); aplicar o conteúdo do aviso direto (um aviso repetido ou fora de ordem mudaria o pedido sem a Autentique confirmar); conferir dentro da requisição do aviso (a Autentique pede 2xx rápido, e o PDF assinado vai para o SharePoint). Um aviso perdido na fila, por reinício ou fila cheia, fica para a conferência periódica.

### A conferência a cada 60 minutos com o aviso

Cada leitura de documento é cobrada no plano pago da Autentique, e o plano gratuito aceita 10 chamadas por minuto. Com o aviso ligado, o Terraform põe `Signature:CheckMinutes` = 60, e a conferência só garante o aviso perdido; sem o aviso, continuam os 15 minutos.

### Os signatários da demonstração no provedor local

Fora da produção, com `Signature:LocalDomains` e um provedor que não é o local, o `SignatureProviders` ganha o provedor local como provedor da demonstração (`SignatureDemonstration`): um pedido com todos os signatários nesses domínios vai para ele, e os outros, para o provedor da instalação, como os e-mails do Graph (ADR-0123). Cada pedido guarda o nome do provedor, e o `Of(term)` acha o provedor dele entre os dois; a conferência, a exclusão do projeto e a página local passam pelos dois. Em produção, a aplicação não sobe com domínios da demonstração; com o provedor local ou sem provedor, eles não mudam nada.

Alternativas descartadas: o `dev` sem a Autentique (o roadmap pede o sandbox no `dev`); a carga de demonstração só com termos anexados (a demonstração perderia os pedidos assinados, recusado e pendente).

### Configuração, Key Vault e Terraform

`Signature:Autentique:ApiKey`, `Signature:Autentique:WebhookSecret` (opcional) e `Signature:Autentique:Sandbox`. A validação ao subir recusa a Autentique sem a chave e o sandbox ou os domínios da demonstração em produção. No Terraform, a variável `autentique = { sandbox, webhook }` liga `Signature__Provider`, o sandbox, a conferência de 60 minutos com o aviso e as referências ao Key Vault dos segredos `Signature--Autentique--ApiKey` e `Signature--Autentique--WebhookSecret`, que quem opera cria à mão, como a chave do Syncfusion (ADR-0053). O módulo `app` ganha o mapa `vault_settings`, configuração lida de um segredo do Key Vault.

## Risks / Trade-offs

- [A documentação da Autentique tem exemplos que discordam (o lugar do recurso no aviso, o formato das datas)] → O aviso aceita os dois lugares, as datas aceitam qualquer formato ISO 8601 (sem fuso, UTC), e o teste no sandbox do `dev` confere antes da produção.
- [O aviso não traz hora assinada, e um aviso capturado vale para sempre] → O aviso só dispara a conferência: repetido, não muda nada.
- [O termo maior que o limite do plano (5 MB no gratuito) é recusado pela Autentique] → O pedido responde 502 com a mensagem de falha do provedor; o guia avisa o limite.
- [O plano gratuito tem 10 documentos por mês e 10 chamadas por minuto] → A conferência periódica fica a cada 60 minutos com o aviso, e uma chamada recusada pelo limite só espera a próxima rodada.
- [Aplicar o Terraform com a Autentique antes de publicar a versão que a conhece derruba o `dev`] → O guia e o roadmap dizem a ordem: publicar a versão, criar os segredos e só então aplicar.
