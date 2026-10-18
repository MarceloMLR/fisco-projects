# Spec Delta

## Purpose

Define o papel de cada pessoa no sistema, como a instalação ganha o primeiro administrador e nunca fica sem nenhum, como o administrador desativa e reativa pessoas e o que a tela Usuários da Administração mostra (ADR-0006, ADR-0008, ADR-0040, ADR-0046).

## ADDED Requirements

### Requirement: Papel no sistema

Todo usuário MUST ter um papel no sistema: Administrador, Gerente de projeto ou Usuário. Quem entra pela primeira vez MUST receber o papel Usuário, o de menor privilégio. Uma troca de papel MUST valer a partir da requisição seguinte da pessoa, sem novo login (ADR-0006, ADR-0040).

#### Scenario: Papel na primeira entrada
- **WHEN** uma pessoa que não é o primeiro administrador configurado entra pela primeira vez
- **THEN** ela recebe o papel Usuário

#### Scenario: Troca de papel na sessão aberta
- **WHEN** o administrador troca o papel de uma pessoa que está com a sessão aberta
- **THEN** a requisição seguinte dessa pessoa já usa o papel novo, sem ela entrar de novo

### Requirement: Primeiro administrador

A configuração da instalação MUST indicar o primeiro administrador pela identidade dele no provedor (no Entra, o identificador imutável `oid`). Na primeira entrada dessa pessoa, ela MUST receber o papel Administrador. Se ela já tinha entrado antes e não há nenhum administrador ativo, MUST receber o papel na entrada seguinte. Enquanto houver um administrador ativo, a configuração MUST NOT desfazer uma troca de papel feita na tela (ADR-0006).

#### Scenario: Primeiro administrador configurado
- **WHEN** a pessoa indicada na configuração entra pela primeira vez
- **THEN** ela recebe o papel Administrador

#### Scenario: Configuração depois da primeira entrada
- **WHEN** a pessoa indicada na configuração já tinha entrado como Usuário, não há administrador ativo e ela entra de novo
- **THEN** ela recebe o papel Administrador

#### Scenario: Troca feita na tela respeitada
- **WHEN** o papel da pessoa indicada na configuração foi trocado na tela, há outro administrador ativo e ela entra de novo
- **THEN** ela continua com o papel escolhido na tela

### Requirement: Último administrador ativo

O sistema MUST impedir que a instalação fique sem administrador ativo: a troca de papel ou a desativação do último administrador ativo MUST ser recusada, com uma mensagem que explica o motivo, e nada MUST ser alterado (ADR-0006).

#### Scenario: Trocar o papel do último administrador
- **WHEN** o único administrador ativo tenta trocar o próprio papel
- **THEN** a troca é recusada, a mensagem explica que a instalação precisa de um administrador ativo e o papel continua Administrador

#### Scenario: Desativar o último administrador
- **WHEN** o único administrador ativo tenta se desativar
- **THEN** a desativação é recusada com a mesma explicação e ele continua ativo

#### Scenario: Outro administrador ativo
- **WHEN** há dois administradores ativos e um deles troca o papel do outro para Usuário
- **THEN** a troca é aceita

### Requirement: Desativar e reativar

O administrador MUST poder desativar e reativar qualquer usuário, respeitando o último administrador ativo. Quem é desativado MUST perder o acesso na requisição seguinte, mesmo com a sessão aberta, e MUST NOT conseguir entrar; nome, e-mail, papel e histórico MUST ser preservados. Quem é reativado MUST voltar a entrar, com o mesmo papel. MUST NOT existir caminho para criar nem excluir usuários manualmente (ADR-0008).

#### Scenario: Desativar acesso
- **WHEN** o administrador desativa uma pessoa que está com a sessão aberta
- **THEN** a requisição seguinte dela recebe 401 e uma nova entrada é recusada como conta desativada

#### Scenario: Reativar acesso
- **WHEN** o administrador reativa uma pessoa desativada
- **THEN** ela volta a entrar, com o papel que tinha

### Requirement: Administração só do administrador

Ver a lista de usuários, trocar o papel de um usuário e desativar ou reativar um usuário MUST ser permitido ao Administrador e negado, com 403, ao Gerente de projeto e ao Usuário. A área de Administração MUST aparecer na tela só para o Administrador (ADR-0046).

#### Scenario: Administrador na administração de usuários
- **WHEN** o Administrador lista os usuários, troca o papel de alguém, desativa e reativa
- **THEN** as quatro ações são aceitas

#### Scenario: Gerente de projeto na administração de usuários
- **WHEN** o Gerente de projeto tenta listar os usuários, trocar o papel de alguém, desativar ou reativar
- **THEN** as quatro ações recebem 403 e nada é alterado

#### Scenario: Usuário na administração de usuários
- **WHEN** o Usuário tenta listar os usuários, trocar o papel de alguém, desativar ou reativar
- **THEN** as quatro ações recebem 403 e nada é alterado

#### Scenario: Endereço da Administração sem ser administrador
- **WHEN** um Gerente de projeto ou um Usuário abre o endereço da tela Usuários
- **THEN** a aplicação mostra o Início, sem o item Administração na barra lateral

### Requirement: Tela Usuários

A tela Usuários da Administração MUST listar as pessoas em ordem alfabética, com nome, e-mail, papel no sistema, situação (ícone, cor e texto) e último acesso, e o total. A lista MUST ter busca por nome ou e-mail, sem diferenciar maiúsculas, minúsculas e acentos, filtro por papel e por situação e paginação. Pelo menu de cada pessoa, o administrador MUST poder trocar o papel e desativar ou reativar o acesso. Nome e e-mail MUST vir só do provedor de identidade, sem edição na tela (ADR-0008).

#### Scenario: Lista de usuários
- **WHEN** o administrador abre a tela Usuários
- **THEN** ele vê as pessoas em ordem alfabética, cada uma com nome, e-mail, papel, situação e último acesso, e o total de usuários

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
