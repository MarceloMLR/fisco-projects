## ADDED Requirements

### Requirement: Foto da pessoa

Qualquer pessoa com sessão MUST poder ler a foto de um usuário no Microsoft 365 pelo identificador dele, lida pelo Microsoft Graph com o token do aplicativo, só JPEG ou PNG. Sem vínculo com o Entra ID, sem foto ou com o Graph fora do ar, a resposta MUST ser 404, sem erro para quem pediu e sem identificar a pessoa no log. A foto MUST NOT ser guardada no banco; o servidor e o navegador a guardam por até uma hora (ADR-0124).

#### Scenario: Foto do Microsoft 365
- **WHEN** alguém com sessão pede a foto de Helena Duarte, que entrou pelo Entra ID e tem foto no Microsoft 365
- **THEN** a resposta traz a imagem, que o navegador guarda por uma hora, e o segundo pedido não vai de novo ao Graph

#### Scenario: Foto só no tamanho original
- **WHEN** a foto de Helena não existe no tamanho de 96x96
- **THEN** a resposta traz a foto no maior tamanho que ela tem

#### Scenario: Pessoa sem foto
- **WHEN** alguém pede a foto de quem entrou localmente ou de quem não tem foto no Microsoft 365
- **THEN** a resposta é 404

#### Scenario: Foto de quem foi anonimizado
- **WHEN** alguém pede a foto de uma pessoa anonimizada
- **THEN** a resposta é 404, mesmo que a foto tenha sido lida antes

#### Scenario: Microsoft 365 fora do ar na foto
- **WHEN** o Graph não responde durante o pedido de uma foto
- **THEN** a resposta é 404, sem erro, e a falha vai para o log sem identificar a pessoa

#### Scenario: Foto sem sessão
- **WHEN** alguém sem sessão pede a foto de uma pessoa
- **THEN** a resposta é 401
