## REMOVED Requirements

### Requirement: Tema automático

**Reason**: O tema passa a ser escolhido e salvo no perfil (ADR-0014), com o automático como padrão. O comportamento de seguir o sistema operacional continua, agora no requisito "Tema no perfil" da capacidade `perfil`.

**Migration**: O cenário "Tema segue o sistema" está em "Tema no perfil", com o mesmo nome.

## MODIFIED Requirements

### Requirement: Cabeçalho com a trilha

Sobre o conteúdo, a aplicação MUST mostrar um cabeçalho da mesma cor do conteúdo e sem linha, com a trilha da página, cujos níveis acima levam às áreas em que a página fica e cujo último nível é a página atual, e, à direita, o avatar da pessoa na sessão, que abre o menu da conta. O menu da conta MUST mostrar o nome e o e-mail da pessoa, o item Meu perfil, a escolha do tema (claro, escuro e automático) e a opção Sair.

#### Scenario: Trilha da página
- **WHEN** a pessoa abre uma página
- **THEN** o cabeçalho mostra a trilha com o nome da página como último nível, marcado como a página atual

#### Scenario: Trilha de uma página dentro de uma área
- **WHEN** o administrador abre a tela Usuários
- **THEN** a trilha mostra Administração e Usuários, e Administração leva à área de Administração

#### Scenario: Conta no cabeçalho
- **WHEN** a pessoa abre o menu do avatar no cabeçalho
- **THEN** o menu mostra o nome e o e-mail dela, Meu perfil, o tema escolhido entre claro, escuro e automático e a opção Sair, e o avatar traz as iniciais do nome

### Requirement: Estrutura no celular

Em telas menores que 900px, a aplicação MUST trocar a barra lateral por uma barra superior com o botão de menu e o título da página; o menu MUST abrir por cima do conteúdo com o nome e o e-mail da pessoa na sessão, a mesma navegação e o item Meu perfil, e fechar ao escolher um item.

#### Scenario: Menu no celular
- **WHEN** a aplicação é aberta numa tela menor que 900px e a pessoa toca no botão de menu
- **THEN** o menu abre com o nome e o e-mail da pessoa, os itens Início, Projetos e Chamados e o item Meu perfil, e escolher um item leva à página e fecha o menu
