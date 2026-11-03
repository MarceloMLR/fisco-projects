## MODIFIED Requirements

### Requirement: Sessão protegida

A sessão MUST ficar só num cookie `HttpOnly`, `Secure` e `SameSite=Lax`, sem nenhum token acessível ao JavaScript. Ela MUST expirar depois de 8 horas sem uso (valor configurável) e, a cada requisição, MUST conferir se o usuário existe e está ativo. Chamadas à API sem sessão válida MUST receber 401, salvo as leituras do Teams, que usam o token do Teams (ADR-0152), e toda alteração na API MUST exigir o cabeçalho `X-CSRF`, contra requisições forjadas (ADR-0054).

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

## ADDED Requirements

### Requirement: Entrada pelo Teams

Com a entrada pelo Entra ligada, as leituras do Teams MUST aceitar o token que o Teams entrega à página: assinado pelo Entra ID, do tenant da instalação, para o aplicativo dela, no prazo, com o escopo `access_as_user` e de um cliente do Teams. A pessoa MUST ser achada pelo vínculo `entra` do identificador do objeto. O convidado, quem nunca entrou e o desativado MUST receber 401, sem criar usuário nem sessão (ADR-0005, ADR-0007, ADR-0008, ADR-0152).

#### Scenario: Token do Teams aceito
- **WHEN** uma pessoa ativa que já entrou pelo Entra lê um chamado pela rota do Teams com o token do Teams
- **THEN** a rota responde como ela, e a resposta não abre sessão nem traz cookie

#### Scenario: Token de outro aplicativo recusado
- **WHEN** a rota do Teams recebe um token para outra audiência, de outro tenant, com outro escopo, de um cliente que não é o Teams, com a assinatura errada ou vencido
- **THEN** a resposta é 401 em Problem Details

#### Scenario: Convidado recusado pelo Teams
- **WHEN** a rota do Teams recebe o token de uma conta convidada do tenant
- **THEN** a resposta é 401, e nenhum usuário é criado

#### Scenario: Quem nunca entrou recusado pelo Teams
- **WHEN** a rota do Teams recebe o token de uma pessoa sem o vínculo `entra` no FiscoProjects
- **THEN** a resposta é 401, e nenhum usuário é criado

#### Scenario: Desativado recusado pelo Teams
- **WHEN** a rota do Teams recebe o token de uma pessoa desativada no FiscoProjects
- **THEN** a resposta é 401

#### Scenario: Teams sem a entrada pelo Entra
- **WHEN** a rota do Teams é chamada numa instalação com a entrada pelo Entra desligada
- **THEN** a resposta é 401, sem conferir nenhum token

### Requirement: Token do Teams só nas leituras do Teams

O token do Teams MUST valer só nas rotas sob `/api/teams`, que só leem (`GET`), e essas rotas MUST NOT aceitar o cookie da sessão. O token MUST NOT ser guardado pela aplicação nem aparecer em endereços, respostas ou registros (ADR-0054, ADR-0057, ADR-0152).

#### Scenario: Token do Teams fora das rotas do Teams
- **WHEN** alguém chama outra rota da API só com o token do Teams
- **THEN** a resposta é 401

#### Scenario: Sessão nas rotas do Teams
- **WHEN** alguém com a sessão aberta chama a rota do Teams sem o token do Teams
- **THEN** a resposta é 401

#### Scenario: Token do Teams fora dos registros
- **WHEN** a rota do Teams recebe um token, aceito ou recusado
- **THEN** nenhum registro, em nenhum nível, traz o token
