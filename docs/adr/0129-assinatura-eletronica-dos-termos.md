---
status: proposta
date: 2026-10-30
decision-makers: Marcelo Ribeiro
---

# ADR-0129: Termos de aceite assinados por um provedor de assinatura eletrônica trocável, só da gestão

Substitui o ADR-0035 e, nos termos de aceite, partes do ADR-0045 e do ADR-0095.

## Contexto e problema

Na v1, o gestor pede o aceite de um marco a uma pessoa do projeto, que assina o PDF fora do sistema, com a ferramenta que preferir, e envia o arquivo assinado; ou o gestor anexa o termo assinado recebido por fora (ADR-0035, ADR-0095). Na prática, quem assina é quase sempre o cliente, que não entra no sistema (ADR-0007), e o gestor vira o carteiro do PDF: manda por e-mail, cobra, recebe e anexa.

Em 29/10/2026, Marcelo pediu a assinatura dos termos dentro do fluxo, por um provedor de assinatura eletrônica com plano gratuito até um limite, trocável sem mexer no resto do código, como o Gantt (ADR-0051), e com os termos vistos e geridos só pelo gerente do projeto e pelo administrador.

Perguntas:

- Qual provedor primeiro, e como trocar de provedor depois?
- Quem assina, e como se identifica?
- O que o sistema guarda de quem assina, que pode estar fora da empresa (ADR-0058)?
- Como o sistema sabe que o termo foi assinado ou recusado?
- O que acontece com o fluxo atual: a resposta da pessoa do projeto e o termo anexado?

## Fatores de decisão

- Assinatura com validade de assinatura eletrônica, sem o gestor carregar PDFs de um lado para o outro.
- Custo zero até um volume pequeno, e um caminho claro quando a empresa precisar de mais (ADR-0002, ADR-0003).
- Trocar de provedor numa implementação só, atrás de um contrato, com uma implementação local para desenvolvimento e testes (ADR-0004).
- Guardar o mínimo de dados pessoais de quem está fora da empresa (ADR-0058, LGPD).
- Os termos ficam com a gestão, sem expor negociações com clientes (ADR-0045).

## Opções consideradas

Provedor (pesquisado em 29/10/2026; os preços mudam e são conferidos na Assinatura-2):

- A1. Autentique: 10 documentos por mês no plano gratuito, API (GraphQL) em todos os planos, ambiente de testes (sandbox) gratuito e sem limite, e o plano Profissional a cerca de US$ 19 por mês, sem limite de documentos
- A2. ZapSign: API só no plano Team (R$ 69,90 por mês, 20 documentos)
- A3. Clicksign: sandbox gratuito, produção a partir de R$ 69 por mês, 20 documentos
- A4. DocuSeal: API só no plano Pro
- A5. Documenso instalado junto, sem custo de licença, mas AGPL e mais um serviço para operar

Quem assina:

- B1. Uma pessoa do projeto, como hoje
- B2. Um ou mais signatários, com o nome e o e-mail digitados, de dentro ou de fora da empresa

Como quem assina se identifica:

- C1. Só pelo link no e-mail, como no plano gratuito
- C2. Também por SMS, WhatsApp ou validação de documento, que custam créditos
- C3. Certificado ICP-Brasil

E-mail de quem assina:

- D1. Guardado com o termo, para sempre
- D2. Guardado só enquanto o pedido está aberto; com o termo assinado, recusado ou cancelado, o e-mail sai, e o nome fica no histórico

Como o sistema sabe da assinatura:

- E1. Só pelo aviso do provedor (webhook)
- E2. Só perguntando ao provedor de tempos em tempos
- E3. O aviso do provedor e, de tempos em tempos, a conferência dos pedidos abertos

## Resultado da decisão

Opções escolhidas, por decisão de Marcelo em 29/10/2026: **A1, B2, C1 e D2**; e **E3**, que garante o resultado mesmo quando um aviso do provedor se perde ou a instalação não recebe avisos de fora.

