## ADDED Requirements

### Requirement: Termos na demonstração

A carga de demonstração MUST incluir termos de aceite do projeto principal: um assinado pela pessoa do pedido, um anexado pela gerente, um recusado e um pendente, com PDFs fictícios feitos pelo comando (ADR-0002, ADR-0035, ADR-0089).

#### Scenario: Termos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a aba Termos de aceite do ERP-FIN mostra termos assinados, recusado e pendente, e a pasta de termos tem os termos assinados
