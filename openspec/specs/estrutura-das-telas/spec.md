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

### Requirement: Tema automático

Enquanto a pessoa não escolhe um tema, a aplicação MUST seguir o tema do sistema operacional (ADR-0014).

#### Scenario: Tema segue o sistema
- **WHEN** o sistema operacional está no tema escuro, ou no claro
- **THEN** a aplicação abre no mesmo tema

### Requirement: Barra lateral no computador

Em telas de 900px ou mais, a aplicação MUST mostrar uma barra lateral fixa, da altura da janela, com o selo e o nome do produto no topo e os itens Início, Projetos e Chamados, destacando o item da página atual.

#### Scenario: Navegação principal
- **WHEN** a aplicação é aberta numa tela de 900px ou mais
- **THEN** a barra lateral mostra o nome do produto e os itens Início, Projetos e Chamados

#### Scenario: Item da página atual
- **WHEN** a pessoa está numa página da navegação principal
- **THEN** o item dessa página fica marcado como o atual, e só ele

### Requirement: Cabeçalho com a trilha

Sobre o conteúdo, a aplicação MUST mostrar um cabeçalho da mesma cor do conteúdo e sem linha, com a trilha da página, cujo último nível é a página atual.

#### Scenario: Trilha da página
- **WHEN** a pessoa abre uma página
- **THEN** o cabeçalho mostra a trilha com o nome da página como último nível, marcado como a página atual

### Requirement: Estrutura no celular

Em telas menores que 900px, a aplicação MUST trocar a barra lateral por uma barra superior com o botão de menu e o título da página; o menu MUST abrir por cima do conteúdo com a mesma navegação e fechar ao escolher um item.

#### Scenario: Menu no celular
- **WHEN** a aplicação é aberta numa tela menor que 900px e a pessoa toca no botão de menu
- **THEN** o menu abre com os itens Início, Projetos e Chamados, e escolher um item leva à página e fecha o menu

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
