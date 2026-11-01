## ADDED Requirements

### Requirement: Carga na demonstração

A carga de demonstração MUST incluir horas previstas em tarefas de mais de um projeto de uma mesma pessoa, de modo que a Carga da equipe mostre uma pessoa acima da capacidade numa semana de ausência dela (ADR-0089, ADR-0142).

#### Scenario: Carga na demonstração
- **WHEN** o comando carrega os dados de demonstração e o administrador abre Carga da equipe
- **THEN** pelo menos uma pessoa está acima da capacidade numa semana com ausência, com tarefas de dois projetos
