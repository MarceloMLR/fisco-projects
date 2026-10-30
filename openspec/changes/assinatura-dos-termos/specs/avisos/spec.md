## MODIFIED Requirements

### Requirement: Avisos dos termos de aceite

O pedido de assinatura e o lembrete MUST NOT gerar e-mail do FiscoProjects: o provedor manda o link a cada signatário. O resumo diário MUST NOT trazer pedidos de aceite para a pessoa responder. O termo assinado, inclusive o anexado pela gestão, e a recusa MUST avisar quem pediu e o gerente do projeto pelo sino e pelo resumo diário. Nenhum aviso MUST levar o termo, o e-mail dos signatários nem o motivo da recusa por e-mail; o aviso leva ao termo no FiscoProjects (ADR-0037, ADR-0038, ADR-0129).

#### Scenario: Pedido de aceite por e-mail
- **WHEN** a gerente pede a assinatura a dois signatários
- **THEN** nenhum e-mail sai da fila do FiscoProjects para eles, e o provedor recebe o termo com os dois

#### Scenario: Lembrete do pedido
- **WHEN** a gerente lembra os signatários de um pedido pendente
- **THEN** nenhum e-mail sai da fila do FiscoProjects, e o provedor manda o link de novo a quem não assinou

#### Scenario: Termo assinado avisa a gestão
- **WHEN** o último signatário assina o termo
- **THEN** quem pediu e o gerente têm a notificação no sino, e o aviso entra no próximo resumo diário deles

#### Scenario: Recusa avisa a gestão
- **WHEN** um signatário recusa o termo
- **THEN** quem pediu e o gerente têm a notificação da recusa no sino e no resumo

#### Scenario: Aceites pendentes no resumo
- **WHEN** chega o horário do resumo e Carla é a pessoa de um pedido pendente do fluxo anterior
- **THEN** o resumo de Carla não traz o pedido
