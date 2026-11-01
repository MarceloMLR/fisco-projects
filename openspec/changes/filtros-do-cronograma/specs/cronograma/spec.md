## ADDED Requirements

### Requirement: Filtros do cronograma

Toda pessoa do projeto MUST poder filtrar a Lista, a Linha do tempo e o Expandir por um filtro de cada vez: atrasadas, datas-limite estouradas, caminho crítico, minhas tarefas, próximas duas semanas ou por responsável. Os itens que passam MUST aparecer sob a fase e, se forem subtarefas, sob a tarefa delas, sem mudar as datas nem o período; o filtro MUST ficar no endereço e dizer quantos itens aparecem (ADR-0020, ADR-0021, ADR-0071, ADR-0102).

#### Scenario: Filtrar as atrasadas
- **WHEN** a gerente escolhe "Atrasadas" num cronograma de 20 tarefas e marcos com 4 atrasados
- **THEN** a Lista mostra só os 4, cada um sob a sua fase, o rodapé diz "4 de 20 tarefas e marcos" e o botão diz "Atrasadas"

#### Scenario: Minhas tarefas
- **WHEN** Diego, desenvolvedor do projeto, escolhe "Minhas tarefas"
- **THEN** aparecem só as tarefas de que ele é o responsável

#### Scenario: Por responsável
- **WHEN** uma pessoa do projeto escolhe Carla Mendes, o cliente ou "Sem responsável" em "Por responsável"
- **THEN** aparecem só as tarefas dela, as do cliente ou as sem responsável, e uma subtarefa aparece sob a tarefa dela

#### Scenario: Próximas duas semanas
- **WHEN** hoje é 19/10/2026 e a pessoa escolhe "Próximas duas semanas"
- **THEN** aparecem as tarefas e os marcos em aberto que acontecem de 19/10/2026 a 01/11/2026, e não os concluídos, os atrasados nem os que começam depois

#### Scenario: Filtro na Linha do tempo
- **WHEN** a pessoa escolhe "Caminho crítico" na Linha do tempo e abre o Expandir
- **THEN** as duas mostram só as fases e os itens do caminho crítico, com as setas entre eles, no mesmo período, e o endereço guarda o filtro

#### Scenario: Filtro sem resultado
- **WHEN** o filtro escolhido não tem nenhum item, como "Datas-limite estouradas" sem nenhuma estourada
- **THEN** a tela diz "Nenhuma tarefa ou marco neste filtro." e "Limpar filtro" volta a mostrar tudo
