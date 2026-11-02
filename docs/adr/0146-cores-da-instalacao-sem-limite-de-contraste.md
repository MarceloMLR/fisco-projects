---
status: proposta
date: 2026-11-01
decision-makers: Marcelo Ribeiro
---

# ADR-0146: Cores da instalação numa seção própria, com a principal, a secundária e a da barra lateral, sem limite de contraste

Substitui o ADR-0103 inteiro, a recusa da cor sem contraste do ADR-0082 e o seletor de cores do navegador do ADR-0100. Mantém do ADR-0082 a derivação em OKLCH, num lugar só do frontend, e as cores de status fora da troca. Mantém do ADR-0100 o logo para o tema escuro, e do ADR-0108 o selo da aba na cor principal.

## Contexto e problema

Hoje a instalação troca só a cor principal (ADR-0103), e só uma cor com 4,5:1 de contraste com o branco (ADR-0082). Cores claras, como um amarelo, são recusadas, e o seletor do navegador muda de um sistema operacional para outro. Marcelo pediu que o cliente personalize as cores pela marca da empresa, sem esse limite, num seletor melhor, que aceite a cor em vários formatos. Ele comparou as opções numa página de prévia e aprovou o protótipo em 01/11/2026.

Quais cores a instalação escolhe, o que acontece com uma cor sem contraste, como ela é escolhida e onde fica na Administração?

## Fatores de decisão

- A empresa usa as cores do manual da marca como são, inclusive as claras.
- Todo texto continua legível, com o contraste do WCAG AA, nos dois temas.
- Sem nenhuma cor escolhida, as telas continuam exatamente as do design system.
- As cores de status nunca mudam (ADR-0065).
- Nenhuma dependência nova para o seletor nem para as contas de cor.

## Opções consideradas

Quais cores (escolha de Marcelo):

- A. Principal, secundária e barra lateral
- B. Só a principal, como hoje

Cor sem contraste com o branco (escolha de Marcelo):

- C. Aceitar qualquer cor e ajustar o que fica sobre ela
- D. Recusar, como hoje
- E. Aceitar com um aviso, sem ajuste

Seletor (escolha de Marcelo):

- F. Seletor próprio, com a área de cor, a barra de matiz e o valor em HEX, RGB ou HSL
- G. O seletor do navegador, como hoje

Onde fica (escolha de Marcelo, depois do protótipo):

- H. Uma seção própria, Cores, na Administração
- I. Dentro de Configurações gerais, como hoje

## Resultado da decisão

Opções escolhidas: **A**, **C**, **F** e **H**, todas por escolha de Marcelo.

### As três cores

As três são opcionais, em `#RRGGBB`. Sem uma delas, vale o que o design system define para o que ela pinta.

- **Principal:** o botão principal, os links, as abas, os controles marcados, o anel de foco, o avatar do responsável, o selo sem logo (da barra lateral e da aba) e o item ativo da barra lateral.
- **Secundária:** a linha selecionada, a notificação não lida e a menção (`bg-selected`), e o painel da página de entrada. Sem ela, `bg-selected` fica a do design system e o painel segue a principal, como hoje.
- **Barra lateral:** o fundo da barra lateral. Os textos, o item sob o ponteiro, as divisórias, o item ativo e o nome da instalação acompanham o fundo. Sem ela, a barra lateral é a do design system, e só o item ativo segue a principal, como hoje. A regra do design system "nunca cor de marca" na barra lateral vale só sem essa cor.

### Qualquer cor aceita, com o que fica sobre ela ajustado

O servidor confere só o formato. A derivação continua num lugar só do frontend (`brandColors.ts`), em OKLCH, e os ajustes nunca recusam a cor: aparecem embaixo do campo como aviso informativo, com ícone, cor e texto.

- **Texto sobre a cor:** o texto do botão principal e o do controle marcado ficam brancos quando o branco tem 4,5:1 sobre a cor. Sem isso, ficam no texto escuro do design system (o que tiver mais contraste).
- **A cor exata:** no tema claro, o botão principal é a própria cor escolhida, e o fundo da barra lateral também.
- **Ajustes só onde a cor é texto ou contorno:**
  - os links e as abas ficam mais escuros que a cor até ter 4,5:1 sobre as superfícies;
  - o controle marcado e o anel de foco, até 3:1;
  - o filete do item ativo, até 3:1 sobre a barra lateral;
  - o avatar do responsável e o fundo do painel da entrada, até o texto branco ter 4,5:1;
  - os textos da barra lateral, até 4,5:1 sobre o fundo dela.
- **Tema escuro:**
  - o botão principal é a cor escolhida, mais clara só até ter 3:1 com o fundo da página, como a cor padrão;
  - a barra lateral é um tom escuro do matiz escolhido, porque uma barra clara não cabe no tema escuro;
  - a linha selecionada e o painel mantêm a claridade do design system, com o matiz da secundária.