- **Provedor:** a Autentique primeiro (Assinatura-2), atrás do contrato `ISignatureProvider` do módulo dos termos: mandar o termo com os signatários, lembrar, cancelar, saber a situação de cada signatário e baixar o PDF assinado. Trocar de provedor é escrever outra implementação do contrato e escolhê-la na configuração (`Signature:Provider`), sem mexer nos casos de uso nem nas telas. A implementação local (`Local`) assina numa página do próprio sistema, aberta pelo link do e-mail local, só fora da produção, como a entrada local (ADR-0078) e os e-mails locais (ADR-0036). A chave do provedor fica no Key Vault (ADR-0052).
- **Pedido:** o administrador e o gerente pedem a assinatura do termo de um marco, com o título, a mensagem opcional, o termo em PDF e de 1 a 10 signatários, com o nome e o e-mail, de dentro ou de fora da empresa. Todos assinam ao mesmo tempo, sem ordem. Sem provedor configurado, o pedido não sai, e a gestão só anexa termos assinados recebidos por fora.
- **Assinatura:** cada signatário recebe o link do provedor por e-mail e assina ou recusa lá, sem entrar no sistema. Quando todos assinam, o sistema baixa do provedor o PDF assinado, que vira o documento Aprovado da pasta de termos, vinculado ao marco, e o marco fica atingido, como hoje (ADR-0093). A recusa de um signatário, com o motivo, recusa o termo: o provedor encerra o documento, e um pedido novo recomeça (ADR-0095, B1).
- **Lembrar e cancelar:** o lembrete pede ao provedor que mande o link de novo a quem ainda não assinou; o cancelamento cancela o documento no provedor e tira o pedido da lista, que fica só na auditoria (ADR-0095, A1).
- **Termo anexado:** continua. A gestão anexa o termo assinado recebido por fora num pedido pendente, que fica Assinado (e o documento do provedor é cancelado), ou sem pedido, com o marco, o título e o PDF; quem assinou está no próprio PDF, e o registro guarda quem anexou.
- **Dados de quem assina (D2):** o sistema guarda o nome e o e-mail de cada signatário só enquanto o pedido está aberto. Com o termo assinado, recusado ou cancelado, o e-mail é apagado, e o nome fica no histórico. Os dados completos de quem assinou ficam no PDF assinado que o provedor devolve. Tudo sai com o projeto excluído (ADR-0111), inclusive o documento no provedor, quando o provedor permite. A auditoria registra o e-mail como segredo (ADR-0039).
- **Quem vê:** só o administrador e o gerente do projeto veem a aba Termos de aceite, a pasta de termos e os termos assinados, e fazem todas as ações. A pessoa do projeto deixa de responder pelo sistema e de ver o próprio termo (ADR-0045 e ADR-0095 nesses pontos).
- **Situação:** o sistema recebe o aviso do provedor num endereço público, conferido pela assinatura do aviso (o segredo fica no Key Vault), e confere os pedidos abertos de tempos em tempos, em segundo plano (ADR-0047). As duas vias aplicam a mesma mudança uma vez só.
- **Avisos:** os e-mails aos signatários saem do provedor. O termo assinado e a recusa avisam quem pediu e o gerente pelo sino e pelo resumo diário, como hoje (ADR-0037, ADR-0038). O Meu trabalho e o resumo diário deixam de trazer pedidos para a pessoa responder.
- **Pedidos de antes:** os termos assinados e recusados do fluxo anterior ficam como estão, com a pessoa do projeto a quem foram pedidos. Os pendentes ficam na lista, sem provedor, e a gestão só os cancela ou anexa o termo assinado.
- **Autenticação:** só o link no e-mail (C1). SMS, WhatsApp, validação de documento e ICP-Brasil ficam para depois, com o custo deles.

### Consequências

