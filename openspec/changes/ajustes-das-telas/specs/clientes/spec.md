## MODIFIED Requirements

### Requirement: CNPJ do cliente

O CNPJ MUST ser aceito com ou sem pontuação, em letras maiúsculas ou minúsculas, e guardado só com os 14 caracteres. Ele MUST ter os dígitos verificadores corretos, valendo também o CNPJ alfanumérico (IN RFB nº 2.229/2024). O campo MUST dizer o motivo exato de um CNPJ inválido: faltar caractere, os dois últimos dígitos não baterem com os anteriores, ou todos os caracteres serem iguais. Um CNPJ já usado por outro cliente, ativo ou inativo, MUST ser recusado, com o nome do cliente que já o usa (ADR-0015).

#### Scenario: CNPJ com e sem pontuação
- **WHEN** o administrador digita "11.222.333/0001-81" ou "11222333000181"
- **THEN** o CNPJ é aceito e aparece como 11.222.333/0001-81

#### Scenario: CNPJ alfanumérico
- **WHEN** o administrador digita "12.abc.345/01de-35"
- **THEN** o CNPJ é aceito e aparece como 12.ABC.345/01DE-35

#### Scenario: CNPJ inválido
- **WHEN** o CNPJ tem um dígito verificador errado, menos de 14 caracteres ou todos os algarismos iguais
- **THEN** o campo mostra o motivo, a API recusa com 400 e nada muda

#### Scenario: Motivo do CNPJ inválido
- **WHEN** o administrador digita "11.222.333/0001", "11.222.333/0001-80" ou "11.111.111/1111-11"
- **THEN** o campo diz, na ordem, que o CNPJ tem 14 caracteres, que os dois últimos dígitos não batem com os anteriores e que um CNPJ não tem todos os caracteres iguais

#### Scenario: CNPJ repetido
- **WHEN** o administrador usa o CNPJ de outro cliente, mesmo inativo
- **THEN** a tela diz que já existe um cliente com esse CNPJ, com o nome dele, a API recusa com 409 e nada muda
