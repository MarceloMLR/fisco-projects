# configuracao-da-instalacao Specification

## Purpose

Define as configurações da instalação que só o administrador muda, na tela Configurações gerais, e onde a aplicação as mostra: o nome e o logo da marca, visíveis também sem sessão, e a auditoria das mudanças (ADR-0003, ADR-0009, ADR-0030, ADR-0039, ADR-0046).

## Requirements

### Requirement: Configurações só do administrador

Só o administrador MUST poder alterar as configurações da instalação e abrir a seção Configurações gerais da Administração. O gerente de projeto e o usuário MUST receber a recusa (403), e quem não tem sessão MUST receber 401, sem nada mudar (ADR-0046).

#### Scenario: Administrador altera as configurações
- **WHEN** o administrador salva um novo nome para a instalação
- **THEN** a alteração é aceita e o novo nome passa a valer

#### Scenario: Gerente de projeto não altera as configurações
- **WHEN** um gerente de projeto pede para trocar o nome ou o logo da instalação
- **THEN** a API responde 403 e nada muda

#### Scenario: Usuário não altera as configurações
- **WHEN** um usuário pede para trocar o nome ou o logo da instalação
- **THEN** a API responde 403 e nada muda

#### Scenario: Alteração sem sessão
- **WHEN** alguém sem sessão pede para trocar o nome ou o logo da instalação
- **THEN** a API responde 401 e nada muda

### Requirement: Nome da instalação

O administrador MUST poder trocar o nome exibido da instalação, obrigatório e com até 60 caracteres, sem os espaços das pontas. Enquanto ninguém o troca, o nome MUST ser "FiscoProjects". O nome MUST aparecer no topo da barra lateral, no painel da página de entrada e no título da aba do navegador (ADR-0003, ADR-0009).

#### Scenario: Nome padrão
- **WHEN** ninguém trocou o nome da instalação
- **THEN** a barra lateral, a página de entrada e o título da aba mostram "FiscoProjects"

#### Scenario: Trocar o nome
- **WHEN** o administrador troca o nome exibido e salva
- **THEN** a barra lateral, a página de entrada e o título da aba passam a mostrar o novo nome

#### Scenario: Nome inválido
- **WHEN** o nome fica vazio ou passa de 60 caracteres
- **THEN** o campo mostra o motivo, a API recusa com 400 e nada muda

### Requirement: Logo da instalação

O administrador MUST poder enviar, trocar e remover o logo da instalação, um PNG ou SVG de até 1 MB. Com logo, ele MUST tomar o lugar do selo no topo da barra lateral, com até 28 px de altura, e no painel da página de entrada; sem logo, aparece o selo. Arquivo de outro formato, acima de 1 MB ou SVG com script MUST ser recusado. O logo MUST ser servido de forma que um SVG nunca execute scripts (ADR-0003, ADR-0030).

#### Scenario: Enviar o logo
- **WHEN** o administrador envia um logo PNG e salva
- **THEN** a barra lateral e a página de entrada mostram o logo no lugar do selo

#### Scenario: Remover o logo
- **WHEN** o administrador remove o logo e salva
- **THEN** a barra lateral e a página de entrada voltam a mostrar o selo

#### Scenario: Logo recusado
- **WHEN** o administrador envia um JPG, um arquivo acima de 1 MB ou um SVG com script
- **THEN** o envio é recusado com o motivo e o logo anterior continua

#### Scenario: Logo sem scripts
- **WHEN** alguém abre o endereço do logo
- **THEN** a resposta proíbe a execução de scripts e a troca do tipo do arquivo pelo navegador

### Requirement: Marca visível sem sessão

O nome, o logo e a cor principal da instalação MUST poder ser lidos sem sessão, porque a página de entrada os mostra (ADR-0009). Nenhuma outra configuração MUST ser exposta sem sessão.

#### Scenario: Marca na página de entrada
- **WHEN** alguém sem sessão abre a página de entrada
- **THEN** a página mostra o nome, o logo e o painel na cor principal da instalação

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

### Requirement: Tela Configurações gerais

A seção Instalação da coluna da Administração MUST ter Configurações gerais, com as seções Marca (nome exibido, logo e cor principal) e Regras gerais (fuso horário), a Prévia da barra lateral e do painel da entrada como vão ficar e a barra de ações com Descartar e Salvar alterações, habilitados só quando algo mudou. Descartar MUST voltar aos valores salvos.

#### Scenario: Configurações gerais na Administração
- **WHEN** o administrador escolhe Configurações gerais na coluna da Administração
- **THEN** a tela mostra a seção Marca com o nome, o logo (com a dica "PNG ou SVG, até 1 MB") e a cor atuais (com a amostra, o campo hexadecimal, "Voltar ao padrão" e o resultado do contraste), a seção Regras gerais com o fuso atual e busca, e a Prévia

#### Scenario: Prévia antes de salvar
- **WHEN** o administrador digita outro nome, escolhe um logo ou digita outra cor
- **THEN** a Prévia mostra o novo nome, o logo e a cor na hora, e o resto da aplicação só muda depois de salvar

#### Scenario: Descartar alterações
- **WHEN** o administrador muda o nome, o fuso ou a cor e escolhe Descartar
- **THEN** o formulário volta aos valores salvos e a barra de ações fica desabilitada

### Requirement: Fuso da instalação

O administrador MUST poder escolher o fuso horário da instalação entre os fusos IANA, com o padrão America/Sao_Paulo. Todas as datas e horas das telas MUST aparecer nesse fuso, qualquer que seja o fuso do navegador, e os filtros por dia MUST começar e terminar à meia-noite desse fuso. Um fuso desconhecido MUST ser recusado. O fuso MUST ser lido só com sessão (ADR-0013).

#### Scenario: Fuso padrão
- **WHEN** ninguém escolheu o fuso da instalação
- **THEN** o fuso é America/Sao_Paulo, e as Configurações gerais mostram o nome do fuso de Brasília com o identificador America/Sao_Paulo

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
