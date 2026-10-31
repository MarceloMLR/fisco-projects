## ADDED Requirements

### Requirement: Contagens das abas juntas

As abas do projeto MUST mostrar o lugar de cada contagem desde o começo, e os números MUST entrar juntos, esmaecendo em 150 ms, quando a última contagem chega, sem as abas mudarem de lugar uma a uma (ADR-0133).

#### Scenario: Contagens juntas
- **WHEN** uma pessoa abre um projeto e a contagem de Links chega antes da de Documentos
- **THEN** as abas mostram o lugar das contagens vazio até a última chegar, e então todos os números aparecem juntos

#### Scenario: Contagem que falha
- **WHEN** a contagem de uma aba não chega por uma falha
- **THEN** os outros números aparecem e o lugar daquela fica vazio