- **Logo escuro:** a barra lateral é escura quando o fundo efetivo dela é escuro, no tema escuro ou com uma cor da barra lateral escura no tema claro, e então mostra o logo para o tema escuro (ADR-0100).
- **Fora da troca:** as cores de status, as superfícies, os textos neutros e o fundo atrás do painel da entrada não mudam.

### Seletor próprio

Ao clicar na amostra de uma cor, abre um seletor do FiscoProjects (um popover no computador e uma folha que sobe no celular) com:

- a área de saturação e brilho e a barra de matiz, as duas pelo ponteiro e pelas setas do teclado;
- a cor atual ao lado da nova;
- o formato do valor (HEX, RGB ou HSL);
- o campo do valor;
- "Cancelar" e "Usar esta cor". Esc também cancela.

O campo do valor, e também o campo ao lado da amostra, aceitam a cor digitada ou colada em `#0F766E`, `0f766e`, `#0f7`, `rgb(15, 118, 110)`, `15, 118, 110` ou `hsl(175, 77%, 26%)`. A cor é guardada sempre em `#RRGGBB`. A prévia muda enquanto a cor é escolhida.

### Seção Cores

A seção Instalação da Administração ganha **Cores** (`/admin/colors`), logo depois de Configurações gerais, com:

- os três campos;
- uma prévia grande das telas, com Claro e Escuro;
- "Descartar" e "Salvar alterações", habilitados só quando algo mudou.

A seção grava pela própria rota, só do administrador, e as três cores entram na auditoria pelo hexadecimal. A marca lida sem sessão (`GET /api/installation`) passa a trazer as três cores. Configurações gerais continua com o nome e os logos na seção Marca, com a prévia pequena, e perde só o campo da cor.

### Consequências

- Bom, porque qualquer cor da marca da empresa é aceita como é, e o texto continua legível.
- Bom, porque o seletor é o mesmo em qualquer navegador e aceita a cor copiada do manual da marca em qualquer formato.
- Bom, porque sem cor nenhuma as telas continuam as do design system.
- Ruim, porque três cores abrem mais combinações para conferir, o motivo do ADR-0103. A prévia grande, nos dois temas, mostra cada combinação antes de salvar.
- Ruim, porque uma cor clara deixa o link um pouco diferente do botão (o link fica mais escuro). O aviso embaixo do campo diz isso.
- Neutro, porque o seletor e a leitura dos formatos passam a ser código do projeto, com testes, sem biblioteca.

### Confirmação

Testes cobrindo:

- a cor padrão, sem nenhuma das três, devolve os valores do `tokens.json`;
- para cores claras, escuras, branco e preto, nos dois temas:
  - o texto do botão e o do controle marcado têm 4,5:1;
  - os links têm 4,5:1 sobre as superfícies;
  - os textos da barra lateral têm 4,5:1 sobre o fundo dela;
  - o texto do painel da entrada tem 4,5:1;
- a cor clara salva pela API, e a cor fora do formato recusada com 400 no campo;
- a leitura de cada formato aceito, e a recusa do que não é cor;
- o seletor pelo ponteiro e pelo teclado, "Cancelar" e Esc sem mudar nada, e "Usar esta cor" preenchendo o campo;
- a barra lateral escura no tema claro mostrando o logo escuro;
- as cores de status iguais;
- as três cores na auditoria e só o administrador mudando.

## Prós e contras das opções

### A. Principal, secundária e barra lateral

- Bom, porque a marca aparece onde a pessoa olha sempre, separando a cor dos botões da cor da barra.
- Ruim, porque contraria o design system na barra lateral quando ligada.

### B. Só a principal

- Bom, porque é uma cor só para conferir.
- Ruim, porque muitas marcas têm duas cores, e a barra lateral não pode ser escura no tema claro.

### C. Aceitar e ajustar

- Bom, porque aceita qualquer cor e mantém o contraste AA pelo cálculo.
- Ruim, porque o link de uma cor clara fica mais escuro que o botão.

### D. Recusar

- Bom, porque a cor escolhida aparece igual em todo lugar.
- Ruim, porque recusa as marcas amarelas, verde-limão e claras em geral.

### E. Aceitar com aviso, sem ajuste

- Bom, porque a cor aparece igual em todo lugar.
- Ruim, porque deixaria texto ilegível nas telas.

### F. Seletor próprio

- Bom, porque é igual em qualquer navegador, aceita vários formatos e segue o design system.
- Ruim, porque o projeto passa a manter um componente com teclado e acessibilidade próprios.

### G. Seletor do navegador

- Bom, porque não pede código.
- Ruim, porque muda de um sistema operacional para outro e, em alguns, nem mostra o hexadecimal.

### H. Seção própria

- Bom, porque a prévia grande cabe ao lado das cores e não disputa espaço com as regras gerais.
- Ruim, porque a marca fica em dois lugares: o nome e os logos em Configurações gerais, e as cores em Cores.

### I. Dentro de Configurações gerais

- Bom, porque a marca fica num lugar só.
- Ruim, porque três cores, os avisos e a prévia grande alongam demais uma tela que já tem as regras gerais, os arquivos e o Microsoft 365.
