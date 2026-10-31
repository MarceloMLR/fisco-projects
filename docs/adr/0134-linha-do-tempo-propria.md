---
status: proposta
date: 2026-10-31
decision-makers: Marcelo Ribeiro
---

# ADR-0134: Linha do tempo própria no lugar do Syncfusion

Faz a evolução prevista no ADR-0051: o Gantt do Syncfusion sai e a Linha do tempo passa a ser desenhada por um componente do próprio FiscoProjects. O ADR-0087 (a chave do Syncfusion chegando ao navegador) deixa de valer quando a chave sair da instalação, na Gantt-2. Os recursos do cronograma continuam os do ADR-0021, e a tela, a do ADR-0071.

## Contexto e problema

O ADR-0051 escolheu o Syncfusion pela licença comunitária, sem custo para Marcelo, isolado no componente `ScheduleGantt`, com o Gantt próprio "depois da v1 ou antes de vender o código a quem não se enquadre na licença comunitária". Com a v1 concluída, em 31/10/2026, Marcelo pediu o Gantt próprio, para tirar a dependência de licença antes de vender e para ir além dele depois (importar do MS Project).

O que já existe ajuda:

- O servidor calcula tudo: as datas em dias úteis, as dependências que empurram, a linha de base, o caminho crítico, os dias não úteis e o desfazer (ADR-0051, ADR-0088, ADR-0091). O componente só desenha o que a API devolve e transforma arrastar, puxar a borda e ligar em eventos (`onMove`, `onResize`, `onLink`).
- O design system descreve o desenho inteiro da Linha do tempo (componente Gantt), com os tokens `gantt-*` nos dois temas.
- A página de impressão e a linha do tempo da Visão geral já desenham as fases e as barras em SVG, sem o Syncfusion, com as contas da escala em `schedulePrint.ts`.

E o Syncfusion custa mais do que a licença:

- cada comprador acima do limite da licença comunitária precisa da própria licença, e a chave passa pelo Key Vault, pelo Terraform, pelo Codespaces e pela API (ADR-0087);
- o visual vem de mais de 300 linhas de CSS por cima do tema Material 3 dele, que ainda importa uma fonte do Google, tirada no build (ADR-0064);
- a telemetria dele precisa ser desligada (ADR-0058);
- ele procura partes de si na página inteira, então só uma Linha do tempo pode estar montada por vez (o Expandir espera a da aba sair), e os testes de escolher linhas e do caminho crítico precisam de arquivos próprios;
- as telas são testadas com ele simulado, e o real só com a largura simulada.

## Fatores de decisão

- Nenhuma licença comercial ou copyleft forte para quem compra (ADR-0002, ADR-0061).
- O mesmo comportamento das specs do cronograma e o visual do design system, nos dois temas, no computador e no celular.
- Acessível pelo teclado e pelo leitor de tela.
- Testável no jsdom como as outras telas.

## Opções consideradas

- A. Manter o Syncfusion, com a licença de cada comprador.
- B. SVAR React Gantt, na edição aberta (MIT).
- C. Frappe Gantt (MIT).
- D. DHTMLX Gantt ou Bryntum Gantt.
- E. Componente próprio, sobre o design system.

## Resultado da decisão

Opção escolhida: **E. Componente próprio**, porque o servidor já faz a parte difícil, o desenho já está no design system e as interações são três (mover, puxar a borda e ligar), contidas num componente.

- O `ScheduleGantt` mantém as propriedades e os eventos de hoje, menos a licença; a aba Cronograma, o Expandir e os detalhes não mudam.
- A tabela à esquerda é uma árvore acessível (`treegrid`), com as fases e as tarefas-resumo que se recolhem, e as colunas de hoje (nome e progresso; no Expandir, também início, fim e responsável; no celular, só o nome).
- O gráfico à direita desenha as linhas, os dias não úteis, as barras, os marcos, a linha de base, o progresso e a seleção em elementos HTML posicionados pelos tokens, e as setas das dependências e a linha de hoje numa camada SVG. A tabela e o gráfico rolam juntos na vertical, e só o gráfico rola para o lado.
- Mover, puxar a borda e ligar usam os eventos de ponteiro, em dias inteiros, com a barra flutuando (`shadow-drag`). Nada muda de verdade até a API responder: a barra volta para o lugar se a API recusar.
- A escala, o período e o ajuste ao espaço continuam em `ganttLayout.ts`, e as linhas em `ganttRows.ts`, sem as conversões de datas que existiam só para o Syncfusion.
- Saem os pacotes `@syncfusion/*`, o CSS do tema, a paleta, a remoção da fonte do Google no build e a regra de uma Linha do tempo por vez. A regra do lint passa a recusar qualquer `@syncfusion/*`.

### Consequências

- Bom, porque ninguém compra licença para usar a Linha do tempo, e a chave sai da instalação.
- Bom, porque o visual sai direto dos tokens, sem sobrescrever o tema de outra biblioteca, e os testes passam a usar o componente real.
- Bom, porque abre caminho para o que o Syncfusion não daria sem a licença: importar do MS Project e o que vier depois.
- Ruim, porque passamos a manter o arrastar, a rolagem e o desempenho com muitas linhas.
- Ruim, porque o que uma biblioteca traria pronto (outros tipos de dependência, por exemplo) passa a ser trabalho nosso, sempre com ADR.

### Confirmação

- Os cenários da Linha do tempo nas specs do cronograma passam com o componente próprio, inclusive pelo teclado.
- Nenhum `@syncfusion/*` no `package.json`, verificado por teste.
- O desenho conferido no navegador contra o componente Gantt do design system, nos dois temas, no computador e no celular.

## Prós e contras das opções

### A. Manter o Syncfusion

- Bom, porque já funciona.
- Ruim, porque cada comprador acima do limite da licença comunitária paga a licença dele, e todos os custos do contexto continuam.

### B. SVAR React Gantt (edição aberta)

- Bom, porque é MIT, em React, com tabela e arrastar.
- Ruim, porque o caminho crítico, a linha de base, os calendários de trabalho, o desfazer e a importação do MS Project estão só na edição PRO, paga (a partir de US$ 749 por desenvolvedor).

### C. Frappe Gantt

- Bom, porque é MIT e leve.
- Ruim, porque não tem a tabela ao lado, a linha de base nem o caminho crítico, e o visual também teria de ser sobrescrito.

### D. DHTMLX Gantt ou Bryntum Gantt

- Bom, porque são completos.
- Ruim, porque o DHTMLX padrão é GPL (copyleft forte) e o PRO e o Bryntum são pagos, o que volta ao problema da licença.
