## ADDED Requirements

### Requirement: Guarda das notificações na instalação

O administrador MUST poder escolher, nas Regras gerais das Configurações gerais, por quantos dias as notificações do sino ficam guardadas, de 1 a 365, com o padrão de 90 (ADR-0038). Um valor fora disso MUST ser recusado.

#### Scenario: Guarda padrão
- **WHEN** ninguém escolheu a guarda das notificações
- **THEN** as Configurações gerais mostram 90 dias

#### Scenario: Guarda inválida
- **WHEN** alguém pede para guardar as notificações por 0 dias ou por mais de 365
- **THEN** a API recusa com 400 e nada muda
