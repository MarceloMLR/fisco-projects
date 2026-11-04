## MODIFIED Requirements

### Requirement: Página do FiscoProjects no Teams

A página do FiscoProjects no Teams MUST abrir sem sessão. Com o aviso de um chamado e a entrada pelo Teams, MUST mostrar o chamado. Nos outros casos (fora do Teams, sem a entrada pelo Teams, outro item ou sem item), MUST mostrar só a marca da instalação e "Abrir no FiscoProjects", que MUST levar ao item no navegador, ou ao Início sem item, pela entrada direta do link do Teams (ADR-0153). A página MUST aceitar só caminhos do próprio FiscoProjects (ADR-0149, ADR-0152, ADR-0153).

#### Scenario: Abrir o item no navegador
- **WHEN** a entrada pelo Teams falha no aviso do chamado #104 e a pessoa escolhe "Abrir no FiscoProjects"
- **THEN** a página do chamado #104 abre no navegador, no endereço da instalação

#### Scenario: Página sem item
- **WHEN** a pessoa abre o FiscoProjects pela barra do Teams
- **THEN** "Abrir no FiscoProjects" leva ao Início

#### Scenario: Endereço de fora recusado
- **WHEN** a página do Teams recebe o endereço de outro site no lugar do caminho do item
- **THEN** "Abrir no FiscoProjects" leva ao Início, e não ao outro site

#### Scenario: Aviso de outro item no Teams
- **WHEN** a pessoa escolhe no Teams o aviso de uma tarefa do cronograma
- **THEN** a página mostra a marca e "Abrir no FiscoProjects", que abre a tarefa no navegador

#### Scenario: Página do Teams fora do Teams
- **WHEN** a página do Teams é aberta no navegador, fora do Teams, com o caminho de um chamado
- **THEN** a página mostra a marca e "Abrir no FiscoProjects", sem pedir token
