## ADDED Requirements

### Requirement: Prazos na demonstração

A carga de demonstração MUST deixar chamados com prazos em dia, vencendo e pausados. Os chamados abrem no dia da carga, pela própria API, então nenhum nasce vencido (ADR-0025, ADR-0089).

#### Scenario: Prazos na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página Chamados mostra prazos em dia, vencendo e pausados
