## ADDED Requirements

### Requirement: Acessos na demonstração

A carga de demonstração MUST incluir acessos fictícios do projeto principal em três ambientes, cada um incluído por uma pessoa do projeto que cadastra acessos, com senhas fictícias (ADR-0002, ADR-0034, ADR-0089).

#### Scenario: Acessos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a aba Acessos do ERP-FIN mostra acessos em Produção, Homologação e Fornecedores, com as senhas mascaradas
