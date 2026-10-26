## ADDED Requirements

### Requirement: Entrada pelo Entra ID

Com a entrada pelo Entra ID ligada, o e-mail informado MUST levar a pessoa à página da Microsoft, com o e-mail como dica e sem passar por endereços do FiscoProjects; senha e MFA ficam na Microsoft. Na volta, a sessão MUST ser aberta para o usuário do vínculo `entra` pelo identificador do objeto, criado no primeiro login, e a pessoa MUST voltar à tela pedida dentro do FiscoProjects. Os tokens da Microsoft MUST NOT chegar ao navegador nem ser guardados (ADR-0009, ADR-0054, ADR-0117).

#### Scenario: Entrada pelo Entra com a dica do e-mail
- **WHEN** a pessoa informa o e-mail na página de entrada, com a entrada pelo Entra ligada
- **THEN** a API devolve o endereço da Microsoft com o e-mail como dica, e o e-mail não aparece em nenhum endereço do FiscoProjects

#### Scenario: Primeira entrada pelo Entra
- **WHEN** uma pessoa da empresa volta da Microsoft pela primeira vez
- **THEN** o usuário é criado com o nome e o e-mail do token e o vínculo `entra` pelo identificador do objeto, a sessão é aberta só com o cookie, sem tokens, e a pessoa vai para a tela que tinha pedido

#### Scenario: Volta só para dentro do FiscoProjects
- **WHEN** a tela pedida na entrada aponta para outro site
- **THEN** depois de entrar, a pessoa vai para o Início

#### Scenario: Outra pessoa depois da sessão expirada pelo Entra
- **WHEN** a sessão de Marina expira e Diego entra pelo Entra no lugar dela
- **THEN** a aplicação mostra o Início, e não a tela em que Marina estava

#### Scenario: Credencial pela identidade gerenciada
- **WHEN** a aplicação troca o código da Microsoft pelo token
- **THEN** ela se identifica com a prova da identidade gerenciada, sem segredo nem certificado

### Requirement: Recusas na entrada pelo Entra

A conta convidada MUST ser recusada, sem criar usuário, mesmo autenticada no Entra (ADR-0007). A recusa do Entra por falta de atribuição ao aplicativo, o usuário desativado e uma volta da Microsoft que falha MUST levar à página de entrada com a mensagem de cada caso, sem sessão (ADR-0005, ADR-0008, ADR-0009). As recusas MUST entrar na auditoria.

#### Scenario: Convidado recusado
- **WHEN** uma conta convidada do tenant volta da Microsoft
- **THEN** a sessão não é aberta, nenhum usuário é criado, a página de entrada diz que contas convidadas não entram e a recusa fica na auditoria

#### Scenario: Sem atribuição no Entra
- **WHEN** o Entra devolve a recusa de uma pessoa que não foi atribuída ao aplicativo
- **THEN** a sessão não é aberta e a página de entrada diz que a pessoa precisa ser liberada pelo administrador do Entra

#### Scenario: Desativado na entrada pelo Entra
- **WHEN** uma pessoa desativada no FiscoProjects volta da Microsoft
- **THEN** a sessão não é aberta e a página de entrada diz que a conta está desativada

#### Scenario: Falha na volta da Microsoft
- **WHEN** a volta da Microsoft falha por outro motivo, como uma resposta adulterada
- **THEN** a sessão não é aberta e a página de entrada diz que não foi possível entrar

## MODIFIED Requirements

### Requirement: Entrada local

Fora da produção, com a entrada local ligada, o e-mail de uma pessoa do diretório de demonstração MUST abrir a sessão sem senha. Qualquer outro e-mail MUST ser recusado com uma mensagem ou, com a entrada pelo Entra também ligada, MUST seguir para a Microsoft, sem registro de recusa. Em produção, a aplicação MUST NOT subir com a entrada local ligada (ADR-0078, ADR-0117).

#### Scenario: Entrada com uma pessoa da demonstração
- **WHEN** a pessoa informa o e-mail de alguém do diretório de demonstração, com letras maiúsculas ou minúsculas
- **THEN** a sessão é aberta para essa pessoa e a aplicação mostra a tela pedida

#### Scenario: E-mail fora da demonstração
- **WHEN** a pessoa informa um e-mail que não está no diretório de demonstração
- **THEN** a sessão não é aberta e a página de entrada mostra que o acesso não foi liberado

#### Scenario: Entradas local e pelo Entra juntas
- **WHEN** as duas entradas estão ligadas e a pessoa informa um e-mail fora do diretório de demonstração
- **THEN** ela segue para a Microsoft com o e-mail como dica, e nenhuma recusa é registrada

#### Scenario: Entrada local em produção
- **WHEN** a aplicação sobe no ambiente de produção com a entrada local ligada
- **THEN** ela não sobe e informa que a entrada local não é permitida em produção
