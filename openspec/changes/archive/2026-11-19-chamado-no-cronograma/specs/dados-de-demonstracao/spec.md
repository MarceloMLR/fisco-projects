## ADDED Requirements

### Requirement: Chamado no cronograma na demonstração

A carga de demonstração MUST ligar o chamado do projeto principal registrado no fornecedor, ainda em aberto, a uma tarefa da fase de testes de que dependem os marcos da homologação e da virada para produção, por quem atende o chamado, com o evento na conversa (ADR-0089, ADR-0164).

#### Scenario: Chamado no cronograma na demonstração
- **WHEN** o comando carrega os dados de demonstração e alguém abre o chamado registrado no fornecedor do projeto principal
- **THEN** a seção "No cronograma" mostra a tarefa ligada e os dois marcos ameaçados, o cronograma mostra a marca do chamado na tarefa, e a Visão geral mostra o chamado em "Precisa de atenção"
