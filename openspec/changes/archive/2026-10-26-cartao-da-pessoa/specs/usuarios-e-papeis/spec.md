## ADDED Requirements

### Requirement: Dados de uma pessoa

Qualquer pessoa com sessão MUST poder ler o nome e o e-mail de um usuário pelo identificador, ativo ou desativado, para o cartão da pessoa (ADR-0118). Do usuário anonimizado, a resposta MUST trazer o identificador anônimo no lugar do nome e nenhum e-mail. Um identificador que não é de um usuário MUST dar 404.

#### Scenario: Dados de uma pessoa
- **WHEN** alguém com sessão pede os dados de Diego Torres pelo identificador dele
- **THEN** a resposta traz o nome e o e-mail de Diego

#### Scenario: Dados da pessoa anonimizada
- **WHEN** alguém pede os dados de uma pessoa anonimizada
- **THEN** a resposta traz o identificador anônimo como nome e nenhum e-mail

#### Scenario: Pessoa que não existe
- **WHEN** alguém pede os dados de um identificador que não é de um usuário
- **THEN** a resposta é 404

#### Scenario: Dados de uma pessoa sem sessão
- **WHEN** alguém sem sessão pede os dados de uma pessoa
- **THEN** a resposta é 401
