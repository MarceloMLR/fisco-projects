## ADDED Requirements

### Requirement: Retenção das ausências

A ausência de uma pessoa MUST ser apagada sozinha, numa limpeza em segundo plano, um ano depois do fim, no fuso da instalação, e MUST ser apagada na anonimização da pessoa (ADR-0058, ADR-0110, ADR-0141).

#### Scenario: Ausência apagada depois de um ano
- **WHEN** a limpeza roda com uma ausência que terminou há mais de um ano e outra que terminou há onze meses
- **THEN** a primeira é apagada e a segunda fica

#### Scenario: Ausências na anonimização
- **WHEN** o administrador anonimiza uma pessoa com ausências
- **THEN** as ausências dela são apagadas na mesma transação
