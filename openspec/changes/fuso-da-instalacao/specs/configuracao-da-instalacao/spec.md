## ADDED Requirements

### Requirement: Fuso da instalação

O administrador MUST poder escolher o fuso horário da instalação entre os fusos IANA, com o padrão America/Sao_Paulo. Todas as datas e horas das telas MUST aparecer nesse fuso, qualquer que seja o fuso do navegador, e os filtros por dia MUST começar e terminar à meia-noite desse fuso. Um fuso desconhecido MUST ser recusado. O fuso MUST ser lido só com sessão (ADR-0013).

#### Scenario: Fuso padrão
- **WHEN** ninguém escolheu o fuso da instalação
- **THEN** o fuso é America/Sao_Paulo, e as Configurações gerais mostram "Horário de Brasília (America/Sao_Paulo)"

#### Scenario: Horas no fuso da instalação
- **WHEN** a instalação está em America/Sao_Paulo e o navegador da pessoa está em outro fuso
- **THEN** o último acesso, as datas e as horas da Auditoria aparecem no horário de Brasília

#### Scenario: Trocar o fuso
- **WHEN** o administrador escolhe outro fuso e salva
- **THEN** as datas e horas de todas as telas passam a aparecer no novo fuso

#### Scenario: Período no fuso da instalação
- **WHEN** o administrador filtra a Auditoria por um dia
- **THEN** a lista mostra os registros da meia-noite desse dia até a meia-noite seguinte, no fuso da instalação

#### Scenario: Fuso inválido
- **WHEN** alguém pede para salvar um fuso que não existe ou que não é IANA
- **THEN** a API recusa com 400 e nada muda

#### Scenario: Fuso só com sessão
- **WHEN** alguém sem sessão pede o fuso da instalação
- **THEN** a API responde 401, e a marca lida sem sessão não traz o fuso

## MODIFIED Requirements

### Requirement: Tela Configurações gerais

A seção Instalação da coluna da Administração MUST ter Configurações gerais, com a seção Marca (nome exibido e logo, com a dica "PNG ou SVG, até 1 MB"), a seção Regras gerais (fuso horário, com busca), a Prévia da barra lateral e do painel da entrada como vão ficar, e a barra de ações com Descartar e Salvar alterações, habilitados só quando algo mudou. Descartar MUST voltar aos valores salvos.

#### Scenario: Configurações gerais na Administração
- **WHEN** o administrador escolhe Configurações gerais na coluna da Administração
- **THEN** a tela mostra a seção Marca com o nome e o logo atuais, a seção Regras gerais com o fuso atual e a Prévia

#### Scenario: Prévia antes de salvar
- **WHEN** o administrador digita outro nome ou escolhe um logo
- **THEN** a Prévia mostra o novo nome e o logo na hora, e o resto da aplicação só muda depois de salvar

#### Scenario: Descartar alterações
- **WHEN** o administrador muda o nome ou o fuso e escolhe Descartar
- **THEN** o formulário volta aos valores salvos e a barra de ações fica desabilitada

### Requirement: Mudança das configurações na auditoria

Cada mudança salva nas configurações da instalação MUST entrar na auditoria, com quem fez, quando e os campos com o valor antes e depois; o logo aparece pelo nome do arquivo enviado e o fuso pelo identificador. A tela de Auditoria MUST ter o tipo de item Configurações no filtro (ADR-0039).

#### Scenario: Troca do nome na auditoria
- **WHEN** o administrador troca o nome da instalação
- **THEN** a auditoria ganha um registro "Alterou as configurações" com o nome antes e depois

#### Scenario: Troca do fuso na auditoria
- **WHEN** o administrador troca o fuso da instalação
- **THEN** a auditoria ganha um registro "Alterou as configurações" com o fuso horário antes e depois

#### Scenario: Configurações no filtro da Auditoria
- **WHEN** o administrador filtra a Auditoria pelo tipo Configurações
- **THEN** a lista mostra só as mudanças das configurações da instalação
