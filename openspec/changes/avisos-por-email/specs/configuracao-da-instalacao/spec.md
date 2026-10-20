## ADDED Requirements

### Requirement: Horário do resumo diário

O administrador MUST poder escolher, nas Regras gerais das Configurações gerais, o horário do resumo diário de e-mails, em horas e minutos no fuso da instalação, com o padrão 07:30 (ADR-0037). O horário MUST ser lido com sessão, para Meu perfil mostrá-lo.

#### Scenario: Horário padrão
- **WHEN** ninguém escolheu o horário do resumo
- **THEN** o resumo sai às 07:30 do fuso da instalação, e as Configurações gerais mostram 07:30

#### Scenario: Trocar o horário
- **WHEN** o administrador escolhe 08:00 e salva
- **THEN** o próximo resumo sai às 08:00, e a mudança entra na auditoria

#### Scenario: Horário inválido
- **WHEN** alguém pede para salvar a instalação sem o horário do resumo
- **THEN** a API recusa com 400 e nada muda
