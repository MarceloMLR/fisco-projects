# estrutura-das-telas Specification

## Purpose

Define o tema visual comum a todas as telas do FiscoProjects (cores, fontes e temas claro e escuro, vindos do design system aprovado) e a estrutura que envolve cada tela no computador e no celular (ADR-0011, ADR-0012, ADR-0014, ADR-0050, ADR-0062 a ADR-0067).

## Requirements

### Requirement: Cores e fontes do design system

As cores das telas MUST vir só dos tokens do design system FiscoProjects, com valores próprios para o tema claro e o escuro; nenhuma cor MUST ser escrita direto no código. As fontes MUST ser a IBM Plex Sans e a IBM Plex Mono, servidas pela própria aplicação, sem serviço de terceiros. Os pares de texto e fundo da estrutura MUST ter contraste mínimo AA (4,5:1) nos dois temas.

#### Scenario: Cores dos tokens nos dois temas
- **WHEN** uma tela é exibida no tema claro ou no escuro
- **THEN** cada cor usada vem de um token do design system, com o valor desse tema

#### Scenario: Cor escrita direto no código
- **WHEN** o código do frontend traz uma cor escrita direto, fora da cópia dos tokens
- **THEN** os testes do frontend falham e apontam o arquivo

#### Scenario: Fonte servida pela aplicação
- **WHEN** uma tela é exibida
- **THEN** o texto usa a IBM Plex Sans, e os arquivos da fonte vêm do endereço da própria aplicação

#### Scenario: Contraste mínimo
- **WHEN** a estrutura é exibida no tema claro ou no escuro
- **THEN** cada par de texto e fundo da barra lateral, do cabeçalho e do conteúdo tem contraste de pelo menos 4,5:1

### Requirement: Barra lateral no computador

Em telas de 900px ou mais, a aplicação MUST mostrar uma barra lateral fixa, da altura da janela, com o nome da instalação no topo, depois do logo da instalação ou, sem logo, do selo, e os itens Início, Projetos, Chamados e Minhas horas e, só para o administrador, Administração, destacando o item da página atual.

#### Scenario: Navegação principal
- **WHEN** a aplicação é aberta numa tela de 900px ou mais
- **THEN** a barra lateral mostra o nome da instalação e os itens Início, Projetos, Chamados e Minhas horas

#### Scenario: Item da página atual
- **WHEN** a pessoa está numa página da navegação principal
- **THEN** o item dessa página fica marcado como o atual, e só ele

#### Scenario: Administração na barra lateral
- **WHEN** o administrador abre a aplicação
- **THEN** a barra lateral mostra também o item Administração, que quem não é administrador não vê

### Requirement: Cabeçalho com a trilha

Sobre o conteúdo, a aplicação MUST mostrar um cabeçalho da mesma cor do conteúdo e sem linha, com a trilha da página, cujos níveis acima levam às áreas em que a página fica e cujo último nível é a página atual, e, à direita, o avatar da pessoa na sessão, que abre o menu da conta. O menu da conta MUST mostrar o nome e o e-mail da pessoa, o item Meu perfil, a escolha do tema (claro, escuro e automático) e a opção Sair.

#### Scenario: Trilha da página
- **WHEN** a pessoa abre uma página
- **THEN** o cabeçalho mostra a trilha com o nome da página como último nível, marcado como a página atual

#### Scenario: Trilha de uma página dentro de uma área
- **WHEN** o administrador abre a tela Usuários
- **THEN** a trilha mostra Administração e Usuários, e Administração leva à área de Administração

#### Scenario: Conta no cabeçalho
- **WHEN** a pessoa abre o menu do avatar no cabeçalho
- **THEN** o menu mostra o nome e o e-mail dela, Meu perfil, o tema escolhido entre claro, escuro e automático e a opção Sair, e o avatar traz as iniciais do nome

### Requirement: Estrutura no celular

Em telas menores que 900px, a aplicação MUST trocar a barra lateral por uma barra superior com o botão de menu e o título da página; o menu MUST abrir por cima do conteúdo com o nome e o e-mail da pessoa na sessão, a mesma navegação e o item Meu perfil, e fechar ao escolher um item.

#### Scenario: Menu no celular
- **WHEN** a aplicação é aberta numa tela menor que 900px e a pessoa toca no botão de menu
- **THEN** o menu abre com o nome e o e-mail da pessoa, os itens Início, Projetos, Chamados e Minhas horas e o item Meu perfil, e escolher um item leva à página e fecha o menu

### Requirement: Conteúdo na altura da janela

A estrutura MUST ocupar exatamente a altura da janela: a página não rola como um todo, e o conteúdo rola por dentro da sua área (ADR-0066).

#### Scenario: Janela sem rolagem
- **WHEN** uma tela é exibida
- **THEN** a estrutura tem a altura da janela, sem rolagem própria, e só a área do conteúdo pode rolar

### Requirement: Endereços das telas

Cada página da navegação principal MUST ter o próprio endereço, e um endereço que não corresponde a nenhuma tela MUST levar ao Início.

