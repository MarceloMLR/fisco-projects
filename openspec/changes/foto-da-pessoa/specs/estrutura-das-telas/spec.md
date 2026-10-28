## ADDED Requirements

### Requirement: Foto no avatar

O avatar de uma pessoa MUST mostrar a foto dela no Microsoft 365 quando a foto carrega, cobrindo o círculo, sem contorno na cor do papel, e MUST continuar com as iniciais enquanto a foto não carrega, quando a pessoa não tem foto e quando a foto falha. Vale para o avatar que abre o cartão, o do cartão, o da pessoa na sessão, o do detalhe da pessoa em Membros e o das listas da menção (ADR-0124).

#### Scenario: Avatar com a foto
- **WHEN** a foto de Helena Duarte carrega no avatar dela numa mensagem do chamado
- **THEN** o avatar mostra a foto no lugar das iniciais, sem contorno

#### Scenario: Avatar sem foto
- **WHEN** a foto de uma pessoa não existe
- **THEN** o avatar continua com as iniciais
