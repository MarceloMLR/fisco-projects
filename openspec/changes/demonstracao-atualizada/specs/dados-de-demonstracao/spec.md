## ADDED Requirements

### Requirement: Novidades fora do projeto principal

A carga de demonstração MUST mostrar as horas apontadas e previstas, a carga da equipe, as ausências, as datas-limite, as notas e os tipos de dependência também fora do projeto principal, em projetos em andamento das duas gerentes, com as mesmas regras que a API confere (ADR-0089, ADR-0128, ADR-0137, ADR-0138, ADR-0139, ADR-0141, ADR-0142).

#### Scenario: Horas de todas as pessoas
- **WHEN** o comando carrega os dados de demonstração e cada pessoa que trabalha nos projetos abre Minhas horas
- **THEN** a semana atual e a anterior mostram horas dela, e a segunda gerente e o líder técnico têm horas em mais de um projeto

#### Scenario: Horas das tarefas da segunda gerente
- **WHEN** o comando carrega os dados de demonstração e Beatriz Lima abre a Lista do TMS-DELTA
- **THEN** a coluna Horas mostra uma tarefa acima das previstas, uma perto e uma dentro

#### Scenario: Carga da equipe com todos os estados
- **WHEN** o comando carrega os dados de demonstração e o administrador abre Carga da equipe
- **THEN** as oito semanas mostram uma pessoa acima da capacidade, uma perto do limite, pessoas dentro e pessoas livres, com ausências de pelo menos quatro pessoas

#### Scenario: Ausências em outros projetos
- **WHEN** o comando registra as ausências do plano
- **THEN** cada uma é registrada pela gerente do projeto, para uma pessoa dele, nenhuma se cruza com outra da mesma pessoa, e uma cai numa tarefa em aberto da pessoa fora do projeto principal

#### Scenario: Datas-limite, notas e dependências em outros projetos
- **WHEN** o comando carrega os dados de demonstração e alguém abre o cronograma de outro projeto em andamento
- **THEN** há datas-limite em mais de um projeto, uma delas estourada, notas em mais de um projeto e uma dependência "começa junto" fora do projeto principal
