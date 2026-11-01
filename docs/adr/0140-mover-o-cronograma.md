---
status: aceita
date: 2026-11-01
decision-makers: Marcelo Ribeiro
---

# ADR-0140: Mover o cronograma em dias úteis

Completa o ADR-0019 (dependências que empurram, sem puxar) e o ADR-0126 (recálculo pelo calendário).

## Contexto e problema

O início de um projeto fiscal escorrega com frequência: o contrato atrasa, o cliente adia o kickoff, o ambiente não fica pronto. Hoje a gerente muda o início da primeira tarefa e as dependências empurram as seguintes, mas só as que dependem dela; as tarefas soltas, os marcos sem dependência e as outras fases ficam onde estavam, e ela as move uma a uma. Quando o projeto adianta, nada puxa as tarefas (ADR-0019). Marcelo pediu, no grupo 15 (GanttB-3), mover o cronograma inteiro em dias úteis. O que anda, em quantos dias e quem decide?

## Fatores de decisão

- Um passo só para o caso comum: o projeto inteiro anda N dias úteis.
- O que já aconteceu não é reescrito (ADR-0126).
- A linha de base e as datas-limite são compromissos de fora: não andam com o cronograma (ADR-0021, GanttA-2).
- Desfazer o passo como qualquer mudança do cronograma (ADR-0091).

## Opções consideradas

O que anda:

- A1. As tarefas e os marcos em aberto (tarefa abaixo de 100%, marco não atingido); os concluídos ficam.
- A2. Todos os itens, também os concluídos.

Como a gestão diz o movimento:

- B1. Em dias úteis, para depois ou para antes.
- B2. Pela nova data de início do projeto.

## Resultado da decisão

Opções escolhidas: **A1** e **B1**. A1 segue a regra do recálculo (ADR-0126): o que foi feito não muda de data. B1 porque, com parte do projeto concluída, "o novo início" é ambíguo, e "5 dias úteis para depois" diz exatamente o que acontece com o que falta.

Regras:

- **Quem.** Quem edita o cronograma (o administrador e o gerente), no projeto em aberto (ADR-0043).
- **Movimento.** De 1 a 999 dias úteis do calendário da instalação, para depois ou para antes (ADR-0018). Cada tarefa e marco com datas próprias em aberto anda esse número de dias úteis, com a mesma duração em dias úteis; depois, começa depois das anteriores pelas dependências, com o tipo e o intervalo (ADR-0137), o que pode segurar, num movimento para antes, um item que depende de um concluído. As tarefas-resumo seguem as subtarefas (ADR-0088).
- **O que não anda.** As tarefas a 100%, os marcos atingidos, as datas-limite e a linha de base, que passa a mostrar a diferença.
- **Rastros.** O movimento é uma mudança só no desfazer de quem o fez (ADR-0091), vai para a auditoria (ADR-0039) e avisa os responsáveis das tarefas que mudam de datas (ADR-0037).

### Consequências

- Bom, porque um atraso no início vira um passo, com o desfazer.
- Bom, porque a linha de base mostra quanto o projeto escorregou.
- Ruim, porque um movimento grande avisa todos os responsáveis de uma vez.
- Neutro, porque a gestão que quer mover só uma fase continua movendo a primeira tarefa dela.

### Confirmação

Testes do domínio: o movimento para depois e para antes em dias úteis, com feriado no caminho; os concluídos parados; o item que depende de um concluído segurado num movimento para antes; as tarefas-resumo; a data-limite e a linha de base paradas. Testes da API: só quem edita move, no projeto em aberto; o número fora de 1 a 999 recusado; o desfazer e a auditoria. Teste de tela: o movimento pelo "⋮" com a confirmação.

## Prós e contras das opções

### A1. Só os itens em aberto

- Bom, porque não reescreve o que aconteceu.
- Ruim, porque um item em aberto que dependia de um concluído pode não andar para antes.

### A2. Todos os itens

- Bom, porque o desenho do cronograma anda inteiro.
- Ruim, porque muda a data em que uma tarefa foi feita.

### B1. Em dias úteis

- Bom, porque diz o que acontece com o que falta, também com parte concluída.
- Ruim, porque a gestão conta os dias úteis até a nova data.

### B2. Pela nova data de início

- Bom, porque é como a gestão fala do atraso.
- Ruim, porque, com o início já concluído, não diz de quanto o resto anda.
