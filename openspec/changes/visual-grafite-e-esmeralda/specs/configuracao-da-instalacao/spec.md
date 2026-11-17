## MODIFIED Requirements

### Requirement: Cor secundária

O administrador MUST poder escolher uma cor secundária, opcional, em hexadecimal, ou voltar ao padrão. Com ela MUST mudar, nos dois temas, a linha selecionada, a notificação não lida, a menção e o painel e o fundo da página de entrada. Sem ela, esses destaques MUST ficar os do design system, e o painel e o fundo da entrada MUST seguir a cor principal ou, sem cor principal, ficar no grafite do design system (ADR-0146, ADR-0147, ADR-0159).

#### Scenario: Sem cor secundária
- **WHEN** a instalação não tem cor secundária
- **THEN** a linha selecionada, a notificação não lida e a menção usam as cores do design system, e o painel e o fundo da entrada usam a cor principal

#### Scenario: Sem nenhuma cor
- **WHEN** a instalação não tem cor principal nem secundária
- **THEN** o painel e o fundo da entrada ficam no grafite do design system

#### Scenario: Trocar a cor secundária
- **WHEN** o administrador escolhe uma cor secundária e salva
- **THEN** a linha selecionada, a notificação não lida, a menção e o painel da entrada passam a usar o matiz dela, nos dois temas, e o botão principal continua na cor principal

#### Scenario: Painel da entrada legível
- **WHEN** a cor secundária é clara, como um amarelo
- **THEN** o fundo do painel da entrada fica mais escuro que ela, até o texto branco ter pelo menos 4,5:1

#### Scenario: Fundo da entrada na cor secundária
- **WHEN** o administrador escolhe uma cor secundária e salva
- **THEN** o fundo da página de entrada, atrás do cartão e do painel, fica num tom escuro do matiz dela, nos dois temas, no lugar do grafite do design system
