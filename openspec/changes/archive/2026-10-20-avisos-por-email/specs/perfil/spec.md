## ADDED Requirements

### Requirement: Avisos por e-mail no perfil

O cartão "Avisos por e-mail" de Meu perfil MUST mostrar o resumo diário, com o horário da instalação, numa caixa que a pessoa marca e desmarca, gravada na hora no perfil dela; e os avisos na hora, sempre marcados e sem poder desmarcar, porque não se desligam (ADR-0037). O resumo MUST vir ligado para quem nunca escolheu.

#### Scenario: Desligar o resumo
- **WHEN** a pessoa desmarca o resumo diário em Meu perfil
- **THEN** a escolha fica gravada, e ela deixa de receber o resumo

#### Scenario: Avisos na hora sempre ligados
- **WHEN** a pessoa abre o cartão Avisos por e-mail
- **THEN** os avisos na hora aparecem marcados e desabilitados, com o texto que diz que não se desligam
