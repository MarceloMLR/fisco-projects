## ADDED Requirements

### Requirement: Documentos na demonstração

A carga de demonstração MUST enviar documentos fictícios para as pastas do projeto principal, por quem os enviaria na tela, com uma nova versão, status diferentes, um documento obsoleto e vínculos com tarefas do cronograma (ADR-0002, ADR-0031, ADR-0089).

#### Scenario: Documentos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a aba Documentos do ERP-FIN mostra documentos em mais de uma pasta, um deles na versão 2, aprovados, em revisão e em rascunho, vinculados a tarefas, e um obsoleto que só aparece com o filtro "Todos"
