## ADDED Requirements

### Requirement: Chamado para o Teams

`GET /api/teams/tickets/{número}` MUST devolver o chamado só para ler, com as regras de quem vê (ADR-0044, ADR-0152): o projeto, o número, o título, o time, o status, a prioridade, a abertura, os prazos, o solicitante, o responsável, os gerentes, quantos anexos há e as últimas 5 interações da conversa, com quantas vieram antes. A resposta MUST NOT trazer e-mails e MUST sair com `no-store`. Quem não vê o chamado, e o número que não existe, MUST receber 404.

#### Scenario: Resumo do chamado no Teams
- **WHEN** o responsável pelo chamado #104 pede o chamado pela rota do Teams
- **THEN** a resposta traz o projeto, o número, o título, o time, o status, a prioridade, a abertura, os dois prazos, o solicitante, o responsável, o gerente e quantos anexos há, com `no-store`

#### Scenario: Últimas interações do chamado
- **WHEN** o chamado tem a descrição, 8 mensagens e 3 eventos, e uma pessoa do projeto o pede pela rota do Teams
- **THEN** a resposta traz as 5 interações mais recentes, da mais antiga para a mais nova, cada mensagem com o autor, o papel, o texto formatado e os anexos pelo nome e pelo tamanho, e diz que 7 vieram antes

#### Scenario: Descrição como a primeira interação
- **WHEN** o chamado tem a descrição e 2 mensagens, e uma pessoa do projeto o pede pela rota do Teams
- **THEN** a resposta traz a descrição, pelo solicitante, e as 2 mensagens, e diz que nenhuma veio antes

#### Scenario: Resumo do chamado sem e-mails
- **WHEN** uma pessoa do projeto pede o chamado pela rota do Teams
- **THEN** a resposta traz as pessoas pelo nome, sem nenhum e-mail

#### Scenario: Leitor lê o chamado no Teams
- **WHEN** um leitor do projeto pede o chamado pela rota do Teams
- **THEN** a resposta traz o chamado

#### Scenario: Administrador lê o chamado no Teams
- **WHEN** o administrador, sem estar no projeto, pede o chamado pela rota do Teams
- **THEN** a resposta traz o chamado

#### Scenario: Fora do projeto não lê o chamado no Teams
- **WHEN** alguém que não está no projeto pede o chamado pela rota do Teams
- **THEN** a resposta é 404, sem nada do chamado

#### Scenario: Chamado que não existe no Teams
- **WHEN** uma pessoa pede pela rota do Teams um número sem chamado
- **THEN** a resposta é 404
