# Spec Delta

## MODIFIED Requirements

### Requirement: Verificações em todo pull request

Todo pull request para a `main` MUST passar por estas verificações: build do backend sem avisos, testes do backend (inclusive os que usam SQL Server em contêiner), formatação do código do backend, verificações do frontend (licenças das dependências, pacotes vulneráveis, formatação, lint, tipos, testes e build) e validação das specs do OpenSpec. Se qualquer verificação falhar, o pull request MUST ficar com a verificação reprovada.

#### Scenario: Pull request para a main
- **WHEN** um pull request para a `main` é aberto, reaberto ou recebe commits novos
- **THEN** a verificação do pull request roda

#### Scenario: Todas as verificações no pull request
- **WHEN** a verificação do pull request roda
- **THEN** ela executa build, testes, formatação, frontend e specs, cada um como uma etapa própria e nessa ordem

#### Scenario: Comando que falha reprova a verificação
- **WHEN** qualquer comando de uma verificação termina com erro
- **THEN** essa verificação para no comando que falhou e termina com erro, e a verificação do pull request fica reprovada

#### Scenario: Pacote com vulnerabilidade conhecida
- **WHEN** um pacote do NuGet usado pelo backend, direto ou transitivo, tem vulnerabilidade conhecida
- **THEN** o build falha

#### Scenario: Pacote do frontend com vulnerabilidade conhecida
- **WHEN** um pacote que vai para o navegador, direto ou transitivo, tem vulnerabilidade conhecida
- **THEN** a verificação do frontend falha

#### Scenario: Dependência do frontend com licença não permitida
- **WHEN** uma dependência do frontend, direta ou transitiva, tem licença fora da lista permitida (licenças permissivas, sem copyleft forte nem licença comercial)
- **THEN** a verificação do frontend falha e aponta o pacote
