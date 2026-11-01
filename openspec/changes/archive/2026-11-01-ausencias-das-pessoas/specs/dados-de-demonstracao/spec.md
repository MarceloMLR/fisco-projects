## ADDED Requirements

### Requirement: Ausências na demonstração

A carga de demonstração MUST incluir, pela gerente, ausências de pessoas do projeto principal, uma delas durante uma tarefa em aberto do responsável (ADR-0089, ADR-0141).

#### Scenario: Ausências na demonstração
- **WHEN** o comando carrega os dados de demonstração e alguém abre o cronograma do projeto principal
- **THEN** pelo menos uma tarefa mostra o aviso do responsável ausente, e Membros mostra a ausência
