## ADDED Requirements

### Requirement: Responsável ausente durante a tarefa

Cada tarefa em aberto com datas próprias e uma pessoa como responsável MUST trazer as ausências dessa pessoa que se cruzam com as datas da tarefa; a Lista MUST mostrar o aviso ao lado do responsável, e o detalhe, os períodos. As datas da tarefa MUST NOT mudar pela ausência (ADR-0019, ADR-0141).

#### Scenario: Tarefa na ausência do responsável
- **WHEN** a Carla, responsável pela tarefa de 03/11 a 07/11, tem ausência de 05/11 a 14/11
- **THEN** a tarefa traz a ausência de 05/11 a 14/11, a Lista mostra o aviso ao lado da Carla, e o detalhe diz "Responsável ausente: 05/11 a 14/11, durante a tarefa"

#### Scenario: Sem aviso na concluída
- **WHEN** a tarefa da Carla está a 100% ou a ausência não se cruza com as datas dela
- **THEN** a tarefa não traz ausências
