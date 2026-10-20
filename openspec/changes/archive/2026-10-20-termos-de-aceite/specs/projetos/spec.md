## MODIFIED Requirements

### Requirement: Resumo do encerramento

Quem muda o status do projeto MUST poder ver, antes de concluir ou cancelar, quantos chamados não estão fechados, quantas tarefas estão abaixo de 100% e quantos pedidos de aceite estão pendentes; as outras pessoas do projeto MUST receber 403 (ADR-0043).

#### Scenario: Resumo do encerramento
- **WHEN** o gerente pede o resumo do encerramento de um projeto com um chamado aberto, uma tarefa em 40% e um pedido de aceite pendente
- **THEN** a API responde com um chamado não fechado, uma tarefa abaixo de 100% e um aceite pendente

#### Scenario: Resumo só de quem muda o status
- **WHEN** um colaborador do projeto pede o resumo do encerramento
- **THEN** a API responde 403

### Requirement: Encerramento pela tela

"Concluir projeto" MUST mostrar, antes de confirmar, os chamados não fechados, que impedem a conclusão, com o link para a aba Chamados do projeto e sem deixar confirmar, e as tarefas abaixo de 100% e os pedidos de aceite pendentes como aviso, que deixa confirmar. "Cancelar projeto" MUST dizer quantos chamados serão fechados (ADR-0043, ADR-0065).

#### Scenario: Conclusão impedida pela tela
- **WHEN** o gerente escolhe "Concluir projeto" num projeto com chamados não fechados
- **THEN** o diálogo diz quantos são, mostra o link para os chamados e não deixa concluir

#### Scenario: Aviso de tarefas incompletas
- **WHEN** o gerente escolhe "Concluir projeto" num projeto sem chamados abertos e com tarefas abaixo de 100%
- **THEN** o diálogo avisa quantas são e deixa concluir

#### Scenario: Aviso de aceites pendentes
- **WHEN** o gerente escolhe "Concluir projeto" num projeto sem chamados abertos e com um pedido de aceite pendente
- **THEN** o diálogo avisa o aceite pendente e deixa concluir

#### Scenario: Cancelamento pela tela
- **WHEN** o gerente escolhe "Cancelar projeto" num projeto com chamados abertos e confirma
- **THEN** o diálogo diz quantos chamados serão fechados, e o projeto fica cancelado
