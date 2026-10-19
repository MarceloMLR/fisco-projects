## MODIFIED Requirements

### Requirement: Barra lateral no computador

Em telas de 900px ou mais, a aplicação MUST mostrar uma barra lateral fixa, da altura da janela, com o nome da instalação no topo, depois do logo da instalação ou, sem logo, do selo, e os itens Início, Projetos e Chamados e, só para o administrador, Administração, destacando o item da página atual.

#### Scenario: Navegação principal
- **WHEN** a aplicação é aberta numa tela de 900px ou mais
- **THEN** a barra lateral mostra o nome da instalação e os itens Início, Projetos e Chamados

#### Scenario: Item da página atual
- **WHEN** a pessoa está numa página da navegação principal
- **THEN** o item dessa página fica marcado como o atual, e só ele

#### Scenario: Administração na barra lateral
- **WHEN** o administrador abre a aplicação
- **THEN** a barra lateral mostra também o item Administração, que quem não é administrador não vê