#### Scenario: Endereço de uma página
- **WHEN** alguém abre direto o endereço de Projetos ou de Chamados
- **THEN** a página correspondente é exibida, com o seu título

#### Scenario: Endereço desconhecido
- **WHEN** alguém abre um endereço que não corresponde a nenhuma tela
- **THEN** a aplicação mostra o Início

### Requirement: Detalhes em gaveta sobreposta

Ao escolher um item de uma lista, com um clique ou com Enter, a tela MUST abrir os detalhes numa gaveta à direita, por cima da tela, com o fundo escurecido, e a lista MUST continuar com toda a largura. A gaveta MUST ter até 640 px de largura e, no celular, a largura da tela. Ela MUST fechar pelo botão Fechar, pela tecla Esc ou por um clique fora dela, e o foco MUST voltar para a lista.

#### Scenario: Detalhes abertos por cima da lista
- **WHEN** a pessoa escolhe um item de uma lista
- **THEN** os detalhes aparecem numa gaveta à direita, por cima da tela, com o fundo escurecido, e a lista não perde largura

#### Scenario: Fechar os detalhes
- **WHEN** a gaveta está aberta e a pessoa aperta Esc, clica fora dela ou no botão Fechar
- **THEN** a gaveta fecha e o foco volta para a lista

#### Scenario: Detalhes no celular
- **WHEN** a pessoa escolhe um item de uma lista no celular
- **THEN** a gaveta ocupa a largura da tela

### Requirement: Botões com ícone

Num botão com ícone, o ícone MUST ficar a 8px do texto (`space-2`), só pelo espaço do botão, como no design system; o ícone do começo MUST avançar 4px sobre o respiro do botão, e o do fim MUST NOT ter margem.

#### Scenario: Ícone perto do texto
- **WHEN** uma tela mostra um botão com ícone no começo ou no fim
- **THEN** o ícone fica a 8px do texto, sem a margem extra do MUI

### Requirement: Voltar do navegador

Um Voltar do navegador MUST voltar uma página. Um link ou botão dentro de uma linha de tabela MUST fazer só a ação dele, sem escolher a linha também; a gaveta aberta pelo endereço MUST abrir e fechar sem entrar no histórico; e o formulário salvo ou cancelado MUST ser trocado pela página seguinte (ADR-0105).

#### Scenario: Link dentro da linha
- **WHEN** a pessoa clica no link ou dá Enter nele, dentro de uma linha de tabela
- **THEN** só o link age, e a linha não é escolhida; o clique fora do link escolhe a linha

#### Scenario: Gaveta fora do histórico
- **WHEN** a pessoa abre e fecha o termo de aceite e depois volta uma vez
- **THEN** ela sai da aba de termos

#### Scenario: Novo projeto pela tela
- **WHEN** a pessoa cria um projeto
- **THEN** a página do projeto troca o formulário no histórico

#### Scenario: Cancelar o Novo projeto
- **WHEN** a pessoa cancela o Novo projeto
- **THEN** a lista de projetos troca o formulário no histórico

#### Scenario: Abrir chamado pela aba do projeto
- **WHEN** a pessoa abre um chamado pela aba do projeto e depois volta uma vez
- **THEN** ela volta à aba Chamados do projeto

### Requirement: Primeira abertura mais rápida

A página do projeto MUST pedir a leitura da aba junto com o projeto, sem esperar por ele. Apontar com o mouse um projeto ou um chamado das listas, ou um projeto da barra lateral, MUST começar a ler o que a página vai mostrar, só o que ainda não está guardado. Abrir um projeto no computador MUST começar a carregar o código da Linha do tempo (ADR-0106).

#### Scenario: Aba pedida junto com o projeto
- **WHEN** a pessoa abre a Visão geral de um projeto
- **THEN** a visão geral é pedida antes de o projeto chegar

#### Scenario: Projeto pedido ao apontar a linha
- **WHEN** a pessoa aponta com o mouse um projeto da lista
- **THEN** o projeto e a visão geral são pedidos

#### Scenario: Linha do tempo carregada antes
- **WHEN** um projeto abre no computador
- **THEN** o código da Linha do tempo começa a carregar

### Requirement: Confirmações no alto da tela

Toda confirmação curta MUST aparecer no alto, no meio: no computador, na faixa do cabeçalho e no meio da área de conteúdo, sem cobrir a barra lateral; no celular, logo abaixo da barra do topo. A posição MUST vir do tema, e nenhuma tela MUST escolher a própria (ADR-0109).

#### Scenario: Confirmação no alto, no meio
- **WHEN** uma tela confirma algo que acabou de dar certo
- **THEN** a confirmação aparece no alto, no meio, abaixo da barra do topo no celular

#### Scenario: Toda confirmação no mesmo lugar
- **WHEN** qualquer tela mostra uma confirmação
- **THEN** ela usa a posição do tema, sem uma posição própria

### Requirement: Cartão da pessoa

