## ADDED Requirements

### Requirement: Cor secundária

O administrador MUST poder escolher uma cor secundária, opcional, em hexadecimal, ou voltar ao padrão. Com ela MUST mudar, nos dois temas, a linha selecionada, a notificação não lida, a menção e o painel da página de entrada. Sem ela, esses destaques MUST ficar os do design system e o painel MUST seguir a cor principal (ADR-0146).

#### Scenario: Sem cor secundária
- **WHEN** a instalação não tem cor secundária
- **THEN** a linha selecionada, a notificação não lida e a menção usam as cores do design system, e o painel da entrada usa a cor principal

#### Scenario: Trocar a cor secundária
- **WHEN** o administrador escolhe uma cor secundária e salva
- **THEN** a linha selecionada, a notificação não lida, a menção e o painel da entrada passam a usar o matiz dela, nos dois temas, e o botão principal continua na cor principal

#### Scenario: Painel da entrada legível
- **WHEN** a cor secundária é clara, como um amarelo
- **THEN** o fundo do painel da entrada fica mais escuro que ela, até o texto branco ter pelo menos 4,5:1

### Requirement: Cor da barra lateral

O administrador MUST poder escolher a cor da barra lateral, opcional, em hexadecimal, ou voltar ao padrão. No tema claro, o fundo da barra lateral MUST ser a própria cor; no escuro, um tom escuro do mesmo matiz. Os textos, o item sob o ponteiro, as divisórias, o item ativo e o nome da instalação MUST acompanhar o fundo. Sem ela, a barra lateral MUST ser a do design system (ADR-0146).

#### Scenario: Sem cor da barra lateral
- **WHEN** a instalação não tem cor da barra lateral
- **THEN** a barra lateral usa as cores do design system, e só o item ativo segue a cor principal

#### Scenario: Barra lateral escura no tema claro
- **WHEN** o administrador escolhe um grafite para a barra lateral e salva
- **THEN** no tema claro a barra lateral fica grafite com os textos claros, e no escuro fica num tom escuro do mesmo matiz

#### Scenario: Barra lateral clara
- **WHEN** o administrador escolhe o branco para a barra lateral
- **THEN** no tema claro a barra lateral fica branca com os textos escuros

#### Scenario: Textos da barra lateral legíveis
- **WHEN** a barra lateral tem qualquer cor, clara ou escura, em qualquer tema
- **THEN** os textos dela têm pelo menos 4,5:1 sobre o fundo, e o filete do item ativo tem pelo menos 3:1

### Requirement: Contraste calculado sobre as cores

Nenhuma cor MUST ser recusada pelo contraste. A aplicação MUST escolher o texto branco ou o escuro sobre a cor, o que tiver contraste, e MUST escurecer ou clarear só o que é texto ou contorno até o mínimo do WCAG AA. Cada ajuste MUST aparecer embaixo do campo como aviso informativo, com ícone, cor e texto. As cores de status MUST NOT mudar (ADR-0146).

#### Scenario: Texto escuro sobre uma cor clara
- **WHEN** a cor principal é um amarelo sem 4,5:1 com o branco
- **THEN** o botão principal é o próprio amarelo com o texto escuro, e o campo avisa que o texto dos botões fica escuro

#### Scenario: Links legíveis com uma cor clara
- **WHEN** a cor principal é clara
- **THEN** os links e as abas ficam mais escuros que ela, até ter pelo menos 4,5:1 sobre as superfícies, e o campo avisa o ajuste

#### Scenario: Cor com bom contraste
- **WHEN** a cor principal tem pelo menos 4,5:1 com o branco
- **THEN** o botão principal e os links usam a própria cor no tema claro, e o campo diz que o texto branco tem bom contraste

#### Scenario: Botão no tema escuro
- **WHEN** a cor principal é muito escura e o tema é o escuro
- **THEN** o botão principal fica mais claro só até ter pelo menos 3:1 com o fundo da página, e o texto sobre ele tem pelo menos 4,5:1

