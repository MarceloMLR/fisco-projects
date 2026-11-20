## ADDED Requirements

### Requirement: Relatório de status na demonstração

A carga de demonstração MUST gerar, pela gerente, um relatório de status do projeto principal, depois dos chamados ligados ao cronograma, com os quatro textos e o chamado que ameaça um marco como risco. Assim a pasta "Relatórios de status" tem o relatório e o próximo rascunho vem preenchido. Ela MUST gerar só no projeto que ainda não tem relatório (ADR-0089, ADR-0165).

#### Scenario: Relatório de status na demonstração
- **WHEN** o comando carrega o projeto principal
- **THEN** a pasta "Relatórios de status" do projeto tem um relatório da gerente, e o rascunho traz os textos e o risco dele

#### Scenario: Relatório da demonstração uma vez só
- **WHEN** o comando roda de novo numa demonstração já carregada
- **THEN** o projeto principal continua com um relatório só
