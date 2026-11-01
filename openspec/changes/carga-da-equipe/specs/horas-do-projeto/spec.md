## ADDED Requirements

### Requirement: Carga da equipe

O sistema MUST dar ao administrador a carga de cada pessoa em oito semanas, de segunda a domingo, a partir da semana pedida (a atual, sem pedido), entre os projetos não concluídos nem cancelados (ADR-0142). A carga da semana MUST somar, em cada dia útil dela, as horas previstas de cada tarefa abaixo de 100% da pessoa divididas pelos dias úteis da tarefa, e trazer as tarefas que pesam nela.

#### Scenario: Tarefa espalhada pelas semanas
- **WHEN** a Carla é responsável por uma tarefa de 40 horas previstas de quinta, 22/10, a quarta, 28/10, em 5 dias úteis
- **THEN** a semana de 19/10 tem 16 horas dela e a de 26/10, 24 horas, cada uma com a tarefa e as horas na semana

#### Scenario: Carga sobre um feriado
- **WHEN** uma tarefa de 24 horas previstas vai de 30/10 a 04/11, com o feriado de 02/11 no meio
- **THEN** cada um dos 3 dias úteis pesa 8 horas, e o feriado não pesa

#### Scenario: Carga entre projetos
- **WHEN** o Diego tem tarefas com horas previstas em dois projetos na mesma semana
- **THEN** a carga da semana dele soma as duas, cada uma com o projeto

#### Scenario: O que não pesa
- **WHEN** a pessoa tem uma tarefa a 100%, uma tarefa sem horas previstas e uma tarefa de um projeto concluído, e o cliente é responsável por outra
- **THEN** nenhuma delas pesa na carga

### Requirement: Capacidade da semana

A capacidade de uma pessoa numa semana MUST ser os dias úteis da semana menos os dias úteis em que a pessoa está ausente, vezes as horas por dia de trabalho da instalação (ADR-0141, ADR-0142). A ausência MUST NOT tirar a carga da semana.

#### Scenario: Capacidade com férias
- **WHEN** a Carla está ausente de quarta a sexta numa semana de 5 dias úteis, com 8 horas por dia de trabalho
- **THEN** a capacidade dela na semana é 16 horas, com 3 dias de ausência, e a carga das tarefas dela continua contando

#### Scenario: Capacidade com feriado
- **WHEN** a semana tem o feriado de 02/11
- **THEN** a capacidade de quem não está ausente é 32 horas

### Requirement: Estado da carga

Cada semana de cada pessoa MUST ter um estado, num lugar só: acima da capacidade (a carga passa da capacidade, também com capacidade zero), perto do limite (a carga chega a 90% da capacidade), dentro e livre (sem carga). A pessoa MUST dizer se está acima da capacidade em alguma das semanas (ADR-0142).

#### Scenario: Estados da carga
- **WHEN** a capacidade é 40 horas e a carga é 44, 38, 20 ou 0 horas
- **THEN** o estado é acima da capacidade, perto do limite, dentro ou livre

#### Scenario: Carga na semana toda de férias
- **WHEN** a pessoa está ausente a semana inteira e tem uma tarefa com horas previstas nela
- **THEN** a capacidade é zero e o estado é acima da capacidade

### Requirement: Pessoas da carga

A carga MUST trazer as pessoas ativas dos projetos não concluídos nem cancelados, em qualquer papel, mesmo sem carga, e quem tem carga sem estar num deles, pelo nome.

#### Scenario: Pessoa livre na carga
- **WHEN** o Lucas é leitor de um projeto em andamento e não tem tarefas
- **THEN** ele aparece com as oito semanas livres

#### Scenario: Pessoa desativada fora da carga
- **WHEN** uma pessoa foi desativada
- **THEN** ela não aparece na carga

### Requirement: Permissões na carga

`GET /api/admin/team-load` MUST ser só do administrador; as outras pessoas MUST receber 403 (ADR-0138, ADR-0142).

#### Scenario: Permissões na carga por papel
- **WHEN** cada papel pede a carga da equipe
- **THEN** o administrador recebe a carga, e o gerente, o líder técnico, o colaborador, o desenvolvedor e o leitor recebem 403

### Requirement: Carga da equipe pela tela

A Administração MUST ter "Carga da equipe" na seção Acompanhamento, depois de Horas dos projetos, com uma linha por pessoa e uma coluna por semana, cada célula com a carga e a capacidade em horas e o estado por ícone, cor e texto, a marca da ausência na semana, a semana anterior, a seguinte e "Esta semana", a busca pelo nome e as visões "Todas" e "Com sobrecarga"; o subtítulo MUST dizer quantas pessoas estão acima da capacidade no período (ADR-0142).

#### Scenario: Carga da equipe na tela
- **WHEN** o administrador abre Carga da equipe
- **THEN** a tela mostra as oito semanas a partir da atual, a carga e a capacidade de cada pessoa em cada uma, com o estado, e quantas pessoas estão acima da capacidade

#### Scenario: Com sobrecarga pela tela
- **WHEN** o administrador escolhe "Com sobrecarga"
- **THEN** só as pessoas acima da capacidade em alguma semana ficam na lista

#### Scenario: Próximas semanas pela tela
- **WHEN** o administrador avança uma semana
- **THEN** a tela pede a carga a partir da semana seguinte, e "Esta semana" volta à atual

### Requirement: Detalhe da carga pela tela

Escolher uma pessoa na Carga da equipe MUST abrir o detalhe na gaveta, pelo endereço (`?person=`), com cada semana: a carga, a capacidade, o estado, as ausências e as tarefas que pesam nela, com o projeto, a tarefa e as horas na semana, cada tarefa levando ao cronograma do projeto com o detalhe dela aberto (ADR-0081, ADR-0142).

#### Scenario: Detalhe da carga
- **WHEN** o administrador escolhe o Diego
- **THEN** a gaveta mostra, em cada semana, as tarefas do Diego com o projeto e as horas, e as ausências dele
