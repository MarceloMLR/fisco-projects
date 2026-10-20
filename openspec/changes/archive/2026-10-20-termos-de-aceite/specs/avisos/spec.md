## ADDED Requirements

### Requirement: Avisos dos termos de aceite

O pedido de aceite e o lembrete MUST gerar e-mail na hora para a pessoa do pedido (ADR-0037). O termo assinado, inclusive o anexado pela gestão, e a recusa MUST avisar quem pediu e o gerente do projeto pelo sino e pelo resumo diário. O resumo diário MUST trazer os pedidos de aceite pendentes da pessoa nos projetos em aberto, com o projeto e o dia do pedido. Nenhum aviso MUST levar o termo nem o motivo da recusa por e-mail; o aviso leva ao termo no FiscoProjects (ADR-0035, ADR-0037, ADR-0038).

#### Scenario: Pedido de aceite por e-mail
- **WHEN** a gerente pede o aceite a Carla
- **THEN** Carla recebe o e-mail na hora, com o botão que abre o termo

#### Scenario: Lembrete do pedido
- **WHEN** a gerente lembra Carla de um pedido pendente
- **THEN** Carla recebe o e-mail do pedido de novo, na hora

#### Scenario: Termo assinado avisa a gestão
- **WHEN** Carla envia o termo assinado
- **THEN** quem pediu e o gerente têm a notificação no sino, e o aviso entra no próximo resumo diário deles

#### Scenario: Recusa avisa a gestão
- **WHEN** Carla recusa o pedido
- **THEN** quem pediu e o gerente têm a notificação da recusa no sino e no resumo

#### Scenario: Aceites pendentes no resumo
- **WHEN** chega o horário do resumo e Carla tem um pedido de aceite pendente
- **THEN** o resumo dela traz o pedido, com o projeto e o dia do pedido
