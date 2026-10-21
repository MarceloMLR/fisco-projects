## ADDED Requirements

### Requirement: Atendimento iniciado na demonstração

A carga de demonstração MUST levar um chamado sem responsável à triagem e depois ao atendimento pelas ações do fluxo, por quem atende o time, que passa a ser o responsável (ADR-0089, ADR-0098).

#### Scenario: Atendimento iniciado na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** um chamado aparece Em andamento, com quem iniciou o atendimento como responsável e o evento na conversa
