## Why

No teste da Assinatura-2 no `dev`, "Lembrar signatários" logo depois do pedido mostrou "O provedor de assinatura não respondeu, e nada foi gravado". A Autentique respondeu, mas recusou o reenvio feito pouco depois do envio do link (`too_many_resent_emails`): a mensagem engana a gestão, que acha que o provedor está fora do ar. Correção da Assinatura-2 (Assinatura-2.1).

## What Changes

- O provedor diz quando recusa o lembrete porque mandou o link há pouco, separado da falha comum.
- O lembrete recusado por isso responde 409 com o tipo `signature-reminded-recently`, sem gravar o lembrete no histórico, e a tela diz que o provedor mandou o link há pouco e pede para lembrar de novo mais tarde.
- Os outros erros do provedor continuam como hoje (502, "não respondeu").

## Capabilities

### New Capabilities

- Nenhuma.

### Modified Capabilities

- `termos-de-aceite`: o lembrete recusado pelo provedor por ter mandado o link há pouco.

## Non-goals

- Esperar e lembrar sozinho mais tarde, ou mostrar quando o próximo lembrete será aceito: a Autentique não diz o prazo.
- Mudar o lembrete automático do provedor ou o limite dele.

## Impact

- `src/Modules/Documents`: o `SignatureProviderException` com o motivo, o `AutentiqueSignatureProvider` (o código `too_many_resent_emails`), o `RemindAcceptance` e o problema novo do endpoint.
- `tests/FiscoProjects.Testing`: o `FakeAutentique` com o reenvio recusado.
- Telas: a mensagem do lembrete na gaveta do termo (`web/src/acceptances`) e o texto em `pt-BR.json`.
- ADRs: 0129, 0130.
