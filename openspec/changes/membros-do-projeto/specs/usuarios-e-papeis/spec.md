# Spec Delta

## MODIFIED Requirements

### Requirement: Tela Usuários

A tela Usuários da Administração MUST listar as pessoas em ordem alfabética, com nome, e-mail, papel no sistema, situação (ícone, cor e texto) e último acesso, e o total. A lista MUST ter busca por nome ou e-mail, sem diferenciar maiúsculas, minúsculas e acentos, filtro por papel e por situação e paginação. Pelo menu de cada pessoa, o administrador MUST poder trocar o papel e desativar ou reativar o acesso. Nome e e-mail MUST vir só do provedor de identidade, sem edição na tela (ADR-0008).

#### Scenario: Lista de usuários
- **WHEN** o administrador abre a tela Usuários
- **THEN** ele vê as pessoas em ordem alfabética, cada uma com nome, e-mail, papel, situação e último acesso, e o total de usuários

#### Scenario: Pessoa que nunca entrou
- **WHEN** o administrador abre a tela Usuários com uma pessoa incluída num projeto pelo diretório, que nunca entrou
- **THEN** a linha dessa pessoa mostra "Nunca entrou" no último acesso

#### Scenario: Busca por nome ou e-mail
- **WHEN** o administrador busca por parte do nome ou do e-mail, com ou sem acentos e maiúsculas
- **THEN** a lista mostra só as pessoas correspondentes

#### Scenario: Filtro por papel e situação
- **WHEN** o administrador filtra por um papel e por uma situação
- **THEN** a lista mostra só as pessoas com esse papel e essa situação

#### Scenario: Trocar o papel na tela
- **WHEN** o administrador escolhe outro papel no menu de uma pessoa
- **THEN** o papel novo aparece na lista e uma mensagem confirma a troca

#### Scenario: Desativar e reativar na tela
- **WHEN** o administrador desativa uma pessoa pelo menu
- **THEN** ela aparece como Inativa, com a linha esmaecida, e o menu passa a oferecer Reativar acesso

#### Scenario: Recusa do último administrador na tela
- **WHEN** o administrador tenta trocar o papel ou desativar o último administrador ativo pela tela
- **THEN** a tela mostra a explicação e a lista continua como estava

## ADDED Requirements

### Requirement: Pessoas da empresa para escolher

Qualquer pessoa com sessão MUST poder buscar pessoas por parte do nome ou do e-mail, sem diferenciar maiúsculas, minúsculas e acentos: os usuários ativos e as pessoas do diretório da empresa que ainda não entraram, em ordem alfabética, sem repetir ninguém. Pedindo só gerentes, a busca MUST trazer só os administradores e gerentes de projeto ativos (ADR-0005, ADR-0007, ADR-0072).

#### Scenario: Pessoas do sistema e do diretório
- **WHEN** um usuário busca por "a" com Carla Mendes no diretório, sem nunca ter entrado
- **THEN** a resposta traz os usuários ativos correspondentes e Carla Mendes, marcada como pessoa do diretório, em ordem alfabética

#### Scenario: Pessoa do diretório que já entrou
- **WHEN** uma pessoa do diretório já entrou no sistema
- **THEN** a busca a traz uma vez só, como usuário

#### Scenario: Só gerentes
- **WHEN** alguém busca pessoas pedindo só gerentes
- **THEN** a resposta traz só administradores e gerentes de projeto ativos, sem as pessoas do diretório

### Requirement: Usuário criado pelo diretório

Ao incluir num projeto uma pessoa do diretório que ainda não entrou, o sistema MUST criar o usuário com o nome e o e-mail do diretório, o papel Usuário, ativo e sem último acesso, e o vínculo com o provedor do diretório (ADR-0005, ADR-0006, ADR-0009).

#### Scenario: Usuário criado ao incluir
- **WHEN** o gerente inclui num projeto uma pessoa do diretório que nunca entrou
- **THEN** a pessoa passa a existir como usuário ativo, com o papel Usuário, sem último acesso e com o vínculo com o provedor do diretório
