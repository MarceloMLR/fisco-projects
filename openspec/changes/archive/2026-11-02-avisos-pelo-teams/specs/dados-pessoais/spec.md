## ADDED Requirements

### Requirement: Avisos do Teams na anonimização

Na mesma transação da anonimização, os avisos do Teams para a pessoa MUST ser apagados da fila, mesmo os que esperavam envio, e, nos avisos de outras pessoas, o nome dela MUST virar o identificador (ADR-0058, ADR-0110, ADR-0149).

#### Scenario: Aviso do Teams esperando envio
- **WHEN** a pessoa anonimizada tinha um aviso do Teams esperando envio
- **THEN** o aviso sai da fila e não é enviado

#### Scenario: Nome em avisos de outras pessoas
- **WHEN** a pessoa anonimizada tinha atribuído um chamado avisado pelo Teams a outra pessoa
- **THEN** o aviso guardado da outra pessoa traz o identificador no lugar do nome
