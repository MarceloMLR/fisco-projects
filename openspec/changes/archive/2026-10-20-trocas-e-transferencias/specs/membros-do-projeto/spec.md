## ADDED Requirements

### Requirement: Chamados no detalhe da pessoa

O detalhe da pessoa MUST mostrar os chamados em aberto do projeto em que ela é responsável ou solicitante, com a parte dela em cada um. Para o administrador e os gestores, MUST oferecer "Passar pendências", que passa esses chamados a outra pessoa, como responsável e como solicitante. A confirmação da remoção MUST avisar que os chamados de que a pessoa é responsável ficam sem responsável (ADR-0027, ADR-0042).

#### Scenario: Chamados da pessoa no detalhe
- **WHEN** o gerente escolhe um desenvolvedor responsável por dois chamados em aberto
- **THEN** o detalhe mostra os dois chamados, com "Responsável" em cada um

#### Scenario: Passar pendências
- **WHEN** o gerente escolhe "Passar pendências", escolhe outro desenvolvedor e confirma
- **THEN** os chamados passam ao outro desenvolvedor, e o detalhe fica sem chamados
