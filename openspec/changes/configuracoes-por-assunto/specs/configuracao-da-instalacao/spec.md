## RENAMED Requirements

- FROM: `### Requirement: Tela Cores`
- TO: `### Requirement: Tela Visual`

## MODIFIED Requirements

### Requirement: Configurações só do administrador

Só o administrador MUST poder alterar as configurações da instalação e abrir as seções Configurações gerais e Visual da Administração. O gerente de projeto e o usuário MUST receber a recusa (403), e quem não tem sessão MUST receber 401, sem nada mudar (ADR-0046, ADR-0150).

#### Scenario: Administrador altera as configurações
- **WHEN** o administrador salva um novo nome para a instalação
- **THEN** a alteração é aceita e o novo nome passa a valer

#### Scenario: Gerente de projeto não altera as configurações
- **WHEN** um gerente de projeto pede para trocar o nome, o logo, as regras gerais ou a pasta dos projetos da instalação
- **THEN** a API responde 403 e nada muda

#### Scenario: Usuário não altera as configurações
- **WHEN** um usuário pede para trocar o nome, o logo, as regras gerais ou a pasta dos projetos da instalação
- **THEN** a API responde 403 e nada muda

#### Scenario: Alteração sem sessão
- **WHEN** alguém sem sessão pede para trocar o nome, o logo, as regras gerais ou a pasta dos projetos da instalação
- **THEN** a API responde 401 e nada muda

### Requirement: Tela Configurações gerais

A seção Instalação da coluna da Administração MUST ter Configurações gerais, numa coluna, só com as Regras gerais (fuso, horário do resumo diário, guarda das notificações e da auditoria, horas por dia de trabalho) e o Microsoft 365, e com a barra de ações Descartar e Salvar alterações das Regras gerais, habilitados só quando alguma regra mudou. Descartar MUST voltar aos valores salvos. O Microsoft 365 MUST continuar gravando na hora (ADR-0150).

#### Scenario: Configurações gerais na Administração
- **WHEN** o administrador escolhe Configurações gerais na coluna da Administração
- **THEN** a tela mostra as Regras gerais, com o fuso atual e busca, e o Microsoft 365, sem a marca, sem a pasta dos projetos e sem prévia

#### Scenario: Prévia antes de salvar
- **WHEN** o administrador abre Configurações gerais
- **THEN** a tela não tem prévia da marca, que fica em Visual, junto com o nome, os logos e as cores

#### Scenario: Descartar alterações
- **WHEN** o administrador muda o fuso ou o horário do resumo e escolhe Descartar
- **THEN** o formulário volta aos valores salvos e a barra de ações fica desabilitada

### Requirement: Tela Visual

A seção Instalação da coluna da Administração MUST ter Visual, logo depois de Configurações gerais, com a Marca (o nome exibido, o logo e o logo para o tema escuro) e as Cores (a principal, a secundária e a da barra lateral), a prévia das telas com Claro e Escuro e a barra de ações com Descartar e Salvar alterações, habilitados só quando algo mudou. Salvar MUST gravar o nome, os logos e as cores que mudaram. O endereço antigo de Cores MUST levar a Visual (ADR-0146, ADR-0150).

#### Scenario: Cores na Administração
- **WHEN** o administrador escolhe Visual na coluna da Administração
- **THEN** a tela mostra os três campos de cor, cada um com a amostra, o valor, "Voltar ao padrão", onde a cor aparece e os avisos de ajuste, e a prévia

#### Scenario: Marca em Visual
- **WHEN** o administrador abre Visual
- **THEN** a Marca vem antes das cores, com o nome exibido, o logo e o logo para o tema escuro, com a dica "PNG ou SVG, até 1 MB"

#### Scenario: Endereço antigo de Cores
- **WHEN** alguém abre o endereço da antiga seção Cores
- **THEN** a tela Visual abre no lugar dela

#### Scenario: Prévia da marca antes de salvar
- **WHEN** o administrador digita outro nome ou escolhe um logo
- **THEN** a prévia mostra o novo nome e o logo na hora, e o resto da aplicação só muda depois de salvar

#### Scenario: Prévia das cores antes de salvar
- **WHEN** o administrador digita ou escolhe uma cor
- **THEN** a prévia mostra a barra lateral, o cabeçalho, os botões, a lista, a menção e o painel e o fundo da entrada com a cor na hora, e o resto da aplicação só muda depois de salvar

#### Scenario: Prévia nos dois temas
- **WHEN** o administrador escolhe Escuro na prévia
- **THEN** a prévia mostra as cores como ficam no tema escuro, sem mudar o tema da pessoa

#### Scenario: Salvar a marca e as cores
- **WHEN** o administrador muda o nome, o logo e uma cor e escolhe Salvar alterações
- **THEN** o nome, o logo e as cores são gravados, a aplicação inteira passa a usá-los e a barra de ações fica desabilitada

#### Scenario: Salvar as cores
- **WHEN** o administrador muda só as cores e escolhe Salvar alterações
- **THEN** as três cores são gravadas, o nome e os logos ficam como estavam e a barra de ações fica desabilitada

#### Scenario: Descartar as cores
- **WHEN** o administrador muda o nome, o logo ou as cores e escolhe Descartar
- **THEN** os campos e a prévia voltam ao que está salvo e a barra de ações fica desabilitada

