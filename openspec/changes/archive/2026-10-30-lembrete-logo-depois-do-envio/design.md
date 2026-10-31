# Design

## Context

O `RemindAcceptance` pede o lembrete ao provedor e trata qualquer `SignatureProviderException` como falha (502, `signature-provider-failed`). A Autentique recusa o `resendSignatures` feito pouco depois do envio, com o código `too_many_resent_emails`, sem dizer o prazo; quando só parte dos signatários está no prazo, ela reenvia aos outros e responde sem erro.

## Goals / Non-Goals

**Goals:** separar "o provedor mandou o link há pouco" da falha comum, do provedor à tela, sem registrar o lembrete.

**Non-Goals:** saber ou mostrar quando o reenvio volta a ser aceito.

## Decisions

- O `SignatureProviderException` ganha o motivo (`SignatureFailure`: `Failed`, o padrão, ou `RemindedRecently`), em vez de uma exceção nova: o contrato continua com uma exceção só, e os casos de uso que não lembram ignoram o motivo.
- O `AutentiqueSignatureProvider` lança `RemindedRecently` para o código `too_many_resent_emails` e registra o caso no log como informação, não como aviso: é o comportamento esperado da Autentique.
- O `RemindAcceptance` devolve `AcceptanceOutcome.RemindedRecently`, e o endpoint responde 409 com o tipo `urn:fiscoprojects:problem:signature-reminded-recently`. 409, e não 502: o provedor respondeu, e o pedido está num estado que não aceita o lembrete agora.
- A tela mostra "O provedor de assinatura mandou o link há pouco. Tente lembrar de novo mais tarde." no lugar da falha, no mesmo lugar da mensagem de hoje.

## Risks / Trade-offs

- [A Autentique pode mudar o código] → Um código desconhecido continua como falha comum (502), como hoje.
