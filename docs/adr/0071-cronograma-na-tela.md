---
status: aceita
date: 2026-10-17
decision-makers: Marcelo Ribeiro
---

# ADR-0071: Cronograma na tela, com Linha do tempo que se ajusta ao espaço, Expandir e faixa de fases nos projetos

Complementa o ADR-0021, que continua definindo os recursos do cronograma; este ADR decide como eles aparecem na tela.

## Contexto e problema

O ADR-0021 definiu os recursos do Gantt na v1 (barras arrastáveis, tabela ao lado, zoom, dependências, linha de base, caminho crítico, desfazer e exportação). Na revisão das telas, Marcelo apontou que a palavra "Gantt" não é conhecida de todo usuário, que o cronograma ficava pequeno, cortado e com espaço em branco embaixo e à esquerda, também no modo expandido e nas fases da Visão geral, e pediu uma forma de expandi-lo para trabalhar à vontade. Como o cronograma aparece na tela?

## Fatores de decisão

- Cronograma grande o suficiente para editar com conforto.
- Nenhum espaço em branco sobrando nem corte do período.
- Linguagem de quem usa, não da ferramenta.
- O andamento dos projetos visível também fora da aba Cronograma.

## Opções consideradas

- A. Largura fixa por dia, com rolagem lateral
- B. Linha do tempo que se ajusta ao espaço, com a opção de expandir, e uma faixa de fases nas listas de projetos

## Resultado da decisão

Opção escolhida: **B**, aprovada por Marcelo na revisão do design.

Regras:

| Item | Definição |
| --- | --- |
| Nomes | A aba se chama "Cronograma"; a visualização gráfica, "Linha do tempo", que alterna com "Lista". A palavra "Gantt" não aparece para o usuário |
| Ajuste ao espaço | O período do projeto ocupa toda a largura disponível e a grade (fins de semana, feriados e a linha de hoje) desce até o fim da área. Se o período não couber na escala escolhida (dias, semanas ou meses), a rolagem lateral fica só dentro da linha do tempo |
| Expandir | Abre um diálogo que cobre quase a tela inteira, com todas as funções e a tabela completa (início, fim, responsável e progresso) |
| Barra de ferramentas | Linha do tempo ou Lista; Dias, Semanas ou Meses; Hoje; "Exibir" (liga e desliga linha de base e caminho crítico); desfazer; Expandir; Nova tarefa; o menu "⋮" com refazer e exportar |
| Concluído | Parte concluída das tarefas em verde, inclusive no caminho crítico (ADR-0067) |
| Faixa de fases | Uma faixa com as fases, a parte concluída, os marcos e o dia de hoje aparece em Meu trabalho (ADR-0070), na lista de Projetos e na prévia do modelo em Novo projeto |
| Visão geral | Fases e marcos numa linha do tempo menor, que também se ajusta ao espaço |

### Consequências

- Bom, porque o cronograma usa toda a tela e não sobra espaço em branco.
- Bom, porque o andamento de cada projeto aparece nas listas sem abrir o cronograma.
- Ruim, porque com escala em dias e projetos longos os dias ficam estreitos; a escala em semanas ou meses resolve, e a rolagem lateral fica restrita à linha do tempo.
- Neutro, porque o ajuste ao espaço precisa ser confirmado no Gantt do Syncfusion (ADR-0051), que parece ter um zoom para caber o projeto; a documentação será conferida antes da implementação. Se não houver, o ajuste é feito calculando a largura do dia a partir do espaço disponível.

### Confirmação

Testes de interface em 1440×900 e 1920×1080 cobrindo: a linha do tempo ocupa a largura e a altura disponíveis, sem espaço em branco; o diálogo Expandir cobre quase a tela inteira com a tabela completa; "Exibir" liga e desliga linha de base e caminho crítico; a palavra "Gantt" não aparece na interface; a faixa de fases mostra fases, concluído, marcos e hoje nas listas de projetos.

## Prós e contras das opções

### A. Largura fixa por dia

- Bom, porque o tamanho do dia é sempre o mesmo.
- Ruim, porque corta o período, deixa espaço em branco e obriga a rolar para os lados.

### B. Ajuste ao espaço, Expandir e faixa de fases

- Bom, porque aproveita a tela e mostra o andamento em todo lugar.
- Ruim, porque exige cálculo da escala conforme o espaço e testes em vários tamanhos de tela.
