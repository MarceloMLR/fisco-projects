---
status: aceita
date: 2026-10-30
decision-makers: Marcelo Ribeiro
---

# ADR-0130: Como os termos são assinados pela Autentique

Complementa o ADR-0129 (termos de aceite assinados por um provedor de assinatura eletrônica trocável, a Autentique primeiro), o ADR-0004 (serviços externos atrás de interfaces, com implementação local) e o ADR-0123 (os e-mails da demonstração na pasta local).

## Contexto e problema

A Assinatura-1 entregou o contrato `ISignatureProvider`, o provedor local e a conferência em segundo plano. Falta a Autentique, com o aviso do provedor (webhook), o sandbox no ambiente `dev` e a chave no Key Vault. A documentação e o esquema da API GraphQL da Autentique, conferidos em 30/10/2026, deixam algumas escolhas abertas:

- Como falar com a API: um SDK da comunidade ou as operações escritas à mão?
- Como cancelar um pedido, se a exclusão do documento não bloqueia o que já tem assinatura?
- Onde fica o endereço do aviso, que a Autentique chama sem sessão e sem o cabeçalho `X-CSRF` (ADR-0054)?
- O que o aviso muda: o conteúdo dele, ou só dispara a conferência?
- Com que frequência a conferência roda, se cada leitura de documento é cobrada?
- Como a carga de demonstração do `dev`, com signatários fictícios, convive com o sandbox?

Preços e limites conferidos em 30/10/2026 (mudam, e cada instalação confere os dela):

- Plano gratuito: 10 documentos por mês pela API, 10 chamadas por minuto, arquivo de até 5 MB.
- Profissional: cerca de US$ 19 por mês, 60 chamadas por minuto, arquivo de até 20 MB. O uso além do plano é cobrado por ação: US$ 0,01 por documento criado, US$ 0,002 por pedido de assinatura por e-mail, US$ 0,0002 por documento lido e US$ 0,00004 por aviso entregue.
- Sandbox: sem cobrança e sem validade, com os documentos apagados depois de alguns dias.

## Fatores de decisão

- O mesmo contrato e os mesmos testes para cada provedor (ADR-0004, ADR-0129).
- Nenhuma chave, e-mail ou termo em log, erro ou URL (ADR-0057).
- A proteção de todas as alterações pelo `X-CSRF` sem exceções (ADR-0054).
- O mínimo de chamadas cobradas, com o resultado certo mesmo sem o aviso.
- A demonstração completa no `dev`, sem e-mails a endereços fictícios.

## Opções consideradas

API:

- A1. SDK da comunidade para .NET
- A2. As operações escritas à mão com o `HttpClient`

Cancelamento:

- B1. Excluir o documento (`deleteDocument`)
- B2. Pôr o prazo do documento (`deadline_at`) no instante atual, que o bloqueia

Endereço do aviso:

- C1. Sob `/api`, com uma marca que pula o `X-CSRF`
- C2. Fora de `/api`, como o retorno da entrada pela Microsoft (ADR-0117)

O que o aviso muda:

- D1. Aplica o conteúdo do aviso
- D2. Só põe a conferência do documento na fila, e a situação vem da Autentique

Signatários da demonstração no `dev`:

- E1. O `dev` sem a Autentique
- E2. A demonstração só com termos anexados
- E3. Fora da produção, os pedidos com todos os signatários nos domínios da demonstração ficam no provedor local

## Resultado da decisão

Opções escolhidas: **A2, B2, C2, D2 e E3**.

