## ADDED Requirements

### Requirement: Conclusão com chamados

O projeto MUST ser concluído só com todos os seus chamados fechados: qualquer chamado em outro status, inclusive Resolvido, MUST impedir a conclusão com 409 e o número de chamados não fechados. Concluir com tarefas abaixo de 100% MUST ser permitido (ADR-0043).

#### Scenario: Concluir com chamado não fechado
- **WHEN** o gerente conclui um projeto com um chamado Resolvido
- **THEN** a API responde 409 com o número de chamados não fechados, e o projeto continua no mesmo status

#### Scenario: Concluir com os chamados fechados
- **WHEN** o gerente conclui um projeto cujos chamados estão todos fechados
- **THEN** o projeto fica concluído

### Requirement: Cancelamento com chamados

Ao cancelar o projeto, os chamados que não estão fechados MUST ser fechados, cada um com o motivo "projeto cancelado" na conversa e quem cancelou (ADR-0043).

#### Scenario: Cancelar com chamados abertos
- **WHEN** o gerente cancela um projeto com dois chamados abertos
- **THEN** o projeto fica cancelado, e os dois chamados ficam Fechados, cada um com o evento "projeto cancelado" na conversa

### Requirement: Resumo do encerramento

Quem muda o status do projeto MUST poder ver, antes de concluir ou cancelar, quantos chamados não estão fechados e quantas tarefas estão abaixo de 100%; as outras pessoas do projeto MUST receber 403 (ADR-0043).

#### Scenario: Resumo do encerramento
- **WHEN** o gerente pede o resumo do encerramento de um projeto com um chamado aberto e uma tarefa em 40%
- **THEN** a API responde com um chamado não fechado e uma tarefa abaixo de 100%

#### Scenario: Resumo só de quem muda o status
- **WHEN** um colaborador do projeto pede o resumo do encerramento
- **THEN** a API responde 403

### Requirement: Encerramento pela tela

"Concluir projeto" MUST mostrar, antes de confirmar, os chamados não fechados, que impedem a conclusão, com o link para a aba Chamados do projeto e sem deixar confirmar, e as tarefas abaixo de 100% como aviso, que deixa confirmar. "Cancelar projeto" MUST dizer quantos chamados serão fechados (ADR-0043, ADR-0065).

#### Scenario: Conclusão impedida pela tela
- **WHEN** o gerente escolhe "Concluir projeto" num projeto com chamados não fechados
- **THEN** o diálogo diz quantos são, mostra o link para os chamados e não deixa concluir

#### Scenario: Aviso de tarefas incompletas
- **WHEN** o gerente escolhe "Concluir projeto" num projeto sem chamados abertos e com tarefas abaixo de 100%
- **THEN** o diálogo avisa quantas são e deixa concluir

#### Scenario: Cancelamento pela tela
- **WHEN** o gerente escolhe "Cancelar projeto" num projeto com chamados abertos e confirma
- **THEN** o diálogo diz quantos chamados serão fechados, e o projeto fica cancelado
