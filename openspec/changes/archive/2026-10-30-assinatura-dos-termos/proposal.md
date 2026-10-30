## Why

Hoje o gestor pede o aceite de um marco a uma pessoa do projeto, que assina o PDF fora do sistema e o envia; na prática, quem assina é o cliente, que não entra no sistema, e o gestor carrega o PDF de um lado para o outro. Marcelo pediu a assinatura dentro do fluxo, por um provedor de assinatura eletrônica com plano gratuito, trocável como o Gantt, e com os termos só da gestão (ADR-0129, que substitui o ADR-0035). Esta é a primeira fatia (Assinatura-1): o contrato do provedor, o provedor local, os signatários e as telas; a Autentique vem na Assinatura-2.

## What Changes

- **BREAKING** O pedido de aceite passa a ter de 1 a 10 signatários, com o nome e o e-mail digitados, de dentro ou de fora da empresa, no lugar da pessoa do projeto; o termo vai para o provedor de assinatura, e cada signatário assina ou recusa pelo link do provedor, sem entrar no sistema.
- **BREAKING** A pessoa do projeto deixa de responder pelo sistema (enviar o PDF assinado ou recusar) e de ver o próprio termo: a aba Termos de aceite, a pasta de termos e os termos assinados ficam só do administrador e do gerente (ADR-0129, no lugar do ADR-0045 nesse ponto).
- Contrato `ISignatureProvider` no módulo `Documents`: mandar o termo com os signatários, lembrar, cancelar, saber a situação de cada signatário e baixar o PDF assinado; escolhido por `Signature:Provider`, com a implementação local (`Local`), que assina numa página do próprio sistema e não sobe em produção. Sem provedor configurado, o pedido de assinatura não sai, e a gestão só anexa termos assinados recebidos por fora.
- Quando todos assinam, o PDF assinado do provedor vira o documento Aprovado da pasta de termos, vinculado ao marco, e o marco fica atingido, como hoje; a recusa de um signatário, com o motivo, recusa o termo.
- A situação de cada pedido aberto é conferida no provedor por um processo em segundo plano e, quando o provedor avisa (Assinatura-2), na hora; as duas vias aplicam a mudança uma vez só.
- "Lembrar" pede ao provedor o link de novo para quem ainda não assinou; "Cancelar pedido" cancela no provedor. "Anexar termo assinado" continua, num pedido pendente ou sem pedido (sem escolher pessoa do projeto).
- O e-mail de cada signatário fica guardado só enquanto o pedido está aberto; ao assinar, recusar ou cancelar, sai, e o nome fica no histórico. A exclusão do projeto cancela no provedor os documentos dele.
- O Meu trabalho e o resumo diário deixam de trazer pedidos para a pessoa responder; quem pediu continua vendo os pedidos pendentes dela. O pedido e o lembrete deixam de gerar e-mail do FiscoProjects (o provedor manda).
- Os termos do fluxo anterior ficam como estão; os pendentes, sem provedor, só são cancelados ou recebem o termo anexado.
- Dados de demonstração pelo provedor local: um termo assinado por dois signatários, um anexado, um recusado e um pendente com um dos dois signatários já assinado.

## Capabilities

### New Capabilities
- Nenhuma: os termos de aceite já têm a capacidade `termos-de-aceite`.

### Modified Capabilities
- `termos-de-aceite`: signatários digitados, o provedor de assinatura trocável e o local, a conferência da situação, o lembrete e o cancelamento no provedor, o termo anexado sem pessoa, o e-mail dos signatários só com o pedido aberto, só a gestão vendo e agindo, os pedidos anteriores e as telas.
- `documentos`: a pasta de termos e os termos assinados só para o administrador e o gerente.
- `meu-trabalho`: sem pedidos de aceite para a pessoa responder; os pedidos que ela fez continuam.
- `avisos`: o pedido e o lembrete sem e-mail do FiscoProjects, e o resumo diário sem os pedidos pendentes da pessoa.
- `usuarios-e-papeis`: o pedido de aceite deixa de escolher uma pessoa do projeto.
- `dados-de-demonstracao`: os termos da demonstração pelo provedor local.

## Non-goals

- A Autentique, o aviso do provedor (webhook), o sandbox no ambiente `dev`, a chave no Key Vault e o passo no guia de instalação (Assinatura-2).
- Identificar quem assina por SMS, WhatsApp, documento ou certificado ICP-Brasil (ADR-0129, depois).
- Ordem de assinatura entre os signatários, gerar o PDF do termo a partir de um modelo e conferir a assinatura do PDF anexado (depois).
- Aceite por clique dentro do sistema (continua fora).
- Mudar os pedidos do fluxo anterior para o provedor.

## Impact

- `src/Modules/Documents`: `AcceptanceTerm` com os signatários (`acceptance_signers`) e o documento no provedor, `AcceptanceAccess` só da gestão, os casos de uso do pedido, da conferência (`ApplySignatureStatus`), do lembrete, do cancelamento e do termo anexado, o contrato `ISignatureProvider`, o provedor local (estado em arquivos fora do Git, como os e-mails locais), a conferência em segundo plano, a página local de assinatura (rotas sem sessão, só fora da produção), a migração, a exclusão do projeto, o `AcceptanceWorkSource` e o `IPendingAcceptances`.
- `src/FiscoProjects.Api`: a configuração `Signature:*` e a recusa de subir com o provedor local em produção.
- `src/Modules/Notifications`: o resumo diário sem os pedidos pendentes da pessoa.
- Telas: `web/src/acceptances` (o pedido com os signatários, a lista, a gaveta com a situação de cada signatário e os links do provedor local) e a página local de assinatura, fora da estrutura das telas; a aba e a pasta de termos só da gestão; os textos e os tipos da API.
- `web/scripts/demo`: os termos pelo provedor local.
- Docs: ADR-0129 (novo, substitui o ADR-0035 e partes do ADR-0045 e do ADR-0095), `CLAUDE.md` (a fronteira dos aceites), `docs/discovery.md`, `openspec/config.yaml`, o glossário e o roadmap.
- ADRs: 0004, 0007, 0036, 0037, 0038, 0039, 0045, 0047, 0052, 0058, 0078, 0081, 0093, 0095, 0111, 0129.
