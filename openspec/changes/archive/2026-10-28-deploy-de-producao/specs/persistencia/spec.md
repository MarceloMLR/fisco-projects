## ADDED Requirements

### Requirement: Migrações pela publicação

Fora do desenvolvimento, as migrações MUST ser aplicadas pelo comando `migrate` da API, que monta os serviços, aplica as migrações de todos os módulos com a conexão configurada e termina sem subir o servidor nem os processos em segundo plano. Com a identidade gerenciada da aplicação configurada, o comando MUST criar o usuário dela no banco, se faltar, e dar a ela só a leitura e a gravação dos dados de todos os módulos, sem poder alterar o banco (ADR-0114, ADR-0125).

#### Scenario: Migrações pelo comando da publicação
- **WHEN** o comando `migrate` roda em produção num banco vazio e depois de novo
- **THEN** a primeira vez aplica as migrações de todos os módulos e a segunda não muda nada, sem falhar

#### Scenario: Acesso da aplicação ao banco
- **WHEN** a identidade da aplicação recebe o acesso dado pelo comando
- **THEN** ela lê e grava os dados e pega o número de um chamado novo, mas não cria nem altera tabelas
