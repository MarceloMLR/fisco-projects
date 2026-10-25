## MODIFIED Requirements

### Requirement: Migração automática só no desenvolvimento

Ao subir no ambiente de desenvolvimento, a API MUST criar o banco, se ainda não existir, e aplicar as migrações pendentes de todos os módulos. Fora do desenvolvimento, a API MUST NOT criar nem alterar o banco ao subir; o banco vem da infraestrutura e as migrações, da publicação.

#### Scenario: API sobe no desenvolvimento
- **WHEN** a API sobe no ambiente de desenvolvimento com o banco acessível e migrações pendentes
- **THEN** as migrações são aplicadas antes de a API atender requisições

#### Scenario: Banco ainda não criado no desenvolvimento
- **WHEN** a API sobe no ambiente de desenvolvimento e o banco configurado ainda não existe
- **THEN** o banco é criado antes de a API atender requisições

#### Scenario: Banco criado pela infraestrutura
- **WHEN** a API sobe no ambiente de desenvolvimento com o banco já criado pela infraestrutura e uma identidade que só enxerga esse banco, como a identidade gerenciada no Azure SQL
- **THEN** a API usa o banco existente, sem tentar criá-lo, e aplica as migrações

#### Scenario: API sobe em produção
- **WHEN** a API sobe em produção com migrações pendentes
- **THEN** nenhuma migração é aplicada
