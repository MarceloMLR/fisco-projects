## ADDED Requirements

### Requirement: Menções dos comentários na anonimização

Na mesma transação da anonimização, as menções da pessoa nos comentários das tarefas e dos marcos MUST passar a dizer o identificador, no texto formatado e no texto simples, e os comentários dela MUST continuar na seção, com o autor pelo nome atual (ADR-0110, ADR-0157).

#### Scenario: Menção no comentário anonimizada
- **WHEN** uma pessoa mencionada num comentário é anonimizada
- **THEN** o comentário mostra o identificador no lugar do nome, e nenhuma coluna dos comentários tem o nome dela