#### Scenario: Cor fora do formato pela API
- **WHEN** alguém manda à API uma cor que não é `#RRGGBB`
- **THEN** a API responde 400 no campo dessa cor e nada muda

#### Scenario: Gerente de projeto não altera as cores
- **WHEN** um gerente de projeto pede para trocar as cores da instalação
- **THEN** a API responde 403 e nada muda

#### Scenario: Usuário não altera as cores
- **WHEN** um usuário pede para trocar as cores da instalação
- **THEN** a API responde 403 e nada muda

#### Scenario: Cores sem sessão
- **WHEN** alguém sem sessão pede para trocar as cores da instalação
- **THEN** a API responde 401 e nada muda

### Requirement: Logo para o tema escuro

O administrador MUST poder enviar, trocar e remover um logo para o tema escuro, opcional, com as regras do logo. A barra lateral escura (a do tema escuro, ou a do tema claro com uma cor da barra lateral escura) MUST mostrar o logo escuro, quando há; sem ele, o logo. A barra lateral clara MUST mostrar o logo. O selo do painel da entrada MUST continuar com o logo (ADR-0100, ADR-0146).

#### Scenario: Logo para o tema escuro pela API
- **WHEN** o administrador envia o logo para o tema escuro e depois o remove
- **THEN** a marca traz o endereço dele enquanto existe, o logo continua o mesmo, e um arquivo que não é PNG nem SVG é recusado

#### Scenario: Enviar o logo para o tema escuro
- **WHEN** o administrador escolhe o logo para o tema escuro em Visual e salva
- **THEN** o logo aparece num quadro escuro, é enviado, e "Remover logo escuro" o tira

#### Scenario: Logo escuro na barra lateral escura
- **WHEN** a barra lateral é escura e a instalação tem o logo escuro
- **THEN** a barra lateral e a prévia mostram o logo escuro, e o selo da entrada mostra o logo

#### Scenario: Logo claro na barra lateral clara
- **WHEN** o tema é o claro, a barra lateral é clara e a instalação tem os dois logos
- **THEN** a barra lateral e a prévia mostram o logo

#### Scenario: Sem logo escuro
- **WHEN** a barra lateral é escura e a instalação não tem o logo escuro
- **THEN** a barra lateral mostra o logo

#### Scenario: Logo escuro na auditoria
- **WHEN** o administrador envia o logo para o tema escuro
- **THEN** a auditoria registra o nome do arquivo

#### Scenario: Logo escuro na barra lateral escura do tema claro
- **WHEN** o tema é o claro, a cor da barra lateral é escura e a instalação tem o logo escuro
- **THEN** a barra lateral mostra o logo escuro

### Requirement: Pasta dos projetos

O administrador MUST poder escolher, em Pastas padrão, a pasta dos projetos: o caminho, dentro da raiz do armazenamento, onde as pastas dos projetos são criadas, com as pastas separadas por "/". Vazia, que é o padrão, elas MUST ficar na própria raiz. A pasta MUST ser criada quando faltar e valer só para os projetos que ainda não têm pasta. Um caminho que o armazenamento não aceita MUST ser recusado, e a mudança MUST entrar na auditoria (ADR-0003, ADR-0030, ADR-0121, ADR-0150).

#### Scenario: Pasta dos projetos padrão
- **WHEN** ninguém escolheu a pasta dos projetos
- **THEN** Pastas padrão diz que as pastas dos projetos ficam na raiz do armazenamento

#### Scenario: Alterar a pasta dos projetos pela tela
- **WHEN** o administrador escolhe "Alterar" em Pastas padrão, digita "Clientes/Projetos" e salva
- **THEN** o diálogo fecha, a linha mostra "Clientes/Projetos" e as outras configurações ficam como estavam

#### Scenario: Escolher a pasta dos projetos
- **WHEN** o administrador salva "Clientes/Projetos" e cria um projeto
- **THEN** a pasta do projeto é criada em "Clientes/Projetos", com as pastas que faltavam

#### Scenario: Pasta dos projetos inválida
- **WHEN** alguém pede a pasta dos projetos "Projetos/../Outros" ou "Projetos/Fin:2026"
- **THEN** a API recusa com 400 no campo da pasta, nada muda, e o diálogo mostra o motivo no campo

#### Scenario: Pasta dos projetos na auditoria
- **WHEN** o administrador troca a pasta dos projetos
- **THEN** a auditoria ganha um registro "Alterou as configurações" com a pasta dos projetos antes e depois

## ADDED Requirements

### Requirement: Cada tela grava o que mostra

Cada tela das configurações MUST gravar só o que mostra: as Regras gerais sem mudar o nome nem a pasta dos projetos, o nome sem mudar as regras, e a pasta dos projetos sem mudar o resto. Cada gravação MUST ser só do administrador e entrar na auditoria como as outras mudanças das configurações (ADR-0039, ADR-0150).

#### Scenario: Regras sem mudar a marca
- **WHEN** o administrador salva as Regras gerais
- **THEN** o nome exibido e a pasta dos projetos ficam como estavam

#### Scenario: Nome sem mudar as regras
- **WHEN** o administrador troca o nome exibido
- **THEN** o fuso, o horário do resumo, as guardas, as horas por dia e a pasta dos projetos ficam como estavam

#### Scenario: Pasta dos projetos sem mudar o resto
- **WHEN** o administrador troca a pasta dos projetos
- **THEN** o nome exibido e as Regras gerais ficam como estavam
