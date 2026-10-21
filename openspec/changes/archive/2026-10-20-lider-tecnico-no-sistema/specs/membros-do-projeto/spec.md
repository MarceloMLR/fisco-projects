## MODIFIED Requirements

### Requirement: Quem gerencia as pessoas do projeto

O administrador e o gerente do projeto MUST poder incluir, trocar o papel e remover qualquer pessoa. O líder técnico MUST poder fazer o mesmo com quem não é gerente, e MUST NOT incluir, remover, rebaixar ou promover gerentes (403). Colaborador, desenvolvedor e leitor MUST receber 403. Para quem não está no projeto e não é administrador, o projeto não existe (404); sem sessão, 401 (ADR-0042, ADR-0043).

#### Scenario: Gerente inclui, troca o papel e remove
- **WHEN** o gerente inclui Diego Torres como desenvolvedor, troca o papel dele para colaborador e o remove
- **THEN** a API aceita as três mudanças, e Diego sai do projeto

#### Scenario: Líder técnico inclui um desenvolvedor
- **WHEN** o líder técnico inclui como desenvolvedor uma pessoa com o papel no sistema Líder técnico e depois troca o papel dela para líder técnico
- **THEN** a API aceita as duas mudanças

#### Scenario: Líder técnico e os gerentes
- **WHEN** o líder técnico tenta incluir um gerente, promover alguém a gerente, rebaixar ou remover um gerente
- **THEN** a API responde 403 e as pessoas do projeto continuam como estavam

#### Scenario: Colaborador, desenvolvedor e leitor não mexem nas pessoas
- **WHEN** um colaborador, um desenvolvedor ou um leitor tenta incluir, trocar o papel ou remover uma pessoa
- **THEN** a API responde 403

#### Scenario: Administrador fora do projeto
- **WHEN** o administrador, sem estar no projeto, inclui uma pessoa
- **THEN** a API aceita a inclusão

#### Scenario: Pessoas de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede as pessoas do projeto ou tenta mudá-las
- **THEN** a API responde 404

#### Scenario: Pessoas do projeto sem sessão
- **WHEN** alguém sem sessão pede as pessoas de um projeto
- **THEN** a API responde 401

### Requirement: Último gerente

O projeto MUST ter sempre ao menos um gerente. Remover o último gerente ou trocar o papel dele MUST ser recusado com 409, mesmo com pedidos ao mesmo tempo (ADR-0042).

#### Scenario: Remover o último gerente
- **WHEN** o administrador tenta remover o único gerente do projeto
- **THEN** a API responde 409 e o gerente continua no projeto

#### Scenario: Rebaixar o último gerente
- **WHEN** o único gerente tenta trocar o próprio papel para desenvolvedor
- **THEN** a API responde 409 e ele continua gerente

#### Scenario: Outro gerente no projeto
- **WHEN** o projeto tem dois gerentes e um deles é removido
- **THEN** a API aceita, e o outro continua gerente

### Requirement: Incluir pessoa pela tela

"Incluir pessoa" MUST aparecer só para quem gerencia as pessoas, com o projeto aberto, e pedir a pessoa, buscada no sistema e no diretório, e o papel. Para o líder técnico, o papel Gerente de projeto MUST NOT aparecer. Quem tem o papel no sistema Líder técnico MUST já vir com o papel Líder técnico, e o papel Líder técnico MUST ficar indisponível para quem não pode tê-lo (ADR-0042, ADR-0085, ADR-0097).

#### Scenario: Incluir pessoa pela tela
- **WHEN** o gerente escolhe "Incluir pessoa", busca uma pessoa que nunca entrou, escolhe o papel Colaborador e confirma
- **THEN** a pessoa aparece no time Funcional e uma mensagem confirma a inclusão

#### Scenario: Papel sugerido ao incluir
- **WHEN** o gerente escolhe em "Incluir pessoa" uma pessoa com o papel no sistema Líder técnico, e depois uma com o papel Usuário
- **THEN** o papel vem como Líder técnico para a primeira, e para a segunda o papel Líder técnico fica indisponível, com a explicação

#### Scenario: Membros sem gerenciar
- **WHEN** um desenvolvedor abre a aba Membros
- **THEN** a tela não mostra "Incluir pessoa" e o detalhe de cada pessoa não oferece trocar o papel nem remover

## ADDED Requirements

### Requirement: Líder técnico com o papel no sistema

Incluir uma pessoa como líder técnico ou trocar o papel de alguém para líder técnico MUST aceitar só quem está ativo e tem, naquele momento, o papel no sistema Líder técnico ou Administrador; os outros MUST receber 400 no papel. Trocar depois o papel no sistema não muda o papel nos projetos (ADR-0097).

#### Scenario: Promover a líder técnico
- **WHEN** o gerente troca para líder técnico o papel de um desenvolvedor com o papel no sistema Líder técnico
- **THEN** a API aceita, e a pessoa passa ao papel Líder técnico

#### Scenario: Líder técnico sem o papel no sistema
- **WHEN** o gerente inclui como líder técnico uma pessoa com o papel no sistema Usuário ou uma pessoa que ainda não entrou, ou troca para líder técnico o papel de um desenvolvedor com o papel Usuário
- **THEN** a API responde 400 no papel e nada muda no projeto

#### Scenario: Líder técnico que perde o papel no sistema
- **WHEN** o administrador troca para Usuário o papel no sistema de quem é líder técnico num projeto
- **THEN** a pessoa continua líder técnico nesse projeto