O avatar de outra pessoa MUST abrir, pelo clique ou pelo teclado, o cartão dela junto do avatar: o avatar, o nome, o papel naquela tela quando a tela o conhece, o e-mail e "Copiar e-mail", que copia e confirma no alto da tela. Da pessoa anonimizada, o cartão MUST dizer que o nome e o e-mail foram removidos. Abrir o cartão numa linha de tabela MUST NOT escolher a linha (ADR-0118).

#### Scenario: Cartão pelo avatar
- **WHEN** alguém clica no avatar de Diego Torres numa mensagem do chamado
- **THEN** abre o cartão com o avatar, o nome, o papel dele no chamado e o e-mail

#### Scenario: Copiar o e-mail do cartão
- **WHEN** a pessoa escolhe "Copiar e-mail" no cartão
- **THEN** o e-mail vai para a área de transferência e a tela confirma "E-mail copiado."

#### Scenario: Cartão numa linha de tabela
- **WHEN** alguém abre o cartão pelo avatar numa linha de tabela que abre um item
- **THEN** só o cartão abre, sem abrir o item da linha

#### Scenario: Pessoa anonimizada no cartão
- **WHEN** alguém abre o cartão de uma pessoa anonimizada
- **THEN** o cartão mostra o identificador anônimo e diz que o nome e o e-mail foram removidos, sem "Copiar e-mail"

### Requirement: Foto no avatar

O avatar de uma pessoa MUST mostrar a foto dela no Microsoft 365 quando a foto carrega, cobrindo o círculo, sem contorno na cor do papel, e MUST continuar com as iniciais enquanto a foto não carrega, quando a pessoa não tem foto e quando a foto falha. Vale para o avatar que abre o cartão, o do cartão, o da pessoa na sessão, o do detalhe da pessoa em Membros e o das listas da menção (ADR-0124).

#### Scenario: Avatar com a foto
- **WHEN** a foto de Helena Duarte carrega no avatar dela numa mensagem do chamado
- **THEN** o avatar mostra a foto no lugar das iniciais, sem contorno

#### Scenario: Avatar sem foto
- **WHEN** a foto de uma pessoa não existe
- **THEN** o avatar continua com as iniciais

### Requirement: Botões em andamento

Todo botão e botão de ícone que manda algo ao servidor MUST ficar em andamento do clique até a resposta: o ícone de carregamento no lugar do ícone, ou antes do texto, o rótulo e a largura mantidos, inativo e ocupado para as tecnologias assistivas. Quando vai ao servidor, o andamento MUST durar pelo menos meio segundo (ADR-0132). Um novo clique ou Enter durante o andamento MUST NOT mandar a ação de novo. Os itens dos menus ficam fora (ADR-0131).

#### Scenario: Botão em andamento
- **WHEN** a pessoa clica num botão com ícone que manda algo ao servidor, e a resposta demora
- **THEN** o ícone dá lugar ao ícone de carregamento na hora, o rótulo continua o mesmo, e o botão fica inativo até a resposta

#### Scenario: Botão sem ícone em andamento
- **WHEN** a pessoa clica num botão sem ícone, e a resposta demora
- **THEN** o ícone de carregamento aparece antes do texto até a resposta

#### Scenario: Botão de ícone em andamento
- **WHEN** a pessoa clica num botão de ícone que manda algo ao servidor
- **THEN** o ícone dá lugar ao ícone de carregamento até a resposta

#### Scenario: Clique repetido durante o andamento
- **WHEN** a pessoa clica de novo num botão em andamento
- **THEN** nada acontece, e a ação vai ao servidor uma vez só

#### Scenario: Formulário em andamento
- **WHEN** a pessoa envia um formulário ou um diálogo, pelo botão ou pelo Enter, e a gravação demora
- **THEN** o botão de envio fica em andamento até a resposta, e um novo envio não manda a gravação de novo

#### Scenario: Fim do andamento com erro
- **WHEN** a resposta é um erro
- **THEN** o botão volta ao normal, com o ícone, e a tela mostra o erro como hoje

#### Scenario: Resposta rápida
- **WHEN** a resposta do servidor chega antes de meio segundo
- **THEN** o botão continua em andamento até completar meio segundo do clique, sem aceitar novo clique, e só então volta ao normal

#### Scenario: Formulário recusado sem ir ao servidor não fica em andamento
- **WHEN** a pessoa envia um formulário que as regras da própria tela recusam, sem ir ao servidor
- **THEN** o botão volta ao normal na hora, os erros aparecem e um novo clique funciona

#### Scenario: Envio de arquivos em andamento
- **WHEN** a pessoa envia arquivos na aba Documentos
- **THEN** "Enviar arquivos" fica em andamento até o último arquivo, dizendo quantos já foram, sem ficar cinza

#### Scenario: Planilha das horas em andamento
- **WHEN** o administrador escolhe "Baixar planilha" em Horas dos projetos
- **THEN** o botão fica em andamento enquanto a planilha é montada, sem ficar cinza
