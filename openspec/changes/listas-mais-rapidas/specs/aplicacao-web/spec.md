## ADDED Requirements

### Requirement: Dados da API guardados durante a sessão

As respostas da API que as telas leem MUST ficar guardadas só na memória da página, durante a sessão:

- Ao voltar a uma tela, filtro ou página já vista, a lista MUST aparecer na hora com os dados guardados e MUST ser atualizada com a API em seguida.
- Uma alteração feita pela pessoa MUST pedir de novo à API as listas que mostram o item alterado.
- Os dados guardados MUST ser descartados quando a pessoa sai ou a sessão termina.
- Nada deles MUST ir para o armazenamento do navegador.

#### Scenario: Volta a uma lista já vista
- **WHEN** a pessoa volta a uma lista, ou a um filtro dela, que já abriu na sessão
- **THEN** as linhas da última vez aparecem na hora, sem indicador de carregamento, e a lista é pedida de novo à API para se atualizar

#### Scenario: Alteração atualiza as listas
- **WHEN** o administrador troca o papel de uma pessoa, a desativa ou a reativa
- **THEN** a lista de usuários e a contagem da Administração são pedidas de novo à API, e a Auditoria, aberta em seguida, mostra o registro da alteração

#### Scenario: Saída descarta os dados guardados
- **WHEN** a pessoa sai, ou a sessão termina, e alguém entra em seguida no mesmo navegador
- **THEN** as listas são pedidas de novo à API, e nada da sessão anterior aparece

#### Scenario: Dados só na memória
- **WHEN** uma lista é aberta
- **THEN** nada dos dados é gravado no `localStorage` nem no `sessionStorage` do navegador

### Requirement: Espera nas listas

Ao trocar o filtro ou a página de uma lista, a tabela MUST continuar mostrando as linhas atuais até a resposta chegar. O indicador de carregamento MUST aparecer só se a espera passar de 300 ms. Na primeira abertura da lista, a tabela MUST mostrar o cabeçalho e, só depois de 300 ms de espera, o esqueleto das linhas. A mensagem de lista vazia MUST aparecer só depois da resposta.

#### Scenario: Resposta rápida sem indicador
- **WHEN** a pessoa troca o filtro de uma lista e a resposta chega em menos de 300 ms
- **THEN** as linhas anteriores ficam na tabela até a resposta, e a lista troca para as novas sem mostrar indicador de carregamento

#### Scenario: Resposta demorada com indicador
- **WHEN** a pessoa troca o filtro de uma lista e a resposta demora mais de 300 ms
- **THEN** a tabela mostra uma barra de progresso no topo, sem esconder as linhas anteriores, até a resposta chegar

#### Scenario: Primeira abertura da lista
- **WHEN** a pessoa abre uma lista pela primeira vez na sessão
- **THEN** a tabela mostra o cabeçalho sem a mensagem de lista vazia e, se a espera passar de 300 ms, o esqueleto das linhas

### Requirement: Menus fecham na hora

Ao escolher uma opção num menu ou num campo de seleção, o menu MUST sumir na hora, sem animação de saída, para que a tela atrás dele fique visível. A abertura MAY continuar animada.

#### Scenario: Escolha num campo de seleção
- **WHEN** a pessoa escolhe uma opção no filtro de uma lista
- **THEN** o menu de opções sai da tela no mesmo instante

#### Scenario: Escolha num menu de ações
- **WHEN** o administrador escolhe uma ação no menu de uma pessoa, na tela Usuários
- **THEN** o menu sai da tela no mesmo instante
