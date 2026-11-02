## MODIFIED Requirements

### Requirement: Documentos na demonstração

A carga de demonstração MUST enviar documentos fictícios para as pastas do projeto principal, por quem os enviaria na tela, com uma nova versão, status diferentes, um documento obsoleto e vínculos com tarefas do cronograma, e MUST deixar "01 Gestão do projeto" do projeto principal vista só pela gestão e pelo líder técnico (ADR-0002, ADR-0031, ADR-0089, ADR-0148).

#### Scenario: Documentos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a aba Documentos do ERP-FIN mostra documentos em mais de uma pasta, um deles na versão 2, aprovados, em revisão e em rascunho, vinculados a tarefas, e um obsoleto que só aparece com o filtro "Todos"

#### Scenario: Pasta da gestão escondida na demonstração
- **WHEN** o comando carrega os dados de demonstração e uma colaboradora do ERP-FIN abre a aba Documentos
- **THEN** a pasta "01 Gestão do projeto" não aparece para ela, e aparece com a marca de restrita para a gerente
