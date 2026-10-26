# entrada-e-sessao Specification

## Purpose

Define como a pessoa entra no FiscoProjects, como o sistema a reconhece, quanto dura e como é protegida a sessão e como ela sai (ADR-0005, ADR-0009, ADR-0054, ADR-0078).

## Requirements

### Requirement: Página de entrada

Sem sessão, toda tela MUST levar à página de entrada, que pede o e-mail. Depois de entrar, a pessoa MUST voltar à tela que tinha pedido, menos depois de "Sair" e quando entra outra pessoa no lugar de quem teve a sessão expirada: nesses casos, MUST ir ao Início. O último e-mail usado MUST ficar lembrado no navegador.

#### Scenario: Tela aberta sem sessão
- **WHEN** alguém sem sessão abre uma tela, como Projetos
- **THEN** a página de entrada é exibida e, depois de entrar, a pessoa volta para Projetos

#### Scenario: Mesma pessoa depois da sessão expirada
- **WHEN** a sessão de Marina expira na página Chamados e ela entra de novo
- **THEN** a aplicação volta para Chamados

#### Scenario: Outra pessoa depois da sessão expirada
- **WHEN** a sessão de Marina expira na página Chamados e Diego entra no lugar dela
- **THEN** a aplicação mostra o Início

#### Scenario: E-mail lembrado
- **WHEN** a pessoa volta à página de entrada depois de ter entrado uma vez
- **THEN** o campo de e-mail já vem preenchido com o último e-mail usado

#### Scenario: Página de entrada com sessão
- **WHEN** alguém que já tem sessão abre a página de entrada
- **THEN** a aplicação mostra o Início

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

### Requirement: Usuário no primeiro login

Na primeira entrada de uma identidade, o sistema MUST criar o usuário, com identificador próprio, e o vínculo com o provedor (provedor e identificador externo) em registro separado. Nas entradas seguintes, MUST reconhecer o mesmo usuário e atualizar nome e e-mail com os dados do provedor. Quem foi incluído num projeto pelo diretório MUST entrar com o usuário já criado, sem outro (ADR-0005, ADR-0009).

#### Scenario: Primeira entrada
- **WHEN** uma identidade entra pela primeira vez
- **THEN** o sistema cria o usuário com o nome e o e-mail do provedor, ativo, e o vínculo com o provedor

#### Scenario: Entradas seguintes
- **WHEN** a mesma identidade entra de novo com outro nome no provedor
- **THEN** o sistema usa o mesmo usuário, sem criar outro, e atualiza o nome

#### Scenario: Primeira entrada de quem foi incluído pelo diretório
- **WHEN** uma pessoa incluída num projeto pelo diretório entra pela primeira vez
- **THEN** o sistema usa o usuário já criado, registra o último acesso, e ela vê o projeto

### Requirement: Sessão protegida

A sessão MUST ficar só num cookie `HttpOnly`, `Secure` e `SameSite=Lax`, sem nenhum token acessível ao JavaScript. Ela MUST expirar depois de 8 horas sem uso (valor configurável) e, a cada requisição, MUST conferir se o usuário existe e está ativo. Chamadas à API sem sessão válida MUST receber 401, e toda alteração na API MUST exigir o cabeçalho `X-CSRF`, contra requisições forjadas (ADR-0054).

#### Scenario: Cookie da sessão
- **WHEN** a sessão é aberta
- **THEN** o navegador recebe um cookie `HttpOnly`, `Secure` e `SameSite=Lax`, e a resposta não traz token

#### Scenario: API sem sessão
- **WHEN** alguém chama a API sem sessão
- **THEN** a resposta tem status 401 em Problem Details

#### Scenario: Sessão parada por 8 horas
- **WHEN** a sessão fica 8 horas sem uso
- **THEN** a próxima chamada recebe 401

#### Scenario: Uso renova a sessão
- **WHEN** a pessoa usa a aplicação antes das 8 horas
- **THEN** a sessão continua válida por mais 8 horas a partir do uso

#### Scenario: Usuário desativado durante a sessão
- **WHEN** o usuário de uma sessão aberta deixa de estar ativo
- **THEN** a próxima chamada recebe 401

#### Scenario: Alteração sem proteção contra requisição forjada
- **WHEN** alguém envia uma alteração à API (POST, PUT, PATCH ou DELETE) sem o cabeçalho `X-CSRF`
- **THEN** a resposta tem status 403 em Problem Details e nada é alterado

#### Scenario: Sessão expirada na tela
- **WHEN** uma chamada da tela recebe 401
- **THEN** a aplicação mostra a página de entrada

### Requirement: Sair

"Sair" MUST encerrar só a sessão do FiscoProjects e levar à página de entrada; a entrada seguinte MUST levar ao Início, seja quem for (ADR-0009).

#### Scenario: Sair
- **WHEN** a pessoa escolhe Sair
- **THEN** a sessão é encerrada, a página de entrada é exibida e as chamadas seguintes recebem 401

#### Scenario: Entrar depois de Sair
- **WHEN** Marina escolhe Sair na página Projetos e Diego entra em seguida
- **THEN** a aplicação mostra o Início, e não Projetos

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
