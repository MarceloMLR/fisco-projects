## MODIFIED Requirements

### Requirement: Lembrar e cancelar

O administrador e o gerente MUST lembrar os signatários de um pedido pendente, o que pede ao provedor o link de novo para quem ainda não assinou e entra no histórico, e, quando o provedor recusa o lembrete por ter mandado o link há pouco, MUST responder 409 sem registrar o lembrete; MUST cancelar um pedido pendente, o que cancela o documento no provedor e tira o pedido da lista, ficando só na auditoria (ADR-0095, ADR-0129, ADR-0130).

#### Scenario: Lembrar a pessoa
- **WHEN** a gerente escolhe "Lembrar" num pedido com um signatário pendente
- **THEN** o provedor manda o link de novo só a ele, e o histórico registra o lembrete

#### Scenario: Lembrete logo depois do envio
- **WHEN** a gerente escolhe "Lembrar signatários" e o provedor recusa porque mandou o link há pouco
- **THEN** a API responde 409, o histórico não registra o lembrete, e a tela diz que o provedor mandou o link há pouco e pede para lembrar de novo mais tarde

#### Scenario: Cancelar pedido
- **WHEN** a gerente cancela um pedido pendente
- **THEN** o documento é cancelado no provedor, o pedido sai da lista, e a auditoria registra a exclusão

#### Scenario: Cancelar pedido respondido
- **WHEN** a gerente tenta cancelar um pedido assinado
- **THEN** a API responde 409
