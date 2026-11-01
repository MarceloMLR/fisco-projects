## ADDED Requirements

### Requirement: Horas por dia de trabalho

O administrador MUST poder escolher, nas Regras gerais das Configurações gerais, quantas horas cabem num dia de trabalho, de 1 a 12, em quartos de hora, com o padrão de 8, para a capacidade da carga da equipe (ADR-0142). Um valor fora disso MUST ser recusado, e a mudança MUST entrar na auditoria.

#### Scenario: Horas por dia padrão
- **WHEN** ninguém escolheu as horas por dia de trabalho
- **THEN** as Configurações gerais mostram 8 horas

#### Scenario: Trocar as horas por dia
- **WHEN** o administrador escolhe 7,5 horas e salva
- **THEN** a capacidade da carga passa a contar 7,5 horas por dia útil, e a mudança entra na auditoria

#### Scenario: Horas por dia inválidas
- **WHEN** alguém pede para salvar 0,5, 12,5 ou 7,3 horas por dia
- **THEN** a API recusa com 400 em `workHoursPerDay` e nada muda
