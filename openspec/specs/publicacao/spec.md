# publicacao Specification

## Purpose
A publicação do ambiente de desenvolvimento e demonstração no Azure a cada merge na `main`, pela identidade federada do pipeline, sem segredos e só com o poder de publicar e conferir, e a conferência da infraestrutura contra o código, sem nada pessoal nos logs públicos (ADR-0055, ADR-0115, ADR-0116).

## Requirements

### Requirement: Publicação a cada merge

Cada merge na `main` MUST publicar a aplicação no ambiente de desenvolvimento e demonstração, montando o pacote com as telas e a API, e MUST esperar o `/health` responder antes de terminar. No ambiente com a entrada local, a publicação MUST carregar em seguida os dados de demonstração que faltam, tentando de novo quando a aplicação reinicia no meio da carga. A lógica MUST ficar nos scripts de `scripts/cd`, chamados pelo workflow, uma publicação por vez (ADR-0055, ADR-0089, ADR-0115).

#### Scenario: Publicação a cada merge
- **WHEN** um PR entra na `main`
- **THEN** o workflow `deploy-dev` monta o pacote, publica no App Service do ambiente `dev` e confere a infraestrutura, nessa ordem

#### Scenario: Demonstração depois da publicação
- **WHEN** a aplicação publicada responde no `/health` num ambiente em `Development`
- **THEN** os dados de demonstração que faltam são carregados pela própria API

#### Scenario: Demonstração depois de um reinício
- **WHEN** a carga dos dados de demonstração falha logo depois da publicação, enquanto a aplicação reinicia
- **THEN** a publicação tenta a carga de novo até três vezes, com 30 segundos de intervalo, e só falha se a última falhar

### Requirement: Pipeline sem segredos e com poder limitado

O pipeline MUST alcançar o Azure por identidade federada, sem segredos no GitHub, só a partir do environment do GitHub do ambiente, que aceita só a `main`, e pelo subject imutável do GitHub, com os números do dono e do repositório. A identidade do pipeline MUST poder só publicar no grupo de recursos do ambiente, travar e ler o estado do Terraform e ler o Entra ID; ela MUST NOT dar papéis no Azure nem mudar o Entra ID (ADR-0055, ADR-0115, ADR-0116).

#### Scenario: Pipeline sem segredos
- **WHEN** o workflow de publicação é conferido
- **THEN** ele não usa segredos, só pede o token de identidade e a leitura do código, e usa ações fixadas por commit

#### Scenario: Identidade do pipeline com poder limitado
- **WHEN** a identidade do pipeline é criada pelo Terraform
- **THEN** ela só vale para o environment do GitHub do ambiente e tem só os papéis de colaborar no grupo de recursos, gravar no contêiner do estado, ler a conta do estado e ler o diretório e os aplicativos

#### Scenario: Federação pelos números do repositório
- **WHEN** o repositório do pipeline é configurado no ambiente
- **THEN** ele é dado pelo dono e pelo repositório com os números de cada um, como no subject do token do GitHub, e um repositório com o mesmo nome e outros números não ganha a identidade

### Requirement: Conferência da infraestrutura

Depois de publicar, o pipeline MUST rodar o `terraform plan` com o estado do ambiente, sem aplicar. Uma diferença MUST aparecer como aviso no resumo da execução, com o que mudaria, sem reprovar a publicação; uma falha do `plan` MUST reprovar. O e-mail dos alertas MUST ficar fora do workflow e dos planos, que aparecem nos logs públicos: o pipeline MUST lê-lo do grupo de ações da instalação e mascará-lo antes de tudo (ADR-0115, ADR-0116).

#### Scenario: Infraestrutura diferente do código
- **WHEN** o `terraform plan` encontra diferenças
- **THEN** a execução avisa que a infraestrutura está diferente do código, mostra o que mudaria e não falha

#### Scenario: Infraestrutura igual ao código
- **WHEN** o `terraform plan` não encontra diferenças
- **THEN** a execução diz que a infraestrutura está igual ao código; se o `plan` falhar, a execução falha

#### Scenario: E-mail dos alertas fora dos logs
- **WHEN** o pipeline confere a infraestrutura
- **THEN** o e-mail dos alertas não está no workflow, é lido do grupo de ações e mascarado antes de qualquer linha do log, e a conferência falha se o grupo não tiver e-mail

### Requirement: Site do SharePoint fora dos logs

O endereço do site do SharePoint MUST ficar fora do workflow, porque traz o nome do tenant: a conferência da infraestrutura MUST lê-lo da configuração da aplicação no ar e mascará-lo antes de conferir (ADR-0116, ADR-0120).

#### Scenario: Site do SharePoint fora dos logs
- **WHEN** o pipeline confere a infraestrutura de um ambiente com o site do SharePoint
- **THEN** o endereço não está no workflow, é lido da aplicação no ar e mascarado, e sem ele a conferência segue sem o site
