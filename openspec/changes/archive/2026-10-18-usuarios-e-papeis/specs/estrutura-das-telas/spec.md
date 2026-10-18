# Spec Delta

## MODIFIED Requirements

### Requirement: Barra lateral no computador

Em telas de 900px ou mais, a aplicação MUST mostrar uma barra lateral fixa, da altura da janela, com o selo e o nome do produto no topo e os itens Início, Projetos e Chamados e, só para o administrador, Administração, destacando o item da página atual.

#### Scenario: Navegação principal
- **WHEN** a aplicação é aberta numa tela de 900px ou mais
- **THEN** a barra lateral mostra o nome do produto e os itens Início, Projetos e Chamados

#### Scenario: Item da página atual
- **WHEN** a pessoa está numa página da navegação principal
- **THEN** o item dessa página fica marcado como o atual, e só ele

#### Scenario: Administração na barra lateral
- **WHEN** o administrador abre a aplicação
- **THEN** a barra lateral mostra também o item Administração, que quem não é administrador não vê

### Requirement: Cabeçalho com a trilha

Sobre o conteúdo, a aplicação MUST mostrar um cabeçalho da mesma cor do conteúdo e sem linha, com a trilha da página, cujos níveis acima levam às áreas em que a página fica e cujo último nível é a página atual, e, à direita, o avatar da pessoa na sessão, que abre o menu da conta.

#### Scenario: Trilha da página
- **WHEN** a pessoa abre uma página
- **THEN** o cabeçalho mostra a trilha com o nome da página como último nível, marcado como a página atual

#### Scenario: Trilha de uma página dentro de uma área
- **WHEN** o administrador abre a tela Usuários
- **THEN** a trilha mostra Administração e Usuários, e Administração leva à área de Administração

#### Scenario: Conta no cabeçalho
- **WHEN** a pessoa abre o menu do avatar no cabeçalho
- **THEN** o menu mostra o nome e o e-mail dela e a opção Sair, e o avatar traz as iniciais do nome
