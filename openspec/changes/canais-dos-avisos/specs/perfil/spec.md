## ADDED Requirements

### Requirement: Canais dos avisos no perfil

O cartão "Avisos" de Meu perfil MUST mostrar três caixas, cada uma gravada na hora (ADR-0037, ADR-0151): "Por e-mail", marcada de início; "Pelo Teams", desmarcada de início e só com o Teams disponível para a pessoa; e o resumo diário, marcado de início. Sem nenhum canal marcado, o cartão MUST dizer que os avisos continuam no sino e no Meu trabalho. Uma gravação que falha MUST voltar a caixa ao que estava.

#### Scenario: Desligar o resumo
- **WHEN** a pessoa desmarca o resumo diário em Meu perfil
- **THEN** a escolha fica gravada, e ela deixa de receber o resumo

#### Scenario: Desligar o e-mail
- **WHEN** a pessoa sem o Teams disponível desmarca "Por e-mail" no cartão Avisos
- **THEN** a escolha fica gravada, o cartão diz que os avisos continuam no sino e no Meu trabalho, e os próximos avisos na hora não chegam por e-mail

#### Scenario: Escolher o Teams
- **WHEN** a pessoa com o Teams disponível marca "Pelo Teams" e desmarca "Por e-mail"
- **THEN** as escolhas ficam gravadas, e os próximos avisos na hora chegam só pelo Teams

#### Scenario: Canal de quem nunca escolheu
- **WHEN** a pessoa com o Teams disponível abre o cartão Avisos pela primeira vez
- **THEN** "Por e-mail" aparece marcado e "Pelo Teams" desmarcado

#### Scenario: Ajuda do aplicativo do Teams
- **WHEN** a pessoa com o Teams disponível abre o cartão Avisos
- **THEN** "Pelo Teams" diz que os avisos só chegam com o aplicativo do FiscoProjects no Teams dela

#### Scenario: Teams fora do cartão
- **WHEN** a pessoa abre o cartão Avisos com os avisos pelo Teams desligados na instalação
- **THEN** o cartão mostra só "Por e-mail" e o resumo diário

#### Scenario: Falha ao gravar
- **WHEN** a gravação de "Por e-mail" falha
- **THEN** a caixa volta ao que estava, e um aviso diz que não foi possível salvar

### Requirement: Escolhas dos avisos na auditoria

Ligar e desligar os avisos por e-mail, pelo Teams e o resumo diário MUST entrar na auditoria, com a pessoa da escolha, quem mudou, quando e o valor antes e depois (ADR-0039, ADR-0151). Uma gravação que não muda nada MUST NOT entrar.

#### Scenario: E-mail desligado na auditoria
- **WHEN** a pessoa desmarca "Por e-mail" em Meu perfil
- **THEN** a Auditoria mostra que ela desligou os avisos por e-mail, com o nome dela, a data e a hora

#### Scenario: Teams ligado na auditoria
- **WHEN** a pessoa marca "Pelo Teams" em Meu perfil
- **THEN** a Auditoria mostra que ela ligou os avisos pelo Teams

#### Scenario: Resumo diário na auditoria
- **WHEN** a pessoa desmarca o resumo diário em Meu perfil
- **THEN** a Auditoria mostra que ela desligou o resumo diário

#### Scenario: Escolha sem mudança fora da auditoria
- **WHEN** a pessoa grava de novo as mesmas escolhas
- **THEN** nenhum registro novo entra na Auditoria

## REMOVED Requirements

### Requirement: Avisos no perfil

**Reason**: Os avisos que pedem ação deixam de ser sempre ligados e passam a ter o e-mail e o Teams ligados e desligados pela pessoa; o cartão Avisos passa a seguir o requisito "Canais dos avisos no perfil" (ADR-0151).

**Migration**: As escolhas gravadas viram as duas caixas: E-mail marca só "Por e-mail", Teams marca só "Pelo Teams" e Os dois marca as duas; o resumo diário fica como estava.
