## MODIFIED Requirements

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
