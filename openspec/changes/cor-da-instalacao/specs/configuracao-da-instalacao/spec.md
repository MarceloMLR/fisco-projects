## ADDED Requirements

### Requirement: Cor principal

O administrador MUST poder trocar a cor principal da instalação por uma cor em hexadecimal, ou voltar à cor padrão do design system. Com a nova cor MUST mudar, nos temas claro e escuro, o botão principal, os links, os controles marcados, o anel de foco, o item ativo da barra lateral, o avatar do responsável e o painel da entrada; as cores de status MUST NOT mudar. Uma cor sem contraste de 4,5:1 com o branco MUST ser recusada (ADR-0082).

#### Scenario: Cor padrão
- **WHEN** ninguém trocou a cor principal
- **THEN** as telas usam as cores do design system, sem nenhuma mudança

#### Scenario: Trocar a cor principal
- **WHEN** o administrador escolhe outra cor com contraste e salva
- **THEN** o botão principal, os links, o item ativo da barra lateral e o painel da entrada passam a usar a nova cor, nos dois temas, e as cores de status continuam as mesmas

#### Scenario: Contraste nos dois temas
- **WHEN** uma cor é aceita
- **THEN** o texto do botão principal, os links sobre as superfícies e o item ativo da barra lateral têm contraste de pelo menos 4,5:1, no tema claro e no escuro

#### Scenario: Cor sem contraste
- **WHEN** o administrador escolhe uma cor clara, como um amarelo, sem 4,5:1 com o branco
- **THEN** a tela mostra "Contraste insuficiente", não deixa salvar, e a API recusa com 400

#### Scenario: Voltar ao padrão
- **WHEN** o administrador escolhe "Voltar ao padrão" e salva
- **THEN** a instalação volta à cor padrão do design system

## MODIFIED Requirements

### Requirement: Marca visível sem sessão

O nome, o logo e a cor principal da instalação MUST poder ser lidos sem sessão, porque a página de entrada os mostra (ADR-0009). Nenhuma outra configuração MUST ser exposta sem sessão.

#### Scenario: Marca na página de entrada
- **WHEN** alguém sem sessão abre a página de entrada
- **THEN** a página mostra o nome, o logo e o painel na cor principal da instalação

### Requirement: Tela Configurações gerais

A seção Instalação da coluna da Administração MUST ter Configurações gerais, com:

- a seção Marca: nome exibido; logo, com a dica "PNG ou SVG, até 1 MB"; e cor principal, com a amostra, o campo hexadecimal, "Voltar ao padrão" e o resultado do contraste;
- a seção Regras gerais, com o fuso horário e busca;
- a Prévia da barra lateral e do painel da entrada como vão ficar;
- a barra de ações com Descartar e Salvar alterações, habilitados só quando algo mudou.

Descartar MUST voltar aos valores salvos.

#### Scenario: Configurações gerais na Administração
- **WHEN** o administrador escolhe Configurações gerais na coluna da Administração
- **THEN** a tela mostra a seção Marca com o nome, o logo e a cor atuais, a seção Regras gerais com o fuso atual e a Prévia

#### Scenario: Prévia antes de salvar
- **WHEN** o administrador digita outro nome, escolhe um logo ou digita outra cor
- **THEN** a Prévia mostra o novo nome, o logo e a cor na hora, e o resto da aplicação só muda depois de salvar

#### Scenario: Descartar alterações
- **WHEN** o administrador muda o nome, o fuso ou a cor e escolhe Descartar
- **THEN** o formulário volta aos valores salvos e a barra de ações fica desabilitada

### Requirement: Mudança das configurações na auditoria

Cada mudança salva nas configurações da instalação MUST entrar na auditoria, com quem fez, quando e os campos com o valor antes e depois. O logo aparece pelo nome do arquivo enviado, o fuso pelo identificador e a cor pelo hexadecimal, vazio para a cor padrão. A tela de Auditoria MUST ter o tipo de item Configurações no filtro (ADR-0039).

#### Scenario: Troca do nome na auditoria
- **WHEN** o administrador troca o nome da instalação
- **THEN** a auditoria ganha um registro "Alterou as configurações" com o nome antes e depois

#### Scenario: Troca do fuso na auditoria
- **WHEN** o administrador troca o fuso da instalação
- **THEN** a auditoria ganha um registro "Alterou as configurações" com o fuso horário antes e depois

#### Scenario: Troca da cor na auditoria
- **WHEN** o administrador troca a cor principal
- **THEN** a auditoria ganha um registro "Alterou as configurações" com a cor principal antes e depois

#### Scenario: Configurações no filtro da Auditoria
- **WHEN** o administrador filtra a Auditoria pelo tipo Configurações
- **THEN** a lista mostra só as mudanças das configurações da instalação
