## ADDED Requirements

### Requirement: Movimento dos diálogos

Ao abrir, o diálogo MUST esmaecer o fundo e crescer de 98% subindo 8px em 200 ms, com a curva `cubic-bezier(0.2, 0, 0, 1)`. Ao fechar, MUST fazer o caminho de volta em 150 ms, com a curva `cubic-bezier(0.4, 0, 1, 1)`, e com o mesmo conteúdo até o fim da saída, também quando a tela o tira de uma vez (ADR-0133).

#### Scenario: Diálogo que cresce ao abrir
- **WHEN** a pessoa abre um diálogo
- **THEN** o fundo esmaece e o diálogo cresce de 98% subindo 8px em 200 ms

#### Scenario: Diálogo até o fim da saída
- **WHEN** a pessoa fecha um diálogo
- **THEN** ele faz o caminho de volta em 150 ms, com o mesmo conteúdo, e só então sai da tela

#### Scenario: Diálogo tirado da tela
- **WHEN** a tela tira de uma vez um diálogo aberto, como depois de salvar
- **THEN** o diálogo ainda sai esmaecendo em 150 ms, sem receber clique

### Requirement: Movimento da gaveta

A gaveta dos detalhes MUST deslizar da direita ao abrir, em 250 ms, e de volta ao fechar, em 200 ms, com o fundo esmaecendo e o mesmo conteúdo até o fim da saída (ADR-0133).

#### Scenario: Gaveta desliza nos dois sentidos
- **WHEN** a pessoa abre e depois fecha os detalhes de um item
- **THEN** a gaveta desliza da direita ao abrir e de volta ao fechar, com o conteúdo até o fim

### Requirement: Movimento da troca de página

Na troca de página, só o conteúdo da página, a aba do projeto ou a seção da Administração MUST esmaecer subindo 4px em 200 ms; a barra lateral, o cabeçalho, o cabeçalho do projeto e as seções da Administração MUST ficar parados, e a página anterior MUST sair na hora. Mudar só a busca do endereço MUST NOT animar a página (ADR-0133).

#### Scenario: Página que entra subindo
- **WHEN** a pessoa vai de Projetos para Chamados
- **THEN** o conteúdo de Chamados esmaece subindo 4px, e a barra lateral e o cabeçalho ficam parados

#### Scenario: Aba que entra subindo
- **WHEN** a pessoa troca de aba num projeto
- **THEN** só a aba esmaece subindo, e o cabeçalho do projeto fica parado

#### Scenario: Endereço com a mesma página
- **WHEN** só a busca do endereço muda, como ao abrir os detalhes de um item
- **THEN** a página não anima de novo

### Requirement: Movimento reduzido

Com "reduzir movimento" ligado no sistema, nada MUST deslizar nem crescer: o aviso, o diálogo, a gaveta e a página MUST só esmaecer, em 150 ms (ADR-0133).

#### Scenario: Janelas com movimento reduzido
- **WHEN** a pessoa pediu ao sistema para reduzir o movimento
- **THEN** o diálogo, a gaveta e a página só esmaecem, em 150 ms, sem crescer nem deslizar
