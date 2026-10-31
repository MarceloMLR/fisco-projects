## ADDED Requirements

### Requirement: Projeto importado na demonstração

A carga de demonstração MUST criar um projeto fictício a partir do XML do MS Project que a exportação grava de outro projeto da demonstração, pela prévia e pela criação da API (ADR-0089, ADR-0135).

#### Scenario: Projeto importado na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a gerente exporta o cronograma de um projeto, cria um projeto novo a partir do arquivo, ligando os recursos às pessoas da demonstração, e ele nasce com as mesmas fases, itens, progresso e responsáveis
