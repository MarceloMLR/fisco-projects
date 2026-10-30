## ADDED Requirements

### Requirement: Assinatura pela Autentique

Com o provedor `Autentique`, o sistema MUST mandar o termo à Autentique pela API dela, com a chave da instalação e cada signatário pelo nome e pelo e-mail; o signatário recebe o link por e-mail da Autentique, e o FiscoProjects MUST NOT mandar e-mail a ele. O cancelamento MUST bloquear o documento na Autentique. A chave e o e-mail dos signatários MUST NOT aparecer nos logs nem nos erros (ADR-0057, ADR-0129, ADR-0130).

#### Scenario: Pedido pela Autentique
- **WHEN** a gerente pede a assinatura de um termo a dois signatários com a Autentique configurada
- **THEN** a Autentique recebe o termo com o título, a mensagem e os dois signatários por e-mail, com a recusa permitida, o pedido fica Pendente com o documento da Autentique, e o FiscoProjects não manda e-mail

#### Scenario: Termo assinado pela Autentique
- **WHEN** os dois signatários assinam na Autentique e a conferência roda
- **THEN** o PDF assinado baixado da Autentique vira o documento Aprovado da pasta de termos, e o marco fica atingido

#### Scenario: Cancelar na Autentique
- **WHEN** a gerente cancela um pedido pendente na Autentique
- **THEN** o documento fica bloqueado na Autentique, e o pedido sai da lista

#### Scenario: Falha da Autentique sem dados pessoais
- **WHEN** a Autentique recusa a chave, limita as chamadas ou fica fora do ar
- **THEN** a API responde 502 ao pedido, nada é gravado, e o erro não traz a chave nem o e-mail dos signatários

### Requirement: Sandbox da Autentique

Fora da produção, a instalação MAY criar os documentos no sandbox da Autentique, de teste, sem custo e sem validade. A aplicação MUST NOT subir em produção com o sandbox ligado, nem com a Autentique sem a chave (ADR-0129, ADR-0130).

#### Scenario: Sandbox fora da produção
- **WHEN** a gerente pede uma assinatura num ambiente com o sandbox ligado
- **THEN** a Autentique recebe o documento como de teste

#### Scenario: Sandbox em produção
- **WHEN** a aplicação sobe em produção com o sandbox da Autentique ligado
- **THEN** ela não sobe, e o erro diz que o sandbox só vale fora da produção

#### Scenario: Autentique sem a chave
- **WHEN** a aplicação sobe com o provedor `Autentique` e sem a chave
- **THEN** ela não sobe, e o erro diz qual configuração falta

### Requirement: Aviso do provedor

O sistema MUST receber os avisos do provedor num endereço público, sem sessão, conferidos pela assinatura do aviso com o segredo da instalação, e MUST responder 200 sem esperar a conferência. O aviso válido MUST só pôr na fila a conferência do pedido do documento: a mudança vem da situação que o provedor dá. Com a assinatura errada, MUST responder 401 sem mudar nada; sem o segredo do aviso, 404 (ADR-0047, ADR-0130).

#### Scenario: Aviso de assinatura
- **WHEN** a Autentique avisa, com a assinatura certa, que os signatários de um pedido aberto assinaram
- **THEN** o endereço responde 200, e o pedido fica Assinado logo depois, sem esperar a conferência em segundo plano

#### Scenario: Aviso forjado
- **WHEN** alguém manda um aviso sem a assinatura certa
- **THEN** o endereço responde 401, e nenhum pedido muda

#### Scenario: Aviso de documento de fora
- **WHEN** chega um aviso válido de um documento que não é de nenhum pedido aberto
- **THEN** o endereço responde 200, e nada muda

#### Scenario: Aviso sem segredo configurado
- **WHEN** a instalação não tem o segredo do aviso, ou o provedor não avisa
- **THEN** o endereço responde 404

### Requirement: Signatários da demonstração no provedor local

Fora da produção, com domínios da demonstração configurados, um pedido com todos os signatários nesses domínios MUST ir para o provedor local, com o link de cada um na gaveta, e os outros pedidos MUST ir para o provedor da instalação. A aplicação MUST NOT subir em produção com domínios da demonstração (ADR-0123, ADR-0129, ADR-0130).

#### Scenario: Pedido aos signatários da demonstração
- **WHEN** a gerente pede a assinatura a signatários só dos domínios da demonstração, com a Autentique configurada
- **THEN** o pedido fica no provedor local, a gaveta mostra o link de cada signatário, e nada vai à Autentique

#### Scenario: Pedido com signatário de fora da demonstração
- **WHEN** um dos signatários do pedido não é de um domínio da demonstração
- **THEN** o pedido inteiro vai à Autentique

#### Scenario: Domínios da demonstração em produção
- **WHEN** a aplicação sobe em produção com domínios da demonstração para a assinatura
- **THEN** ela não sobe, e o erro diz que eles só valem fora da produção
