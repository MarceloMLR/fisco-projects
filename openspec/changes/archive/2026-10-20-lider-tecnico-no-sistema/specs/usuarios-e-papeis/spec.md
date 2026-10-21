## MODIFIED Requirements

### Requirement: Papel no sistema

Todo usuário MUST ter um papel no sistema: Administrador, Gerente de projeto, Líder técnico ou Usuário. Quem entra pela primeira vez MUST receber o papel Usuário, o de menor privilégio. O Líder técnico no sistema só permite ser líder técnico num projeto. Uma troca de papel MUST valer a partir da requisição seguinte da pessoa, sem novo login (ADR-0006, ADR-0040, ADR-0097).

#### Scenario: Papel na primeira entrada
- **WHEN** uma pessoa que não é o primeiro administrador configurado entra pela primeira vez
- **THEN** ela recebe o papel Usuário

#### Scenario: Troca de papel na sessão aberta
- **WHEN** o administrador troca o papel de uma pessoa que está com a sessão aberta
- **THEN** a requisição seguinte dessa pessoa já usa o papel novo, sem ela entrar de novo

#### Scenario: Papel Líder técnico
- **WHEN** o administrador dá a Rafael Souza o papel Líder técnico na tela Usuários
- **THEN** a lista, o filtro por papel e o Meu perfil de Rafael mostram "Líder técnico"

### Requirement: Pessoas da empresa para escolher

Qualquer pessoa com sessão MUST poder buscar pessoas por parte do nome ou do e-mail, sem diferenciar maiúsculas, minúsculas e acentos: os usuários ativos e as pessoas do diretório da empresa que ainda não entraram, em ordem alfabética, sem repetir ninguém. Pedindo só gerentes, a busca MUST trazer só os administradores e gerentes de projeto ativos; pedindo só líderes técnicos, só os administradores e líderes técnicos ativos (ADR-0005, ADR-0007, ADR-0072, ADR-0097).

#### Scenario: Pessoas do sistema e do diretório
- **WHEN** um usuário busca por "a" com Carla Mendes no diretório, sem nunca ter entrado
- **THEN** a resposta traz os usuários ativos correspondentes e Carla Mendes, marcada como pessoa do diretório, em ordem alfabética

#### Scenario: Pessoa do diretório que já entrou
- **WHEN** uma pessoa do diretório já entrou no sistema
- **THEN** a busca a traz uma vez só, como usuário

#### Scenario: Só gerentes
- **WHEN** alguém busca pessoas pedindo só gerentes
- **THEN** a resposta traz só administradores e gerentes de projeto ativos, sem as pessoas do diretório

#### Scenario: Só líderes técnicos
- **WHEN** alguém busca pessoas pedindo só líderes técnicos
- **THEN** a resposta traz só administradores e líderes técnicos ativos, cada um marcado como quem pode ser líder técnico, sem as pessoas do diretório
