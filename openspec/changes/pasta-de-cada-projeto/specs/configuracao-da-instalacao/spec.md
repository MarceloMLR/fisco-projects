## ADDED Requirements

### Requirement: Pasta dos projetos

O administrador MUST poder escolher, na seção Arquivos das Configurações gerais, a pasta dos projetos: o caminho, dentro da raiz do armazenamento, onde as pastas dos projetos são criadas, com as pastas separadas por "/". Vazia, que é o padrão, elas MUST ficar na própria raiz. A pasta MUST ser criada quando faltar e valer só para os projetos que ainda não têm pasta. Um caminho que o armazenamento não aceita MUST ser recusado, e a mudança MUST entrar na auditoria (ADR-0003, ADR-0030, ADR-0121).

#### Scenario: Pasta dos projetos padrão
- **WHEN** ninguém escolheu a pasta dos projetos
- **THEN** as Configurações gerais mostram o campo vazio, e as pastas dos projetos ficam na raiz do armazenamento

#### Scenario: Escolher a pasta dos projetos
- **WHEN** o administrador salva "Clientes/Projetos" e um projeto novo guarda o primeiro arquivo
- **THEN** a pasta do projeto é criada em "Clientes/Projetos", com as pastas que faltavam

#### Scenario: Pasta dos projetos inválida
- **WHEN** alguém pede a pasta dos projetos "Projetos/../Outros" ou "Projetos/Fin:2026"
- **THEN** a API recusa com 400 no campo da pasta, e nada muda

#### Scenario: Pasta dos projetos na auditoria
- **WHEN** o administrador troca a pasta dos projetos
- **THEN** a auditoria ganha um registro "Alterou as configurações" com a pasta dos projetos antes e depois

## MODIFIED Requirements

### Requirement: Tela Configurações gerais

A seção Instalação da coluna da Administração MUST ter Configurações gerais, com as seções Marca (nome exibido, logo e cor principal), Regras gerais (fuso horário) e Arquivos (pasta dos projetos), a Prévia da barra lateral e do painel da entrada como vão ficar e a barra de ações com Descartar e Salvar alterações, habilitados só quando algo mudou. Descartar MUST voltar aos valores salvos.

#### Scenario: Configurações gerais na Administração
- **WHEN** o administrador escolhe Configurações gerais na coluna da Administração
- **THEN** a tela mostra a seção Marca com o nome, o logo (com a dica "PNG ou SVG, até 1 MB") e a cor atuais (com a amostra, o campo hexadecimal, "Voltar ao padrão" e o resultado do contraste), a seção Regras gerais com o fuso atual e busca, a seção Arquivos com a pasta dos projetos e a Prévia

#### Scenario: Prévia antes de salvar
- **WHEN** o administrador digita outro nome, escolhe um logo ou digita outra cor
- **THEN** a Prévia mostra o novo nome, o logo e a cor na hora, e o resto da aplicação só muda depois de salvar

#### Scenario: Descartar alterações
- **WHEN** o administrador muda o nome, o fuso, a cor ou a pasta dos projetos e escolhe Descartar
- **THEN** o formulário volta aos valores salvos e a barra de ações fica desabilitada
