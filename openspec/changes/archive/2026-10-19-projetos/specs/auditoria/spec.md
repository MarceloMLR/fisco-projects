# Spec Delta

## ADDED Requirements

### Requirement: Referências a outros itens

Um campo que aponta para outro item, como o cliente de um projeto ou a pessoa incluída num projeto, MUST ser gravado com o identificador e o tipo do item, sem o nome. A tela de Auditoria MUST mostrar o nome atual do item, pedido ao módulo dono do tipo, no campo e no rótulo do registro. Um item que não existe mais MUST aparecer como "—" (ADR-0084).

#### Scenario: Referência pelo nome
- **WHEN** a auditoria tem a troca do cliente de um projeto
- **THEN** a tela mostra o nome do cliente antes e depois, e não o identificador

#### Scenario: Nome atual na referência
- **WHEN** o item de uma referência muda de nome depois do registro
- **THEN** a tela mostra o nome atual

#### Scenario: Item que não existe mais
- **WHEN** o item de uma referência foi excluído
- **THEN** a tela mostra "—" no lugar do nome