- Bom, porque o gestor pede a assinatura e recebe o termo assinado sem carregar PDFs, e o cliente assina sem entrar no sistema.
- Bom, porque o provedor é uma implementação só, e uma empresa que precise de outro (ou de mais de 10 documentos por mês) troca a configuração ou o plano, sem mudança no resto.
- Bom, porque o e-mail de quem está fora da empresa fica só o tempo do pedido.
- Ruim, porque os termos passam a depender de um serviço externo, com conta e plano por instalação, e o plano gratuito tem 10 documentos por mês.
- Ruim, porque os documentos também ficam na conta do provedor, pelo contrato com ele; a exclusão do projeto pede a exclusão lá, quando o provedor permite.
- Ruim, porque a pessoa do projeto deixa de ver o termo que assinou no sistema; ela tem o PDF que o provedor manda.

### Confirmação

Testes cobrindo: o pedido com 1 a 10 signatários e o PDF; o termo assinado só quando todos assinam, com o PDF do provedor na pasta de termos e o marco atingido; a recusa de um signatário recusando o termo; o lembrete e o cancelamento no provedor; o e-mail dos signatários apagado ao fechar o pedido; só a gestão vendo e agindo nos termos; o aviso do provedor e a conferência aplicando a mudança uma vez; o provedor local fora da produção; e o mesmo conjunto de testes do contrato para cada implementação do `ISignatureProvider`.

## Prós e contras das opções

### A1. Autentique

- Bom, porque tem plano gratuito com API e sandbox gratuito, e é brasileira, com a assinatura eletrônica da MP 2.200-2 (art. 10, § 2º).
- Ruim, porque o gratuito tem 10 documentos por mês, e o uso da API conta no plano.

### A2 a A4. ZapSign, Clicksign e DocuSeal

- Bom, porque são provedores conhecidos, com API.
- Ruim, porque a API só vem em plano pago.

### A5. Documenso instalado junto

- Bom, porque não tem custo por documento.
- Ruim, porque é AGPL e mais um serviço para instalar e manter em cada empresa.

### B1. Pessoa do projeto

- Bom, porque não guarda dado de quem está fora do sistema.
- Ruim, porque quem assina de fato, o cliente, fica de fora.

### B2. Signatários digitados

- Bom, porque o cliente e outras pessoas de fora assinam direto.
- Ruim, porque guarda nome e e-mail de quem está fora da empresa, o que D2 limita.

### C1. Só o link no e-mail

- Bom, porque é gratuito e simples para quem assina.
- Ruim, porque a identificação é a mais fraca das opções.

### C2 e C3. SMS, documento ou ICP-Brasil

- Bom, porque identificam melhor quem assina.
- Ruim, porque custam créditos ou certificado.

### D1. E-mail para sempre

- Bom, porque o histórico mostra o e-mail.
- Ruim, porque guarda dado pessoal de quem está fora da empresa sem necessidade.

### D2. E-mail só com o pedido aberto

- Bom, porque guarda o mínimo, e o PDF assinado tem os dados completos.
- Ruim, porque o histórico de um termo fechado mostra só o nome.

### E1, E2 e E3. Aviso, conferência ou os dois

- E1 é imediato, mas um aviso perdido deixa o pedido parado, e a instalação precisa de endereço público.
- E2 funciona sem endereço público, mas atrasa a situação e faz mais chamadas ao provedor.
- E3 junta os dois: imediato quando o aviso chega e certo quando não chega.

## Mais informações

- A validade jurídica de cada forma de assinatura deve ser confirmada com um advogado; esta decisão não é parecer jurídico. Entre particulares, vale a assinatura eletrônica aceita pelas partes (MP 2.200-2, art. 10, § 2º); a Lei 14.063 trata das interações com o poder público.
- A Assinatura-1 entrega o contrato, o provedor local, os signatários e as telas; a Assinatura-2, a Autentique, com o sandbox no ambiente `dev`, a chave e o segredo do aviso no Key Vault e o passo no guia de instalação.
