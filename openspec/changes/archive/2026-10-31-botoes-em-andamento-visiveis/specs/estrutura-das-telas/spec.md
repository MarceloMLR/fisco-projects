## MODIFIED Requirements

### Requirement: Botões em andamento

Todo botão e botão de ícone que manda algo ao servidor MUST ficar em andamento do clique até a resposta: o ícone de carregamento no lugar do ícone, ou antes do texto, o rótulo e a largura mantidos, inativo e ocupado para as tecnologias assistivas. Quando vai ao servidor, o andamento MUST durar pelo menos meio segundo (ADR-0132). Um novo clique ou Enter durante o andamento MUST NOT mandar a ação de novo. Os itens dos menus ficam fora (ADR-0131).

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

#### Scenario: Resposta rápida
- **WHEN** a resposta do servidor chega antes de meio segundo
- **THEN** o botão continua em andamento até completar meio segundo do clique, sem aceitar novo clique, e só então volta ao normal

#### Scenario: Formulário recusado sem ir ao servidor não fica em andamento
- **WHEN** a pessoa envia um formulário que as regras da própria tela recusam, sem ir ao servidor
- **THEN** o botão volta ao normal na hora, os erros aparecem e um novo clique funciona

#### Scenario: Envio de arquivos em andamento
- **WHEN** a pessoa envia arquivos na aba Documentos
- **THEN** "Enviar arquivos" fica em andamento até o último arquivo, dizendo quantos já foram, sem ficar cinza

#### Scenario: Planilha das horas em andamento
- **WHEN** o administrador escolhe "Baixar planilha" em Horas dos projetos
- **THEN** o botão fica em andamento enquanto a planilha é montada, sem ficar cinza
