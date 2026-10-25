# integracao-continua Specification

## Purpose

Define as verificações automáticas que todo pull request para a `main` precisa passar antes do merge, como elas são executadas e com que garantias de segurança (ADR-0055, ADR-0056, ADR-0060).

## Requirements

### Requirement: Verificações em todo pull request

Todo pull request para a `main` MUST passar por estas verificações: build do backend sem avisos, testes do backend (inclusive os que usam SQL Server em contêiner), formatação do código do backend, verificações do frontend (licenças das dependências, pacotes vulneráveis, formatação, lint, tipos, testes e build), formatação e validação do Terraform e validação das specs do OpenSpec. Se qualquer verificação falhar, o pull request MUST ficar com a verificação reprovada.

#### Scenario: Pull request para a main
- **WHEN** um pull request para a `main` é aberto, reaberto ou recebe commits novos
- **THEN** a verificação do pull request roda

#### Scenario: Todas as verificações no pull request
- **WHEN** a verificação do pull request roda num commit que muda código
- **THEN** build e testes, formatação, frontend, infraestrutura e specs rodam em jobs paralelos, e o job `verify` só aprova quando todos passaram

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

#### Scenario: Terraform fora do padrão
- **WHEN** o código do Terraform está fora da formatação ou não é válido
- **THEN** a verificação da infraestrutura falha, sem precisar de credenciais do Azure

### Requirement: Verificações em scripts do repositório

A lógica de cada verificação MUST ficar num script versionado no repositório, executável fora do GitHub com os mesmos comandos. O workflow MUST apenas preparar as ferramentas e chamar esses scripts (ADR-0055).

#### Scenario: Etapas do workflow
- **WHEN** o workflow da verificação é lido
- **THEN** toda etapa que executa comandos chama um script de verificação que existe no repositório

#### Scenario: Script chamado de outra pasta
- **WHEN** alguém roda um script de verificação a partir de qualquer pasta
- **THEN** o script trabalha a partir da raiz do repositório

### Requirement: Workflow sem segredos e com versões fixadas

O workflow da verificação MUST NOT usar segredos, MUST pedir só permissões de leitura (do conteúdo, das verificações e dos pull requests) e MUST usar versões fixas das ações, do executor e das ferramentas (ADR-0060, ADR-0090).

#### Scenario: Sem segredos
- **WHEN** o workflow da verificação é lido
- **THEN** ele não referencia segredos e só pede permissões de leitura do conteúdo, das verificações e dos pull requests

#### Scenario: Versões fixadas
- **WHEN** o workflow da verificação é lido
- **THEN** toda ação é fixada pelo commit, o executor tem versão explícita e as versões do .NET e do Node vêm dos arquivos de versão do repositório

### Requirement: Commit que só muda documentação

Num commit cujo código é igual ao do último commit do pull request aprovado na verificação, a verificação MUST pular build, testes, formatação e frontend e rodar as specs. Só conta como documentação o que está em `docs/`, `openspec/` e `.claude/` e os arquivos `.md` fora de `web/`. Sem commit aprovado, a comparação MUST ser com a `main` (ADR-0090).

#### Scenario: Archive só com documentação
- **WHEN** o último commit do pull request passou na verificação e o commit novo só muda `openspec/` e `docs/roadmap.md`
- **THEN** só as specs rodam, e o job `verify` aprova se elas passarem

#### Scenario: Código depois de um commit aprovado
- **WHEN** o commit novo muda um arquivo em `src/`, `tests/`, `web/` ou qualquer outro lugar que não seja documentação
- **THEN** todas as verificações rodam

#### Scenario: Commit aprovado anterior a uma falha
- **WHEN** o último commit verificado do pull request falhou e o commit novo só muda documentação
- **THEN** a comparação usa o último commit aprovado, ou a `main` sem nenhum, e o código que mudou desde então faz tudo rodar

#### Scenario: Parte pulada sem motivo
- **WHEN** build, testes, formatação ou frontend não rodaram e o commit muda código
- **THEN** o job `verify` reprova

### Requirement: Cache dos pacotes

A verificação MUST guardar em cache os pacotes do NuGet, pela chave dos arquivos de projeto e do `global.json`, e os do npm, pelos `package-lock.json`, para não baixá-los de novo a cada execução (ADR-0090).

#### Scenario: Pacotes em cache
- **WHEN** o workflow da verificação é lido
- **THEN** o job do backend e o da formatação restauram o cache do NuGet, e os jobs do frontend e das specs, o do npm
