## ADDED Requirements

### Requirement: Chamados de um projeto cancelado

O chamado fechado pelo cancelamento do projeto MUST ficar Fechado em qualquer status em que estava, com o evento "projeto cancelado" na conversa, com quem cancelou e o status de antes. Os prazos MUST parar: o resolvido guarda o prazo da resolução, e os outros ficam sem prazo de resolução, como um chamado cancelado (ADR-0025, ADR-0043).

#### Scenario: Fechado pelo cancelamento do projeto
- **WHEN** o projeto é cancelado com um chamado Aguardando solicitante
- **THEN** o chamado fica Fechado, a conversa mostra "fechou o chamado · projeto cancelado · Aguardando solicitante → Fechado" e o chamado fica sem prazo de resolução
