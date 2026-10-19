## ADDED Requirements

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

O nome e o logo da instalação MUST poder ser lidos sem sessão, porque a página de entrada os mostra (ADR-0009). Nenhuma outra configuração MUST ser exposta sem sessão.

#### Scenario: Marca na página de entrada
- **WHEN** alguém sem sessão abre a página de entrada
- **THEN** a página mostra o nome e o logo da instalação

### Requirement: Mudança das configurações na auditoria

Cada mudança salva nas configurações da instalação MUST entrar na auditoria, com quem fez, quando e os campos com o valor antes e depois; o logo aparece pelo nome do arquivo enviado. A tela de Auditoria MUST ter o tipo de item Configurações no filtro (ADR-0039).

#### Scenario: Troca do nome na auditoria
- **WHEN** o administrador troca o nome da instalação
- **THEN** a auditoria ganha um registro "Alterou as configurações" com o nome antes e depois

#### Scenario: Configurações no filtro da Auditoria
- **WHEN** o administrador filtra a Auditoria pelo tipo Configurações
- **THEN** a lista mostra só as mudanças das configurações da instalação

### Requirement: Tela Configurações gerais

A seção Instalação da coluna da Administração MUST ter Configurações gerais, com a seção Marca (nome exibido e logo, com a dica "PNG ou SVG, até 1 MB"), a Prévia da barra lateral e do painel da entrada como vão ficar, e a barra de ações com Descartar e Salvar alterações, habilitados só quando algo mudou. Descartar MUST voltar aos valores salvos.

#### Scenario: Configurações gerais na Administração
- **WHEN** o administrador escolhe Configurações gerais na coluna da Administração
- **THEN** a tela mostra a seção Marca com o nome e o logo atuais e a Prévia

#### Scenario: Prévia antes de salvar
- **WHEN** o administrador digita outro nome ou escolhe um logo
- **THEN** a Prévia mostra o novo nome e o logo na hora, e o resto da aplicação só muda depois de salvar

#### Scenario: Descartar alterações
- **WHEN** o administrador muda o nome e escolhe Descartar
- **THEN** o formulário volta aos valores salvos e a barra de ações fica desabilitada
