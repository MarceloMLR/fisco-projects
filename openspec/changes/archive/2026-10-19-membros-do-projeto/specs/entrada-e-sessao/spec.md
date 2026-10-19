# Spec Delta

## MODIFIED Requirements

### Requirement: Usuário no primeiro login

Na primeira entrada de uma identidade, o sistema MUST criar o usuário, com identificador próprio, e o vínculo com o provedor (provedor e identificador externo) em registro separado. Nas entradas seguintes, MUST reconhecer o mesmo usuário e atualizar nome e e-mail com os dados do provedor. Quem foi incluído num projeto pelo diretório MUST entrar com o usuário já criado, sem outro (ADR-0005, ADR-0009).

#### Scenario: Primeira entrada
- **WHEN** uma identidade entra pela primeira vez
- **THEN** o sistema cria o usuário com o nome e o e-mail do provedor, ativo, e o vínculo com o provedor

#### Scenario: Entradas seguintes
- **WHEN** a mesma identidade entra de novo com outro nome no provedor
- **THEN** o sistema usa o mesmo usuário, sem criar outro, e atualiza o nome

#### Scenario: Primeira entrada de quem foi incluído pelo diretório
- **WHEN** uma pessoa incluída num projeto pelo diretório entra pela primeira vez
- **THEN** o sistema usa o usuário já criado, registra o último acesso, e ela vê o projeto
