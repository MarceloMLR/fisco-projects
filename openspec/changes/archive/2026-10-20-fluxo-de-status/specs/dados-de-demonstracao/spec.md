## ADDED Requirements

### Requirement: Status na demonstração

A carga de demonstração MUST deixar chamados de exemplo em status diferentes, com o responsável definido pela primeira resposta de quem atende, mudando o status pelas ações do fluxo, cada uma pela pessoa que pode (ADR-0024, ADR-0089).

#### Scenario: Status na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página Chamados mostra chamados Abertos, Em andamento, Aguardando solicitante e Resolvidos, com e sem responsável