#### Scenario: Cores de status iguais
- **WHEN** a instalação tem as três cores
- **THEN** as cores de sucesso, aviso, erro e informação continuam as do design system

### Requirement: Tela Cores

A seção Instalação da coluna da Administração MUST ter Cores, logo depois de Configurações gerais, com os campos da cor principal, da secundária e da barra lateral, a prévia das telas com Claro e Escuro e a barra de ações com Descartar e Salvar alterações, habilitados só quando algo mudou. Só o administrador MUST abrir a seção e salvar as cores (ADR-0046, ADR-0146).

#### Scenario: Cores na Administração
- **WHEN** o administrador escolhe Cores na coluna da Administração
- **THEN** a tela mostra os três campos, cada um com a amostra, o valor, "Voltar ao padrão", onde a cor aparece e os avisos de ajuste, e a prévia

#### Scenario: Prévia das cores antes de salvar
- **WHEN** o administrador digita ou escolhe uma cor
- **THEN** a prévia mostra a barra lateral, o cabeçalho, os botões, a lista, a menção e o painel da entrada com a cor na hora, e o resto da aplicação só muda depois de salvar

#### Scenario: Prévia nos dois temas
- **WHEN** o administrador escolhe Escuro na prévia
- **THEN** a prévia mostra as cores como ficam no tema escuro, sem mudar o tema da pessoa

#### Scenario: Salvar as cores
- **WHEN** o administrador muda as cores e escolhe Salvar alterações
- **THEN** as três cores são gravadas, a aplicação inteira passa a usá-las e a barra de ações fica desabilitada

#### Scenario: Descartar as cores
- **WHEN** o administrador muda as cores e escolhe Descartar
- **THEN** os campos e a prévia voltam às cores salvas e a barra de ações fica desabilitada

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

## MODIFIED Requirements

### Requirement: Configurações só do administrador

Só o administrador MUST poder alterar as configurações da instalação e abrir as seções Configurações gerais e Cores da Administração. O gerente de projeto e o usuário MUST receber a recusa (403), e quem não tem sessão MUST receber 401, sem nada mudar (ADR-0046).

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

### Requirement: Marca visível sem sessão

O nome, o logo, o logo para o tema escuro e as três cores da instalação MUST poder ser lidos sem sessão, porque a página de entrada os mostra (ADR-0009). Nenhuma outra configuração MUST ser exposta sem sessão.

#### Scenario: Marca na página de entrada
- **WHEN** alguém sem sessão abre a página de entrada
- **THEN** a página mostra o nome, o logo e o painel na cor secundária da instalação ou, sem ela, na cor principal

### Requirement: Mudança das configurações na auditoria

Cada mudança salva nas configurações da instalação MUST entrar na auditoria, com quem fez, quando e os campos com o valor antes e depois. O logo aparece pelo nome do arquivo enviado, o fuso pelo identificador e cada cor pelo hexadecimal, vazio para a cor padrão. A tela de Auditoria MUST ter o tipo de item Configurações no filtro (ADR-0039).

#### Scenario: Troca do nome na auditoria
- **WHEN** o administrador troca o nome da instalação
- **THEN** a auditoria ganha um registro "Alterou as configurações" com o nome antes e depois

#### Scenario: Troca do fuso na auditoria
- **WHEN** o administrador troca o fuso da instalação
- **THEN** a auditoria ganha um registro "Alterou as configurações" com o fuso horário antes e depois

#### Scenario: Troca da cor na auditoria
- **WHEN** o administrador troca a cor principal, a secundária ou a da barra lateral
- **THEN** a auditoria ganha um registro "Alterou as configurações" com cada cor trocada antes e depois

#### Scenario: Configurações no filtro da Auditoria
- **WHEN** o administrador filtra a Auditoria pelo tipo Configurações
- **THEN** a lista mostra só as mudanças das configurações da instalação

