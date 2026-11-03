## MODIFIED Requirements

### Requirement: Página do FiscoProjects no Teams

A página do FiscoProjects no Teams MUST abrir sem sessão. Com o aviso de um chamado e a entrada pelo Teams, MUST mostrar o chamado. Nos outros casos (fora do Teams, sem a entrada pelo Teams, outro item ou sem item), MUST mostrar só a marca da instalação e "Abrir no FiscoProjects", que MUST levar ao item no navegador, ou ao Início sem item. A página MUST aceitar só caminhos do próprio FiscoProjects (ADR-0149, ADR-0152).

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

## ADDED Requirements

### Requirement: Chamado na página do Teams

Com o aviso de um chamado e a entrada pelo Teams, a página do Teams MUST mostrar o chamado só para ler, no tema do Teams (ADR-0152): o projeto, o número, o título, o status, a prioridade e o prazo de resolução; as últimas interações da conversa, com as menções destacadas e quantas vieram antes; as pessoas, os prazos, o time, a abertura e quantos anexos há. "Responder no FiscoProjects" MUST abrir o chamado no navegador. Na largura do celular, tudo MUST ficar numa coluna, com o botão no pé.

#### Scenario: Chamado do aviso no Teams
- **WHEN** Diego escolhe no Teams o aviso de que foi mencionado no chamado #6
- **THEN** a página mostra o projeto, "#6", o título, o status, a prioridade, o prazo de resolução, as pessoas com "(você)" ao lado de Diego, os prazos, o time, a abertura e quantos anexos há, sem nenhum campo para responder

#### Scenario: Menção no chamado do Teams
- **WHEN** a mensagem que mencionou Diego está entre as últimas interações
- **THEN** a página mostra a mensagem com o autor, o papel, a hora e a menção a Diego destacada

#### Scenario: Interações anteriores no Teams
- **WHEN** o chamado tem mais interações do que a página mostra
- **THEN** a conversa começa dizendo quantas interações anteriores estão no FiscoProjects

#### Scenario: Responder no FiscoProjects
- **WHEN** a pessoa escolhe "Responder no FiscoProjects" na página do chamado #6 no Teams
- **THEN** a página do chamado #6 abre no navegador, no endereço da instalação

#### Scenario: Chamado no Teams do celular
- **WHEN** a página do chamado abre na largura do celular
- **THEN** o cabeçalho, as pessoas e a conversa ficam numa coluna, e "Responder no FiscoProjects" fica no pé, na largura toda

#### Scenario: Tema escuro do Teams no chamado
- **WHEN** a página do chamado abre com o Teams no tema escuro
- **THEN** o chamado aparece no tema escuro do FiscoProjects

### Requirement: Chamado que a pessoa não vê no Teams

Quando a leitura do chamado responde 404, a página do Teams MUST dizer "Você não vê este chamado", sem nada do chamado, com "Abrir no FiscoProjects". Uma falha da leitura que não seja 404 nem 401 MUST levar à marca e "Abrir no FiscoProjects", com a frase de que não foi possível mostrar o chamado no Teams (ADR-0044, ADR-0152).

#### Scenario: Chamado de outro projeto no Teams
- **WHEN** a página do Teams abre o aviso de um chamado de um projeto em que a pessoa não está
- **THEN** a página diz "Você não vê este chamado" e mostra "Abrir no FiscoProjects", sem o título nem a conversa

#### Scenario: Falha ao ler o chamado no Teams
- **WHEN** a leitura do chamado falha por um erro do servidor
- **THEN** a página diz que não foi possível mostrar o chamado no Teams e mostra a marca e "Abrir no FiscoProjects"

### Requirement: Token do Teams só na memória da página

A página do Teams MUST pedir o token ao Teams só para ler o chamado, MUST mandá-lo só no cabeçalho de autorização das rotas do Teams e MUST NOT guardá-lo no navegador, em cookie, no cache das consultas nem no endereço (ADR-0054, ADR-0152). Com o token recusado (401), a página MUST pedir um token novo uma vez e, se for recusado de novo, mostrar a marca e "Abrir no FiscoProjects".

#### Scenario: Token só na memória da página
- **WHEN** a página do Teams mostra um chamado
- **THEN** o token não está no `localStorage`, no `sessionStorage`, nos cookies, no cache das consultas nem no endereço

#### Scenario: Token recusado uma vez
- **WHEN** a primeira leitura do chamado recebe 401 e a segunda, com o token novo, é aceita
- **THEN** a página mostra o chamado

#### Scenario: Token recusado de novo
- **WHEN** a leitura do chamado recebe 401 também com o token novo
- **THEN** a página mostra a marca e "Abrir no FiscoProjects"
