## ADDED Requirements

### Requirement: Arquivos no SharePoint

Com `FileStorage:Provider` = `SharePoint`, os arquivos MUST ficar na pasta raiz configurada de uma biblioteca do site do SharePoint, criada na primeira vez, pelo Microsoft Graph com o token do aplicativo, com as mesmas regras da implementação local. Um id fora da pasta raiz MUST valer como inexistente. Sem o endereço https do site, a aplicação MUST NOT subir (ADR-0030, ADR-0120).

#### Scenario: Implementação do SharePoint em produção
- **WHEN** a aplicação sobe em produção com o SharePoint e um módulo guarda um arquivo
- **THEN** o arquivo fica na pasta raiz da biblioteca do site, criada na primeira vez

#### Scenario: SharePoint sem o endereço do site
- **WHEN** a aplicação sobe com o SharePoint sem um endereço https do site
- **THEN** ela não sobe e aponta `FileStorage:SharePoint:SiteUrl`

#### Scenario: Item fora da pasta raiz
- **WHEN** um módulo pede para abrir ou excluir um item do site que está fora da pasta raiz
- **THEN** o item conta como inexistente, e nada é aberto nem excluído