### Requirement: Tela Configurações gerais

A seção Instalação da coluna da Administração MUST ter Configurações gerais, com as seções Marca (nome exibido e logos), Regras gerais (fuso horário) e Arquivos (pasta dos projetos), a Prévia da barra lateral e do painel da entrada com as cores salvas e a barra de ações com Descartar e Salvar alterações, habilitados só quando algo mudou. Descartar MUST voltar aos valores salvos.

#### Scenario: Configurações gerais na Administração
- **WHEN** o administrador escolhe Configurações gerais na coluna da Administração
- **THEN** a tela mostra a seção Marca com o nome e o logo (com a dica "PNG ou SVG, até 1 MB"), sem campo de cor, a seção Regras gerais com o fuso atual e busca, a seção Arquivos com a pasta dos projetos e a Prévia

#### Scenario: Prévia antes de salvar
- **WHEN** o administrador digita outro nome ou escolhe um logo
- **THEN** a Prévia mostra o novo nome e o logo na hora, com as cores salvas, e o resto da aplicação só muda depois de salvar

#### Scenario: Descartar alterações
- **WHEN** o administrador muda o nome, o fuso ou a pasta dos projetos e escolhe Descartar
- **THEN** o formulário volta aos valores salvos e a barra de ações fica desabilitada

### Requirement: Cor principal

O administrador MUST poder trocar a cor principal da instalação por qualquer cor em hexadecimal, ou voltar à cor padrão do design system. Com a nova cor MUST mudar, nos temas claro e escuro, o botão principal, os links, as abas, os controles marcados, o anel de foco, o item ativo da barra lateral, o avatar do responsável e o selo; as cores de status MUST NOT mudar. Nenhuma cor MUST ser recusada pelo contraste (ADR-0146).

#### Scenario: Cor padrão
- **WHEN** ninguém trocou a cor principal, a secundária nem a da barra lateral
- **THEN** as telas usam as cores do design system, sem nenhuma mudança

#### Scenario: Trocar a cor principal
- **WHEN** o administrador escolhe outra cor e salva
- **THEN** o botão principal, os links, o item ativo da barra lateral e o selo passam a usar a nova cor, nos dois temas, e as cores de status continuam as mesmas

#### Scenario: Contraste nos dois temas
- **WHEN** a instalação tem qualquer cor principal
- **THEN** o texto do botão principal, os links sobre as superfícies e o item ativo da barra lateral têm contraste de pelo menos 4,5:1, no tema claro e no escuro

#### Scenario: Cor sem contraste
- **WHEN** o administrador escolhe uma cor clara, como um amarelo, sem 4,5:1 com o branco, e salva
- **THEN** a tela aceita a cor com o aviso dos ajustes, a API a grava, e o botão principal fica amarelo com o texto escuro

#### Scenario: Voltar ao padrão
- **WHEN** o administrador escolhe "Voltar ao padrão" e salva
- **THEN** a instalação volta à cor padrão do design system

### Requirement: Logo para o tema escuro

O administrador MUST poder enviar, trocar e remover um logo para o tema escuro, opcional, com as regras do logo. A barra lateral escura (a do tema escuro, ou a do tema claro com uma cor da barra lateral escura) MUST mostrar o logo escuro, quando há; sem ele, o logo. A barra lateral clara MUST mostrar o logo. O selo do painel da entrada MUST continuar com o logo (ADR-0100, ADR-0146).

#### Scenario: Logo para o tema escuro pela API
- **WHEN** o administrador envia o logo para o tema escuro e depois o remove
- **THEN** a marca traz o endereço dele enquanto existe, o logo continua o mesmo, e um arquivo que não é PNG nem SVG é recusado

#### Scenario: Enviar o logo para o tema escuro
- **WHEN** o administrador escolhe o logo para o tema escuro nas Configurações gerais e salva
- **THEN** o logo aparece num quadro escuro, é enviado, e "Remover logo escuro" o tira

