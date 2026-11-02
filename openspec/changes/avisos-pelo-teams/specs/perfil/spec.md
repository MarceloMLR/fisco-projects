## RENAMED Requirements

- FROM: `### Requirement: Avisos por e-mail no perfil`
- TO: `### Requirement: Avisos no perfil`

## MODIFIED Requirements

### Requirement: Avisos no perfil

O cartão "Avisos" de Meu perfil MUST mostrar dois controles, gravados na hora no perfil da pessoa (ADR-0037, ADR-0149):

- o resumo diário, com o horário da instalação, numa caixa que a pessoa marca e desmarca, ligado para quem nunca escolheu;
- os avisos na hora: com o Teams disponível para a pessoa, a escolha entre E-mail, Teams e Os dois, com Os dois para quem nunca escolheu; sem ele, uma caixa sempre marcada e desabilitada, porque os avisos na hora não se desligam.

#### Scenario: Desligar o resumo
- **WHEN** a pessoa desmarca o resumo diário em Meu perfil
- **THEN** a escolha fica gravada, e ela deixa de receber o resumo

#### Scenario: Avisos na hora sempre ligados
- **WHEN** a pessoa sem o Teams disponível abre o cartão Avisos
- **THEN** os avisos na hora aparecem marcados e desabilitados, com o texto que diz que não se desligam

#### Scenario: Escolher o Teams
- **WHEN** a pessoa com o Teams disponível escolhe Teams nos avisos na hora
- **THEN** a escolha fica gravada, e os próximos avisos na hora chegam só pelo Teams

#### Scenario: Canal de quem nunca escolheu
- **WHEN** a pessoa com o Teams disponível abre o cartão Avisos pela primeira vez
- **THEN** os avisos na hora mostram Os dois
