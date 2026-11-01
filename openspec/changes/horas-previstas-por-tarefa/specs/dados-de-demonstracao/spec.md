## ADDED Requirements

### Requirement: Horas previstas na demonstração

A carga de demonstração MUST pôr, pelo administrador, horas previstas em tarefas do projeto principal com horas apontadas, de modo que o cronograma e Horas dos projetos mostrem uma tarefa acima das previstas, uma perto das previstas e uma dentro (ADR-0089, ADR-0138).

#### Scenario: Horas previstas na demonstração
- **WHEN** o comando carrega os dados de demonstração e o administrador abre o detalhe do projeto principal em Horas dos projetos
- **THEN** a seção "Por tarefa" mostra uma tarefa acima das previstas, uma perto e uma dentro
