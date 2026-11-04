## ADDED Requirements

### Requirement: Entrada direta pelo link do Teams

Os links da página do Teams MUST levar ao item por uma página de passagem, sem sessão e só com caminhos do FiscoProjects: com sessão, ao item; sem sessão, com a entrada pelo Entra e sem "Sair" neste navegador desde a última entrada, direto à entrada da Microsoft, com o último e-mail como dica e a volta ao item; nos outros casos, à página de entrada, que leva ao item depois de entrar (ADR-0054, ADR-0117, ADR-0153).

#### Scenario: Link do Teams com sessão
- **WHEN** Diego, com a sessão aberta no navegador, escolhe "Responder no FiscoProjects" no chamado #6 do Teams
- **THEN** o chamado #6 abre, sem a página de entrada

#### Scenario: Link do Teams sem sessão
- **WHEN** Diego, sem sessão no navegador e com a entrada pelo Entra, escolhe "Responder no FiscoProjects" no chamado #6 do Teams
- **THEN** o navegador vai direto à entrada da Microsoft, com o último e-mail dele como dica, e, depois de entrar, o chamado #6 abre

#### Scenario: Link do Teams sem e-mail lembrado
- **WHEN** o navegador não tem o último e-mail e Diego, sem sessão, escolhe "Responder no FiscoProjects"
- **THEN** o navegador vai à entrada da Microsoft sem dica

#### Scenario: Link do Teams depois de Sair
- **WHEN** Diego escolheu Sair neste navegador e depois escolhe "Responder no FiscoProjects" no Teams
- **THEN** a página de entrada aparece e, depois de entrar, o chamado #6 abre

#### Scenario: Link do Teams sem a entrada pelo Entra
- **WHEN** a instalação só tem a entrada local e a pessoa, sem sessão, escolhe "Abrir no FiscoProjects"
- **THEN** a página de entrada aparece e, depois de entrar, o item abre

#### Scenario: Recusa da Microsoft pelo link do Teams
- **WHEN** a Microsoft recusa a entrada que começou pelo link do Teams
- **THEN** a página de entrada mostra o motivo, como em toda entrada pelo Entra

#### Scenario: Link do Teams para outro site
- **WHEN** a página de passagem recebe o endereço de outro site no lugar do caminho do item
- **THEN** ela leva ao Início, e não ao outro site

## MODIFIED Requirements

### Requirement: Sair

"Sair" MUST encerrar só a sessão do FiscoProjects e levar à página de entrada; a entrada seguinte MUST levar ao Início, seja quem for (ADR-0009). O navegador MUST guardar que a pessoa saiu, só essa marca, até a próxima entrada, para o link do Teams mostrar a página de entrada (ADR-0153).

#### Scenario: Sair
- **WHEN** a pessoa escolhe Sair
- **THEN** a sessão é encerrada, a página de entrada é exibida e as chamadas seguintes recebem 401

#### Scenario: Entrar depois de Sair
- **WHEN** Marina escolhe Sair na página Projetos e Diego entra em seguida
- **THEN** a aplicação mostra o Início, e não Projetos

#### Scenario: Sair lembrado até a próxima entrada
- **WHEN** a pessoa escolhe Sair, fecha o navegador e depois entra de novo
- **THEN** a marca de que ela saiu fica no navegador até a entrada e some depois dela, sem nenhum token guardado
