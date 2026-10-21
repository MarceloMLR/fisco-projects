## Context

Os anexos ficam no `IFileStorage` e são servidos por `GET /api/tickets/{number}/attachments/{id}` (`OpenTicketAttachment`), com `nosniff`, `sandbox` e `no-store`; o tipo que o navegador mostra (`FileTypes.OpensInBrowser`) sai sem nome de download.

## Goals / Non-Goals

**Goals:** baixar um anexo com o nome original; baixar todos num `.zip`.

**Non-Goals:** escolher quais anexos baixar; baixar os anexos de vários chamados de uma vez.

## Decisions

- **`download=true`** no endereço do anexo, em vez de outro endpoint: o mesmo acesso e os mesmos cabeçalhos, só com o nome de download.
- **O `.zip` montado no servidor**, com o `System.IO.Compression` do .NET, escrito direto na resposta, um anexo por vez, sem guardar o `.zip`. Alternativa descartada: montar no navegador, que exigiria uma biblioteca e baixar cada arquivo antes.
- **Nomes repetidos** (o mesmo nome em mensagens diferentes) ganham " (2)", " (3)" antes da extensão, na ordem de envio; o `.zip` se chama `chamado-{número}-anexos.zip`.
- **Na tela**, o nome do anexo continua um link que abre o arquivo, e o ícone "Baixar" fica ao lado, como outro link, porque um link não pode ficar dentro de outro.

## Risks / Trade-offs

- **[Chamado com muitos anexos grandes]** O `.zip` pode demorar. Mitigação: é escrito direto na resposta, sem memória extra, e cada anexo tem no máximo 15 MB.
