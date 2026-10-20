## ADDED Requirements

### Requirement: Linha de base na demonstração

O projeto principal MUST ter a linha de base salva pela gerente antes de uma tarefa ser adiada, para a diferença da linha de base e o caminho crítico aparecerem na demonstração (ADR-0021, ADR-0089).

#### Scenario: Linha de base na demonstração
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a gerente salva a linha de base do ERP-FIN e depois adia "Parametrizar regras de ICMS", que empurra as seguintes e deixa o fim do projeto depois da linha de base
