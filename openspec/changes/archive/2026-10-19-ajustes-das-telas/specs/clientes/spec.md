## ADDED Requirements

### Requirement: Motivo do CNPJ inválido

O campo do CNPJ MUST dizer o motivo exato de um CNPJ inválido: faltar caractere, os dois últimos dígitos não baterem com os anteriores, ou todos os caracteres serem iguais (ADR-0015).

#### Scenario: Motivo do CNPJ inválido
- **WHEN** o administrador digita "11.222.333/0001", "11.222.333/0001-80" ou "11.111.111/1111-11"
- **THEN** o campo diz, na ordem, que o CNPJ tem 14 caracteres, que os dois últimos dígitos não batem com os anteriores e que um CNPJ não tem todos os caracteres iguais
