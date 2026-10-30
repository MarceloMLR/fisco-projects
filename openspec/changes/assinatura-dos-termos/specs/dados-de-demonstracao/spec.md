## MODIFIED Requirements

### Requirement: Termos na demonstração

A carga de demonstração MUST incluir termos de aceite do projeto principal pelo provedor local, com signatários fictícios de fora da empresa: um assinado por dois signatários, um anexado pela gerente, um recusado com o motivo e um pendente com um de dois signatários já assinado, com PDFs fictícios feitos pelo comando (ADR-0002, ADR-0089, ADR-0129).

#### Scenario: Termos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a aba Termos de aceite do ERP-FIN mostra termos assinados, recusado e pendente, a gaveta do pendente mostra um signatário assinado e o link local do outro, e a pasta de termos tem os termos assinados
