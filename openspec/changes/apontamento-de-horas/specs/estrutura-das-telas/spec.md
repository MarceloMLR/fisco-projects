## MODIFIED Requirements

### Requirement: Barra lateral no computador

Em telas de 900px ou mais, a aplicação MUST mostrar uma barra lateral fixa, da altura da janela, com o nome da instalação no topo, depois do logo da instalação ou, sem logo, do selo, e os itens Início, Projetos, Chamados e Minhas horas e, só para o administrador, Administração, destacando o item da página atual.

#### Scenario: Navegação principal
- **WHEN** a aplicação é aberta numa tela de 900px ou mais
- **THEN** a barra lateral mostra o nome da instalação e os itens Início, Projetos, Chamados e Minhas horas

#### Scenario: Item da página atual
- **WHEN** a pessoa está numa página da navegação principal
- **THEN** o item dessa página fica marcado como o atual, e só ele

#### Scenario: Administração na barra lateral
- **WHEN** o administrador abre a aplicação
- **THEN** a barra lateral mostra também o item Administração, que quem não é administrador não vê

### Requirement: Estrutura no celular

Em telas menores que 900px, a aplicação MUST trocar a barra lateral por uma barra superior com o botão de menu e o título da página; o menu MUST abrir por cima do conteúdo com o nome e o e-mail da pessoa na sessão, a mesma navegação e o item Meu perfil, e fechar ao escolher um item.

#### Scenario: Menu no celular
- **WHEN** a aplicação é aberta numa tela menor que 900px e a pessoa toca no botão de menu
- **THEN** o menu abre com o nome e o e-mail da pessoa, os itens Início, Projetos, Chamados e Minhas horas e o item Meu perfil, e escolher um item leva à página e fecha o menu
