---
status: aceita
date: 2026-11-01
decision-makers: Marcelo Ribeiro
---

# ADR-0143: Edição do cronograma na Lista, num modo de edição pelo teclado

Completa o ADR-0071 (cronograma na tela), que deixa a edição de cada item no detalhe, e o ADR-0021 (recursos do Gantt).

## Contexto e problema

Para mudar o nome, as datas, a duração, as dependências ou o progresso de uma tarefa, a gerente abre o detalhe, muda o campo e salva, item por item. Quem monta um cronograma de 40 tarefas, ou acerta as durações depois de uma reunião, quer fazer isso como numa planilha: escrever na célula e seguir para a próxima pelo teclado. Marcelo pediu, no grupo 15 (GanttB-6), a edição direto na tabela. Onde ela fica, como convive com o clique que abre o detalhe e o que cada célula muda?

## Fatores de decisão

- Editar várias tarefas seguidas sem abrir e fechar o detalhe.
- Teclado como numa planilha: Enter, F2 ou digitar para editar, Tab para a próxima célula, Esc para desistir.
- Não editar sem querer: o clique na linha continua abrindo o detalhe fora da edição.
- As mesmas regras e as mesmas rotas da API: o servidor continua a fonte da verdade das datas (ADR-0051), e cada mudança é um passo do desfazer (ADR-0091).

## Opções consideradas

- A. Um modo "Editar na tabela" na Lista, ligado e desligado por um botão da barra.
- B. As células da Lista sempre editáveis, pelo duplo clique, com o clique simples abrindo o detalhe.
- C. A edição na tabela da Linha do tempo, no Expandir.

## Resultado da decisão

Opção escolhida: **A**. Um modo explícito separa o clique que abre o detalhe do clique que edita, e a Lista já é a tabela do cronograma, com todas as colunas (B mistura os dois cliques numa mesma linha; C pede uma grade editável nova, a tabela própria da Linha do tempo, ADR-0134).

Regras:

- **O modo.** "Editar na tabela" fica na barra da aba, na Lista, no computador, para quem edita o cronograma ou atualiza o progresso de alguma tarefa (ADR-0043); ligado, a linha não abre mais o detalhe e uma linha diz como editar pelo teclado; desligado, tudo volta ao que era.
- **O que se edita.** Quem edita o cronograma muda o nome de toda tarefa e marco; o início, o fim (só da tarefa), a duração (só da tarefa) e "Depende de" das que têm datas próprias (a tarefa-resumo tira os dela das subtarefas, ADR-0088); o progresso, quem edita e o responsável (`canUpdateProgress`), das tarefas com datas próprias. O responsável, a data-limite, a nota, a fase e o marco atingido continuam no detalhe, e as fases, no diálogo delas.
- **O que cada célula pede à API.** O nome, a duração e "Depende de" pelo pedido de editar o item, com os outros campos como estão (o tipo e o intervalo das dependências que ficam não mudam, ADR-0137); o início pelo pedido das datas, que mantém a duração e empurra as seguintes, como o arrastar da barra; o fim pelo pedido das datas com o início e o fim, como puxar a borda; o progresso pelo pedido do progresso (ADR-0086). Cada célula salva é um passo do desfazer e um aviso, como no detalhe.
- **Valor inválido.** A tela confere antes de mandar (o nome de 1 a 200 caracteres, o dia como dd/mm/aaaa, o fim no início ou depois, a duração de 1 a 999 dias úteis, o progresso de 0 a 100): a célula continua em edição, com o motivo. A recusa do servidor (a dependência circular, o projeto encerrado) volta a célula ao valor de antes, com o aviso.
- **Teclado.** As setas andam pelas células; Enter, F2 ou uma tecla de texto começam a edição; Enter salva e desce; Tab salva e vai para a direita; Esc desiste.

Fora:

| Item | Classificação |
| --- | --- |
| Colar várias células de uma planilha | depois |
| Incluir e excluir linhas pela tabela | fora: "Nova tarefa" e o detalhe continuam |
| Responsável, data-limite e nota na tabela | depois |
| Edição na tabela da Linha do tempo e no celular | fora |

### Consequências

- Bom, porque a gerente acerta várias tarefas seguidas pelo teclado, sem abrir o detalhe.
- Bom, porque nenhuma regra nova: as mesmas rotas, as mesmas conferências e o mesmo desfazer.
- Ruim, porque é um modo a mais para aprender, e o clique na linha muda de efeito enquanto ele está ligado.
- Ruim, porque cada célula é um pedido: acertar 20 durações são 20 passos do desfazer.

### Confirmação

Testes de tela cobrindo: o modo ligado e desligado e quem o vê; o nome, a duração, o início, o fim, "Depende de" e o progresso salvos pela célula, com o pedido certo; o valor inválido que deixa a célula em edição; a recusa do servidor que volta o valor; e as células que não se editam (a tarefa-resumo, o marco, as fases e o progresso de quem não é o responsável).

## Prós e contras das opções

### A. Modo "Editar na tabela" na Lista

- Bom, porque separa ver e editar, e usa a tabela que já existe.
- Ruim, porque pede ligar o modo.

### B. Células sempre editáveis

- Bom, porque não há modo.
- Ruim, porque o clique simples e o duplo clique fazem coisas diferentes na mesma linha, e é fácil editar sem querer.

### C. Na tabela da Linha do tempo

- Bom, porque fica ao lado das barras.
- Ruim, porque a tabela da Linha do tempo é própria (ADR-0134) e não tem edição de célula; seria uma grade nova.
