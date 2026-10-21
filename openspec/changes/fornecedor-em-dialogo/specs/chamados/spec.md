## MODIFIED Requirements

### Requirement: Fornecedores pela tela

O painel do chamado MUST ter a seção "No fornecedor", com cada registro numa linha (fornecedor, número, status por ícone, cor e texto, e o destaque de parado), que abre o diálogo do registro com os dados e o histórico de status, e "Registrar" e, no diálogo, "Atualizar" para quem atende. A página Chamados MUST ter a visão "Com fornecedor", com a contagem, e a lista MUST mostrar o fornecedor de cada chamado. A Administração MUST ter "Fornecedores" (ADR-0028, ADR-0065).

#### Scenario: Registrar pela tela
- **WHEN** quem atende escolhe "Registrar" em "No fornecedor", preenche o fornecedor e o número e confirma
- **THEN** a seção mostra o registro, e a conversa, o evento

#### Scenario: Registro pela tela
- **WHEN** alguém escolhe um registro em "No fornecedor", depois de duas mudanças de status
- **THEN** o diálogo mostra o link, a data de abertura no fornecedor, quem registrou, a observação e o histórico com o registro e as duas mudanças, cada uma com quem fez e a anotação, sem "Atualizar" para quem não atende

#### Scenario: Atualizar pelo registro
- **WHEN** quem atende escolhe "Atualizar" no diálogo do registro, muda o status e confirma
- **THEN** a seção mostra o novo status

#### Scenario: Com fornecedor
- **WHEN** alguém escolhe a visão "Com fornecedor"
- **THEN** a lista traz só os chamados com registro em fornecedor

#### Scenario: Fornecedores na Administração
- **WHEN** o administrador inclui um fornecedor em "Fornecedores"
- **THEN** a lista mostra o fornecedor ativo, com o portal
