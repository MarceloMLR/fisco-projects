## Why

A Assinatura-1 entregou o contrato do provedor de assinatura, o provedor local e as telas, mas nenhuma instalação assina de verdade: sem provedor configurado, a gestão só anexa termos recebidos por fora. Esta é a segunda fatia do ADR-0129 (Assinatura-2): a Autentique, o primeiro provedor real, com o aviso do provedor, o sandbox no ambiente `dev`, a chave no Key Vault e o passo no guia de instalação.

## What Changes

- Provedor `Autentique` (`Signature:Provider` = `Autentique`), implementado pela API GraphQL da Autentique com a chave da instalação: o termo vai com os signatários por e-mail, cada um recebe o link da Autentique e assina ou recusa lá; o sistema confere a situação, lembra quem não assinou, cancela (o documento fica bloqueado na Autentique), baixa o PDF assinado e exclui o documento com o projeto. Os testes do contrato do provedor passam a valer também para ela, contra uma Autentique simulada no processo.
- Aviso do provedor: a Autentique avisa cada assinatura e recusa num endereço público da instalação (`POST /signature-notices`, fora de `/api`), conferido pela assinatura do aviso com o segredo da instalação. O aviso só põe a conferência daquele pedido na fila; a situação vem sempre da Autentique, e a conferência em segundo plano continua como garantia.
- Sandbox: fora da produção, `Signature:Autentique:Sandbox` cria documentos de teste, sem custo e sem validade; a aplicação não sobe em produção com o sandbox nem com a Autentique sem a chave.
- Signatários da demonstração: fora da produção, os pedidos com todos os signatários nos domínios de `Signature:LocalDomains` ficam no provedor local, como os e-mails da demonstração (ADR-0123), para a carga de demonstração e quem testa no `dev` não mandarem e-mails a endereços fictícios.
- Terraform: a variável `autentique` (sandbox e aviso) liga o provedor na aplicação, com a chave e o segredo do aviso lidos do Key Vault, criados à mão; o `dev` usa o sandbox e os domínios da demonstração.
- Guia de instalação: a conta e a chave da Autentique, o cadastro do aviso e os segredos no Key Vault.
- ADR-0130 com as decisões da integração.

## Capabilities

### New Capabilities

- Nenhuma: a Autentique entra nas capacidades `termos-de-aceite` e `infraestrutura`.

### Modified Capabilities

- `termos-de-aceite`: a assinatura pela Autentique, o sandbox, o aviso do provedor e os signatários da demonstração no provedor local.
- `infraestrutura`: a Autentique ligada pelo Terraform, com os segredos no Key Vault, e o sandbox no `dev`.

## Non-goals

- Outros provedores de assinatura e a troca de provedor de uma instalação com pedidos abertos.
- Identificar quem assina por SMS, WhatsApp, documento ou certificado ICP-Brasil, e posicionar a assinatura no PDF (ADR-0129, depois).
- Cadastrar o aviso na Autentique pela API: o cadastro é no painel dela, pelo guia.
- Conta, plano e cobrança da Autentique, que são de cada instalação.
- Mudar as telas: o texto do pedido já diz que cada signatário recebe o link do provedor por e-mail.

## Impact

- `src/Modules/Documents`: o `AutentiqueSignatureProvider` (GraphQL e envio do arquivo pelo `HttpClient`), as opções `Signature:Autentique:*` e `Signature:LocalDomains` e a validação ao subir, o `SignatureProviders` com o provedor da demonstração, o contrato `ISignatureNotices`, a fila dos avisos lida pela `SignatureCheck` e a rota `POST /signature-notices`.
- `src/FiscoProjects.Api`: o mapeamento da rota do aviso fora de `/api`.
- `tests/FiscoProjects.Testing`: o `FakeAutentique`, que simula a API e os arquivos assinados.
- `infra/terraform`: a variável `autentique`, os segredos `Signature--Autentique--ApiKey` e `Signature--Autentique--WebhookSecret` lidos do Key Vault e o `dev` com o sandbox e os domínios da demonstração.
- Docs: ADR-0130 (novo), `docs/instalacao.md`, `CLAUDE.md`, o glossário e o roadmap.
- ADRs: 0004, 0036, 0047, 0052, 0053, 0054, 0057, 0123, 0127, 0129, 0130.
