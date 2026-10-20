---
status: proposta
date: 2026-10-19
decision-makers: Marcelo Ribeiro
---

# ADR-0091: Desfazer e refazer no cronograma pelo servidor, com o histórico de cada pessoa

Detalha o "desfazer" do ADR-0021 e o "refazer" do menu "⋮" do ADR-0071.

## Contexto e problema

O ADR-0021 pôs na v1 desfazer as últimas ações da própria pessoa no cronograma, incluindo as tarefas empurradas em cadeia, e bloqueou o desfazer quando alguém alterou essas tarefas depois. O ADR-0071 pôs o botão "Desfazer" na barra da aba e o "refazer" no menu "⋮". As datas são calculadas no servidor (ADR-0051), e mais de uma pessoa pode editar o mesmo cronograma. Onde fica o histórico e como desfazer volta o cronograma?

## Fatores de decisão

- O servidor é a fonte da verdade das datas (ADR-0051): desfazer não pode recalcular nada no navegador.
- Desfazer volta a ação e tudo o que ela empurrou, numa vez só.
- Nunca apagar o trabalho de outra pessoa: se alguém mudou os mesmos itens depois, o desfazer é recusado (ADR-0021).
- O histórico continua depois de recarregar a página ou trocar de computador.

## Opções consideradas

- A. Histórico no navegador: desfazer manda à API as ações inversas, uma a uma.
- B. Histórico no servidor, com a ação inversa de cada caso de uso.
- C. Histórico no servidor, com as fotos dos registros que a ação mudou, antes e depois.

## Resultado da decisão

Opção escolhida: **C**.

- Cada mudança do cronograma feita por quem edita (administrador e gerente, ADR-0043) guarda, numa tabela do módulo `Projects`, os registros de fases, tarefas, marcos e dependências que ela mudou, como eram antes e como ficaram, com o nome da ação ("a inclusão da tarefa X"). As tarefas empurradas e as tarefas-resumo recalculadas entram na mesma mudança.
- Desfazer volta a última mudança da pessoa no projeto que ainda não foi desfeita. Antes de voltar, compara cada registro com o "depois" guardado: se algum está diferente, alguém mudou esses itens depois, e o desfazer é recusado (409). A mudança sai do histórico, e o próximo desfazer vai para a anterior.
- Refazer, no "⋮", aplica de novo a última mudança desfeita, com a mesma comparação contra o "antes". Uma mudança nova da pessoa apaga o que ela podia refazer.
- Cada pessoa guarda as últimas 20 mudanças por projeto; as mais antigas saem.
- O progresso atualizado pelo responsável que não edita o cronograma e a linha de base não entram no histórico.
- Desfazer e refazer passam pela auditoria como qualquer mudança (ADR-0039): o registro mostra os itens que voltaram.

### Consequências

- Bom, porque desfazer volta exatamente o que a ação mudou, inclusive as datas empurradas, sem recalcular nada.
- Bom, porque a comparação com o "depois" protege o trabalho de outra pessoa sem precisar de controle de versão em cada item.
- Bom, porque o histórico vale em qualquer computador e depois de recarregar a página.
- Ruim, porque cada mudança grava as fotos dos registros num JSON, uma tabela a mais que cresce até 20 linhas por pessoa e projeto.
- Neutro, porque uma ação de outra pessoa que só toca outros itens não bloqueia o desfazer.

### Confirmação

Testes cobrindo:
- desfazer volta a tarefa e as empurradas;
- refazer reaplica;
- uma mudança nova apaga o refazer;
- o bloqueio quando outra pessoa mudou os itens;
- o limite de 20;
- a matriz de permissões do cronograma com desfazer e refazer.

## Prós e contras das opções

### A. Histórico no navegador

- Bom, porque não precisa de tabela.
- Ruim, porque se perde ao recarregar e porque a ação inversa não volta as datas que o servidor empurrou: mover uma tarefa de volta não "desempurra" as seguintes.

### B. Ação inversa de cada caso de uso

- Bom, porque guarda pouco.
- Ruim, porque cada caso de uso precisa da sua inversa, e as inversas de empurrar e de excluir com dependências são tão complexas quanto o próprio caso de uso.

### C. Fotos dos registros, antes e depois

- Bom, porque é um mecanismo só para todas as ações, e volta o estado exato.
- Ruim, porque grava mais dados por mudança.
