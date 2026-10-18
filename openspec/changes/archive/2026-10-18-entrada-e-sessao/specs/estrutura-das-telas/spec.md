# Spec Delta

## MODIFIED Requirements

### Requirement: Cabeçalho com a trilha

Sobre o conteúdo, a aplicação MUST mostrar um cabeçalho da mesma cor do conteúdo e sem linha, com a trilha da página, cujo último nível é a página atual, e, à direita, o avatar da pessoa na sessão, que abre o menu da conta.

#### Scenario: Trilha da página
- **WHEN** a pessoa abre uma página
- **THEN** o cabeçalho mostra a trilha com o nome da página como último nível, marcado como a página atual

#### Scenario: Conta no cabeçalho
- **WHEN** a pessoa abre o menu do avatar no cabeçalho
- **THEN** o menu mostra o nome e o e-mail dela e a opção Sair, e o avatar traz as iniciais do nome

### Requirement: Estrutura no celular

Em telas menores que 900px, a aplicação MUST trocar a barra lateral por uma barra superior com o botão de menu e o título da página; o menu MUST abrir por cima do conteúdo com o nome e o e-mail da pessoa na sessão e a mesma navegação, e fechar ao escolher um item.

#### Scenario: Menu no celular
- **WHEN** a aplicação é aberta numa tela menor que 900px e a pessoa toca no botão de menu
- **THEN** o menu abre com o nome e o e-mail da pessoa e os itens Início, Projetos e Chamados, e escolher um item leva à página e fecha o menu
