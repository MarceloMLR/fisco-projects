# Spec Delta

## MODIFIED Requirements

### Requirement: Situação e exclusão do cliente

O administrador MUST poder inativar e reativar um cliente; um cliente inativo continua na lista, apagado. O administrador MUST poder excluir um cliente sem projetos, depois de confirmar, e o logo dele sai do armazenamento. Um cliente com projetos, mesmo concluídos ou cancelados, MUST NOT ser excluído: a API responde 409 e a tela sugere inativar (ADR-0015).

#### Scenario: Inativar e reativar cliente
- **WHEN** o administrador inativa um cliente e depois o reativa
- **THEN** o cliente aparece como Inativo e depois como Ativo

#### Scenario: Excluir cliente
- **WHEN** o administrador exclui um cliente sem projetos e confirma
- **THEN** o cliente sai da lista e o logo dele é apagado

#### Scenario: Cliente com projetos
- **WHEN** o administrador pede para excluir um cliente que tem um projeto
- **THEN** a API responde 409, o cliente continua na lista e a tela sugere inativar

#### Scenario: Cliente que não existe
- **WHEN** alguém pede para alterar ou excluir um cliente que não existe
- **THEN** a API responde 404

## ADDED Requirements

### Requirement: Clientes para escolher

Qualquer pessoa com sessão MUST poder ler o nome, o logo e a situação de todos os clientes, para escolher o cliente de um projeto e filtrar a lista de projetos. Essa leitura MUST NOT trazer o CNPJ nem a observação (ADR-0015, ADR-0043).

#### Scenario: Clientes para escolher
- **WHEN** um usuário pede os clientes para escolher
- **THEN** a resposta traz o nome, o logo e a situação de cada cliente, em ordem alfabética, sem o CNPJ nem a observação
