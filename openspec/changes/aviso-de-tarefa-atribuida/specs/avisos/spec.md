## ADDED Requirements

### Requirement: Aviso de tarefa atribuída

Quem passa a ser responsável por uma tarefa, ao incluir a tarefa com responsável ou ao trocar o responsável, MUST receber na hora o aviso de tarefa atribuída, com quem atribuiu, o nome da tarefa, o projeto, o fim e o link que abre a tarefa no cronograma, pelos canais dos avisos na hora: o e-mail e o Teams conforme a escolha da pessoa, e o sino sempre. Valem as regras de quem recebe: quem se atribui a tarefa MUST NOT receber (ADR-0037, ADR-0151, ADR-0156).

#### Scenario: Tarefa atribuída
- **WHEN** a gerente inclui uma tarefa com a Carla como responsável
- **THEN** a Carla recebe o e-mail da tarefa atribuída, com o link da tarefa, e a notificação no sino

#### Scenario: Responsável trocado
- **WHEN** a gerente troca o responsável de uma tarefa da Carla para o Diego
- **THEN** o Diego recebe o aviso, e a Carla não

#### Scenario: Quem se atribui não recebe
- **WHEN** a gerente inclui uma tarefa com ela mesma como responsável
- **THEN** ninguém recebe o aviso de tarefa atribuída

#### Scenario: Tarefa atribuída no Teams
- **WHEN** a gerente passa uma tarefa para uma pessoa que tem o Teams ligado
- **THEN** a pessoa recebe na aba Atividade o aviso da tarefa atribuída

### Requirement: Tarefa sem aviso de atribuição

O aviso de tarefa atribuída MUST NOT sair quando o responsável é o cliente do projeto, para quem deixou de ser responsável, no desfazer e no refazer, e nas tarefas criadas com o projeto por um modelo ou pelo MS Project (ADR-0156).

#### Scenario: Cliente como responsável
- **WHEN** a gerente passa uma tarefa para o cliente do projeto
- **THEN** nenhum aviso de tarefa atribuída sai

#### Scenario: Desfazer a troca do responsável
- **WHEN** a gerente desfaz a troca do responsável de uma tarefa
- **THEN** quem volta a ser o responsável não recebe o aviso de novo

#### Scenario: Projeto do MS Project
- **WHEN** a gerente cria um projeto pelo XML do MS Project, com responsáveis nas tarefas
- **THEN** nenhum aviso de tarefa atribuída sai
