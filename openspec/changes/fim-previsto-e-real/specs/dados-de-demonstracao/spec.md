## ADDED Requirements

### Requirement: Fim real na demonstração

A carga de demonstração MUST informar, nas tarefas concluídas, o dia da conclusão, e, nos marcos atingidos, o dia atingido: o fim planejado de cada um, sem passar do dia em que o comando roda, para o fim real não ser o dia da carga (ADR-0089, ADR-0167).

#### Scenario: Fim real na demonstração
- **WHEN** o comando conclui uma tarefa do projeto principal que terminava duas semanas antes e atinge o marco seguinte
- **THEN** o fim real da tarefa e o do marco são os dias planejados deles, e não o dia da carga
