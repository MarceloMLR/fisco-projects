## ADDED Requirements

### Requirement: Confirmações no alto da tela

Toda confirmação curta MUST aparecer no alto, no meio: no computador, na faixa do cabeçalho e no meio da área de conteúdo, sem cobrir a barra lateral; no celular, logo abaixo da barra do topo. A posição MUST vir do tema, e nenhuma tela MUST escolher a própria (ADR-0109).

#### Scenario: Confirmação no alto, no meio
- **WHEN** uma tela confirma algo que acabou de dar certo
- **THEN** a confirmação aparece no alto, no meio, abaixo da barra do topo no celular

#### Scenario: Toda confirmação no mesmo lugar
- **WHEN** qualquer tela mostra uma confirmação
- **THEN** ela usa a posição do tema, sem uma posição própria
