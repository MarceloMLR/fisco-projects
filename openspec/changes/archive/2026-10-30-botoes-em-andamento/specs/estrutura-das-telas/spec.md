## ADDED Requirements

### Requirement: Botões em andamento

Todo botão e botão de ícone que manda algo ao servidor MUST ficar em andamento do clique até a resposta, com sucesso ou erro: o ícone de carregamento no lugar do ícone do botão, ou antes do texto quando não há ícone, o rótulo e a largura mantidos, inativo e marcado como ocupado para as tecnologias assistivas. Durante o andamento, um novo clique ou um novo Enter MUST NOT mandar a ação de novo. Os itens dos menus ficam fora (ADR-0131).

#### Scenario: Botão em andamento
- **WHEN** a pessoa clica num botão com ícone que manda algo ao servidor, e a resposta demora
- **THEN** o ícone dá lugar ao ícone de carregamento na hora, o rótulo continua o mesmo, e o botão fica inativo até a resposta

#### Scenario: Botão sem ícone em andamento
- **WHEN** a pessoa clica num botão sem ícone, e a resposta demora
- **THEN** o ícone de carregamento aparece antes do texto até a resposta

#### Scenario: Botão de ícone em andamento
- **WHEN** a pessoa clica num botão de ícone que manda algo ao servidor
- **THEN** o ícone dá lugar ao ícone de carregamento até a resposta

#### Scenario: Clique repetido durante o andamento
- **WHEN** a pessoa clica de novo num botão em andamento
- **THEN** nada acontece, e a ação vai ao servidor uma vez só

#### Scenario: Formulário em andamento
- **WHEN** a pessoa envia um formulário ou um diálogo, pelo botão ou pelo Enter, e a gravação demora
- **THEN** o botão de envio fica em andamento até a resposta, e um novo envio não manda a gravação de novo

#### Scenario: Fim do andamento com erro
- **WHEN** a resposta é um erro
- **THEN** o botão volta ao normal, com o ícone, e a tela mostra o erro como hoje
