## ADDED Requirements

### Requirement: Logo na aba do navegador

A aba do navegador MUST mostrar o logo da instalação, quando há; sem logo, MUST mostrar o selo da barra lateral (o ícone da linha do tempo sobre a cor principal). O ícone MUST mudar quando o logo ou a cor principal muda, e MUST entrar só depois de a marca chegar (ADR-0108).

#### Scenario: Logo na aba
- **WHEN** a instalação tem logo e alguém abre uma tela
- **THEN** a aba do navegador mostra o logo

#### Scenario: Selo na aba sem logo
- **WHEN** a instalação não tem logo e alguém abre a página de entrada
- **THEN** a aba mostra o selo, na cor principal

#### Scenario: Selo na cor principal
- **WHEN** a instalação escolheu uma cor principal e não tem logo
- **THEN** o selo da aba usa a cor principal e o texto sobre ela do design system