- **API:** o `AutentiqueSignatureProvider` monta as seis operações (criar com o arquivo, ler, reenviar, atualizar o prazo, excluir e baixar o PDF assinado) com o `HttpClient`, como o envio de e-mails e o SharePoint. O termo vai com o título, a mensagem, a recusa permitida e cada signatário por e-mail, que recebe o link da Autentique; os ids dos signatários saem das assinaturas devolvidas, pelo e-mail. O PDF assinado é baixado do endereço que a Autentique dá, com a chave só quando o endereço é dela.
- **Cancelamento:** o prazo no instante atual bloqueia o documento, inclusive com uma assinatura feita. A exclusão fica para a exclusão do projeto (ADR-0111).
- **Aviso:** `POST /signature-notices`, fora de `/api`, sem sessão. O aviso é conferido pela HMAC SHA-256 do corpo cru (`X-Autentique-Signature`) com o segredo do aviso; com a assinatura errada, responde 401; sem o segredo configurado, 404. O aviso válido só põe o documento numa fila em memória e responde 200 na hora; a conferência em segundo plano lê a fila e aplica a situação que a Autentique dá, uma vez só. Um aviso perdido, por reinício ou fila cheia, fica para a conferência periódica.
- **Conferência:** com o aviso ligado, a cada 60 minutos, só como garantia; sem o aviso, a cada 15.
- **Sandbox:** `Signature:Autentique:Sandbox`, só fora da produção, liga o sandbox; o `dev` o usa.
- **Demonstração (E3):** com `Signature:LocalDomains`, fora da produção, o pedido com todos os signatários nesses domínios vai para o provedor local, e os outros, para a Autentique, como os e-mails da demonstração (ADR-0123). Em produção, a aplicação não sobe com eles.
- **Segredos:** a chave (`Signature--Autentique--ApiKey`) e o segredo do aviso (`Signature--Autentique--WebhookSecret`) ficam no Key Vault, criados à mão por quem opera; o Terraform liga o provedor e põe só as referências (ADR-0052, ADR-0053).

### Consequências

- Bom, porque a Autentique passa nos mesmos testes do contrato que o provedor local, contra uma Autentique simulada no processo, e nenhum teste chama a Autentique real (ADR-0056).
- Bom, porque um aviso forjado, repetido ou fora de ordem não muda nada sem a Autentique confirmar.
- Bom, porque a proteção do `X-CSRF` continua sem exceção.
- Bom, porque o `dev` mostra a demonstração inteira e testa a Autentique com signatários reais, no sandbox.
- Ruim, porque o formato das respostas é o da documentação e do esquema publicados, com exemplos que discordam; o teste no sandbox do `dev` confere antes da produção.
- Ruim, porque o termo maior que o limite do plano é recusado pela Autentique, e o pedido diz só que o provedor falhou.
- Ruim, porque um aviso perdido atrasa a situação até a próxima conferência, de até 60 minutos.

### Confirmação

Testes cobrindo: o contrato do provedor contra a Autentique simulada; o sandbox; as assinaturas de fora do pedido ignoradas; a falha sem a chave e os e-mails; a chave só nos endereços da Autentique; o aviso de assinatura, o forjado, o de documento de fora e o sem segredo; o pedido aos signatários da demonstração no provedor local; a aplicação que não sobe sem a chave e com o sandbox ou os domínios da demonstração em produção; e o Terraform com os segredos só por referência ao Key Vault.

## Prós e contras das opções

### A1. SDK da comunidade

- Bom, porque já traz as operações.
- Ruim, porque os de .NET são pequenos e sem manutenção conhecida, e trariam uma dependência para seis operações.

### A2. Operações à mão

- Bom, porque não há dependência nova, e o padrão é o do Graph e do SharePoint.
- Ruim, porque o formato da API fica por conta do código e dos testes.

### B1. Excluir o documento

- Bom, porque não sobra documento na Autentique.
- Ruim, porque, com uma assinatura feita, a Autentique só move o documento para a lixeira, e os outros ainda assinam.

### B2. Prazo no instante atual

- Bom, porque bloqueia o documento em qualquer situação, como a documentação recomenda.
- Ruim, porque o documento continua na conta da Autentique até a exclusão do projeto.

### C1. Sob `/api`

- Bom, porque todas as rotas ficam sob `/api`.
- Ruim, porque abre uma exceção na proteção de todas as alterações por uma rota só.

### C2. Fora de `/api`

- Bom, porque a proteção do `X-CSRF` não muda, como no retorno da entrada pela Microsoft.
- Ruim, porque é uma rota de fora do padrão `/api`.

### D1. Aplicar o conteúdo do aviso

- Bom, porque não faz chamada à Autentique.
- Ruim, porque a Autentique não garante a ordem e repete eventos, e um aviso capturado vale para sempre.

### D2. Só disparar a conferência

- Bom, porque a situação vem sempre da Autentique, e o aviso responde rápido.
- Ruim, porque cada aviso custa uma leitura de documento.

### E1 e E2. Sem a Autentique no `dev`, ou só termos anexados

- Bom, porque não há roteamento de pedidos.
- Ruim, porque o `dev` não testaria a Autentique, ou a demonstração perderia os pedidos assinados, recusado e pendente.

### E3. Domínios da demonstração no provedor local

- Bom, porque segue o padrão dos e-mails da demonstração, e a demonstração fica inteira.
- Ruim, porque a instalação fora da produção tem dois provedores ao mesmo tempo.
