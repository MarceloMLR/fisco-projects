## MODIFIED Requirements

### Requirement: Anexos do chamado

Quem manda mensagem MUST poder anexar vários arquivos a ela, e o solicitante, à descrição logo depois de abrir o chamado. Cada arquivo MUST ter até 15 MB (configurável). Executáveis e scripts MUST ser recusados com 400. Os arquivos ficam na pasta do chamado e só quem vê o chamado MUST conseguir abri-los e baixá-los; imagens e PDF abrem no navegador, e MUST poder ser baixados com o nome original. Todos os anexos de um chamado MUST poder ser baixados de uma vez, num `.zip` (ADR-0026, ADR-0030).

#### Scenario: Anexar arquivos a uma mensagem
- **WHEN** alguém manda uma mensagem com uma imagem e um XML
- **THEN** os dois ficam na mensagem e em "Anexos da conversa", e a imagem abre no navegador

#### Scenario: Anexo recusado
- **WHEN** alguém anexa um arquivo acima do limite ou um executável
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Anexo fora do acesso
- **WHEN** alguém que não está no projeto pede um anexo de um chamado dele
- **THEN** a API responde 404

#### Scenario: Baixar um anexo
- **WHEN** quem vê o chamado pede uma imagem anexada como download
- **THEN** a API entrega o arquivo como download, com o nome original

#### Scenario: Baixar todos os anexos
- **WHEN** quem vê o chamado pede todos os anexos de um chamado com dois arquivos de mesmo nome e um PDF
- **THEN** a API entrega um `.zip` com os três, na ordem da lista de anexos, cada um com o nome que tem no chamado

#### Scenario: Baixar todos sem anexos ou sem acesso
- **WHEN** alguém pede todos os anexos de um chamado sem anexos, ou de um chamado que não vê
- **THEN** a API responde 404

## ADDED Requirements

### Requirement: Baixar anexos pela tela

Cada anexo, na conversa e em "Anexos da conversa", MUST ter o ícone "Baixar" ao lado do nome, que continua abrindo o arquivo. "Anexos da conversa" MUST ter "Baixar todos" quando há anexos (ADR-0026).

#### Scenario: Baixar pela tela
- **WHEN** alguém abre um chamado com anexos
- **THEN** cada anexo mostra o nome, que abre o arquivo, e "Baixar", que baixa com o nome original, e "Anexos da conversa" mostra "Baixar todos"

#### Scenario: Sem anexos para baixar
- **WHEN** alguém abre um chamado sem anexos
- **THEN** "Anexos da conversa" diz que não há anexos, sem "Baixar todos"
