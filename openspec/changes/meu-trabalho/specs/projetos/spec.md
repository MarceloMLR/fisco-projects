## ADDED Requirements

### Requirement: Atraso do cronograma na lista

A lista de projetos MUST trazer, no cronograma de cada projeto, o atraso em dias úteis do item mais atrasado: a tarefa ou o marco com mais dias úteis de atraso no calendário da instalação; zero sem atraso (ADR-0088, ADR-0096).

#### Scenario: Atraso pelo item mais atrasado
- **WHEN** hoje é 19/10/2026 e um projeto tem uma tarefa com 2 dias úteis de atraso e um marco com 1
- **THEN** a lista traz o cronograma do projeto com 2 dias úteis de atraso

#### Scenario: Cronograma sem atraso
- **WHEN** nenhuma tarefa nem marco do projeto está atrasado
- **THEN** a lista traz o cronograma do projeto com zero dias de atraso

### Requirement: Próximos marcos da pessoa

`GET /api/projects/upcoming-milestones` MUST responder os 10 primeiros marcos não atingidos, de hoje em diante, dos projetos não concluídos nem cancelados de que a pessoa participa, pelo dia e pelo código do projeto, cada um com o projeto, o dia, os dias úteis que faltam e se há pedido de aceite pendente para ele (ADR-0070, ADR-0096).

#### Scenario: Próximos marcos pela API
- **WHEN** hoje é 19/10/2026 e o projeto da pessoa tem os marcos Homologação, em 03/11/2026, com um pedido de aceite pendente, e Entrada em produção, em 13/11/2026
- **THEN** a API responde os dois marcos nessa ordem, com 10 e 18 dias úteis, e o aceite pedido só no primeiro

#### Scenario: Marco atingido ou passado fica fora
- **WHEN** um marco do projeto foi atingido e outro venceu ontem sem ser atingido
- **THEN** nenhum dos dois aparece nos próximos marcos

#### Scenario: Só projetos da pessoa
- **WHEN** o administrador, fora de um projeto, pede os próximos marcos
- **THEN** os marcos desse projeto não aparecem
