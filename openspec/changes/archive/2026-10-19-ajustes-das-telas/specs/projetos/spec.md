## ADDED Requirements

### Requirement: Abas do projeto sem rolagem vertical

A linha das abas do projeto MUST NOT rolar na vertical nem mostrar barra de rolagem; ela só rola para o lado quando as abas não cabem na largura, como no celular (ADR-0066, ADR-0071).

#### Scenario: Abas sem barra de rolagem
- **WHEN** uma pessoa abre a página de um projeto no computador
- **THEN** a linha das abas não rola na vertical e não mostra barra de rolagem, e o traço da aba escolhida fica sobre a linha do cabeçalho
