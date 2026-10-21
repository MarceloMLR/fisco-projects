## ADDED Requirements

### Requirement: Meu trabalho na demonstração

A carga de demonstração MUST deixar itens na fila das pessoas da demonstração, com datas relativas ao dia da carga: tarefas atrasadas, de hoje e da semana, chamados aguardando o solicitante e sem responsável, e um pedido de aceite pendente (ADR-0070, ADR-0089).

#### Scenario: Meu trabalho na demonstração
- **WHEN** o comando carrega os dados de demonstração e Marina Costa entra
- **THEN** o Meu trabalho de Marina mostra itens em Atrasado, Aguardando você e nos próximos dias, os projetos dela e os próximos marcos
