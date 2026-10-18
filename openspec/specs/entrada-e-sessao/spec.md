# entrada-e-sessao Specification

## Purpose

Define como a pessoa entra no FiscoProjects, como o sistema a reconhece, quanto dura e como é protegida a sessão e como ela sai (ADR-0005, ADR-0009, ADR-0054, ADR-0078).

## Requirements

### Requirement: Página de entrada

Sem sessão, toda tela MUST levar à página de entrada, que pede o e-mail. Depois de entrar, a pessoa MUST voltar à tela que tinha pedido. O último e-mail usado MUST ficar lembrado no navegador.

#### Scenario: Tela aberta sem sessão
- **WHEN** alguém sem sessão abre uma tela, como Projetos
- **THEN** a página de entrada é exibida e, depois de entrar, a pessoa volta para Projetos

#### Scenario: E-mail lembrado
- **WHEN** a pessoa volta à página de entrada depois de ter entrado uma vez
- **THEN** o campo de e-mail já vem preenchido com o último e-mail usado

#### Scenario: Página de entrada com sessão
- **WHEN** alguém que já tem sessão abre a página de entrada
- **THEN** a aplicação mostra o Início

### Requirement: Entrada local

Fora da produção, com a entrada local ligada, o e-mail de uma pessoa do diretório de demonstração MUST abrir a sessão sem senha, e qualquer outro e-mail MUST ser recusado com uma mensagem. Em produção, a aplicação MUST NOT subir com a entrada local ligada (ADR-0078).

#### Scenario: Entrada com uma pessoa da demonstração
- **WHEN** a pessoa informa o e-mail de alguém do diretório de demonstração, com letras maiúsculas ou minúsculas
- **THEN** a sessão é aberta para essa pessoa e a aplicação mostra a tela pedida

#### Scenario: E-mail fora da demonstração
- **WHEN** a pessoa informa um e-mail que não está no diretório de demonstração
- **THEN** a sessão não é aberta e a página de entrada mostra que o acesso não foi liberado

#### Scenario: Entrada local em produção
- **WHEN** a aplicação sobe no ambiente de produção com a entrada local ligada
- **THEN** ela não sobe e informa que a entrada local não é permitida em produção

### Requirement: Usuário no primeiro login

Na primeira entrada de uma identidade, o sistema MUST criar o usuário, com identificador próprio, e o vínculo com o provedor (provedor e identificador externo) em registro separado. Nas entradas seguintes, MUST reconhecer o mesmo usuário e atualizar nome e e-mail com os dados do provedor (ADR-0005, ADR-0009).

#### Scenario: Primeira entrada
- **WHEN** uma identidade entra pela primeira vez
- **THEN** o sistema cria o usuário com o nome e o e-mail do provedor, ativo, e o vínculo com o provedor

#### Scenario: Entradas seguintes
- **WHEN** a mesma identidade entra de novo com outro nome no provedor
- **THEN** o sistema usa o mesmo usuário, sem criar outro, e atualiza o nome

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

"Sair" MUST encerrar só a sessão do FiscoProjects e levar à página de entrada (ADR-0009).

#### Scenario: Sair
- **WHEN** a pessoa escolhe Sair
- **THEN** a sessão é encerrada, a página de entrada é exibida e as chamadas seguintes recebem 401
