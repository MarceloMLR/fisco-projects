# integracao-continua Specification

## Purpose

Define as verificações automáticas que todo pull request para a `main` precisa passar antes do merge, como elas são executadas e com que garantias de segurança (ADR-0055, ADR-0056, ADR-0060).

## Requirements

### Requirement: Verificações em todo pull request

Todo pull request para a `main` MUST passar por estas verificações: build do backend sem avisos, testes do backend (inclusive os que usam SQL Server em contêiner), formatação do código do backend, verificações do frontend (licenças das dependências, pacotes vulneráveis, formatação, lint, tipos, testes e build), validação das specs do OpenSpec e ortografia. Se qualquer verificação falhar, o pull request MUST ficar com a verificação reprovada.

#### Scenario: Pull request para a main
- **WHEN** um pull request para a `main` é aberto, reaberto ou recebe commits novos
- **THEN** a verificação do pull request roda

#### Scenario: Todas as verificações no pull request
- **WHEN** a verificação do pull request roda
- **THEN** ela executa build, testes, formatação, frontend, specs e ortografia, cada um como uma etapa própria e nessa ordem

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

### Requirement: Verificações em scripts do repositório

A lógica de cada verificação MUST ficar num script versionado no repositório, executável fora do GitHub com os mesmos comandos. O workflow MUST apenas preparar as ferramentas e chamar esses scripts (ADR-0055).

#### Scenario: Etapas do workflow
- **WHEN** o workflow da verificação é lido
- **THEN** toda etapa que executa comandos chama um script de verificação que existe no repositório

#### Scenario: Script chamado de outra pasta
- **WHEN** alguém roda um script de verificação a partir de qualquer pasta
- **THEN** o script trabalha a partir da raiz do repositório

### Requirement: Workflow sem segredos e com versões fixadas

O workflow da verificação MUST NOT usar segredos, MUST pedir só permissão de leitura do conteúdo do repositório e MUST usar versões fixas das ações, do executor e das ferramentas (ADR-0060).

#### Scenario: Sem segredos
- **WHEN** o workflow da verificação é lido
- **THEN** ele não referencia segredos e só pede permissão de leitura do conteúdo

#### Scenario: Versões fixadas
- **WHEN** o workflow da verificação é lido
- **THEN** toda ação é fixada pelo commit, o executor tem versão explícita e as versões do .NET e do Node vêm dos arquivos de versão do repositório
