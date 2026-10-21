## ADDED Requirements

### Requirement: Aquecimento depois de subir

Depois de subir, a API MUST rodar uma vez, em segundo plano, as leituras principais das telas de cada módulo, com uma amostra dos próprios dados, para a primeira visita não pagar a primeira execução das consultas. O aquecimento MUST NOT alterar dados nem atrasar as respostas, e uma falha MUST ir só para o log, sem impedir os outros módulos. A configuração `WarmUp:Enabled` MUST poder desligá-lo (ADR-0106).

#### Scenario: Leituras principais aquecidas
- **WHEN** a instalação tem um projeto com cronograma, um chamado, um acesso e notificações, e o aquecimento roda
- **THEN** cada módulo com leituras de tela roda as dele até o fim, e a auditoria, as pastas e as notificações não lidas continuam iguais

#### Scenario: Aquecimento depois de subir
- **WHEN** a API sobe com o aquecimento ligado e um módulo falha ao aquecer
- **THEN** a API responde enquanto aquece, e os módulos seguintes aquecem mesmo assim