#### Scenario: Logo escuro na barra lateral escura
- **WHEN** a barra lateral é escura e a instalação tem o logo escuro
- **THEN** a barra lateral e a Prévia mostram o logo escuro, e o selo da entrada mostra o logo

#### Scenario: Logo claro na barra lateral clara
- **WHEN** o tema é o claro, a barra lateral é clara e a instalação tem os dois logos
- **THEN** a barra lateral e a Prévia mostram o logo

#### Scenario: Sem logo escuro
- **WHEN** a barra lateral é escura e a instalação não tem o logo escuro
- **THEN** a barra lateral mostra o logo

#### Scenario: Logo escuro na auditoria
- **WHEN** o administrador envia o logo para o tema escuro
- **THEN** a auditoria registra o nome do arquivo

#### Scenario: Logo escuro na barra lateral escura do tema claro
- **WHEN** o tema é o claro, a cor da barra lateral é escura e a instalação tem o logo escuro
- **THEN** a barra lateral mostra o logo escuro

### Requirement: Seletor de cores

Clicar na amostra de uma cor MUST abrir o seletor do FiscoProjects, com a área de saturação e brilho, a barra de matiz, a cor atual ao lado da nova, o formato do valor (HEX, RGB ou HSL), o campo do valor, "Cancelar" e "Usar esta cor". O seletor MUST funcionar pelo ponteiro e pelo teclado, e a cor MUST ser guardada em `#RRGGBB` (ADR-0146).

#### Scenario: Escolher a cor pelo seletor
- **WHEN** o administrador arrasta na área de cor ou na barra de matiz e escolhe "Usar esta cor"
- **THEN** o campo mostra a cor em hexadecimal, a prévia a mostra e o formulário fica pronto para salvar

#### Scenario: Seletor pelo teclado
- **WHEN** o administrador usa as setas na área de cor e na barra de matiz
- **THEN** a saturação, o brilho e o matiz mudam passo a passo, e a prévia acompanha

#### Scenario: Cancelar o seletor
- **WHEN** o administrador muda a cor no seletor e escolhe "Cancelar" ou aperta Esc
- **THEN** o seletor fecha, o campo e a prévia voltam à cor de antes

#### Scenario: Cor em vários formatos
- **WHEN** o administrador digita ou cola `#0F766E`, `0f766e`, `#0f7`, `rgb(15, 118, 110)`, `15, 118, 110` ou `hsl(175, 77%, 26%)` no campo
- **THEN** a cor é aceita e guardada em `#RRGGBB`

#### Scenario: Valor que não é cor
- **WHEN** o administrador digita um valor que não é cor em nenhum dos formatos
- **THEN** o campo mostra o erro com exemplos dos formatos aceitos, e a cor salva não muda

#### Scenario: Seletor no celular
- **WHEN** o administrador abre o seletor numa tela estreita
- **THEN** o seletor sobe como uma folha do pé da tela, por cima de um fundo escurecido

### Requirement: Logo na aba do navegador

A aba do navegador MUST mostrar o logo da instalação, quando há; sem logo, MUST mostrar o selo da barra lateral (o ícone da linha do tempo sobre a cor principal). O ícone MUST mudar quando o logo ou a cor principal muda, e MUST entrar só depois de a marca chegar (ADR-0108).

#### Scenario: Logo na aba
- **WHEN** a instalação tem logo e alguém abre uma tela
- **THEN** a aba do navegador mostra o logo

#### Scenario: Selo na aba sem logo
- **WHEN** a instalação não tem logo e alguém abre a página de entrada
- **THEN** a aba mostra o selo, na cor principal

#### Scenario: Selo na cor principal
- **WHEN** a instalação escolheu uma cor principal e não tem logo
- **THEN** o selo da aba usa a cor principal, com o ícone branco ou escuro, o que tiver contraste sobre ela
