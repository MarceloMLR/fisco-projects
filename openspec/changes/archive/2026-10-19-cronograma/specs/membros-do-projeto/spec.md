# Spec Delta

## ADDED Requirements

### Requirement: Tarefas de quem sai ou passa a leitor

Remover uma pessoa do projeto, ou trocar o papel dela para leitor, MUST deixar sem responsável as tarefas dela no cronograma, na mesma mudança; as datas e o progresso das tarefas não mudam. A confirmação da remoção pela tela MUST avisar que as tarefas da pessoa ficam sem responsável (ADR-0042, ADR-0043).

#### Scenario: Remover o responsável de tarefas
- **WHEN** o gerente remove do projeto um desenvolvedor responsável por duas tarefas
- **THEN** as duas tarefas ficam sem responsável, com as mesmas datas e o mesmo progresso, e a auditoria tem "Alterou uma tarefa" para cada uma

#### Scenario: Responsável passa a leitor
- **WHEN** o gerente troca para leitor o papel de uma colaboradora responsável por uma tarefa
- **THEN** a tarefa fica sem responsável e a colaboradora continua no projeto como leitora
