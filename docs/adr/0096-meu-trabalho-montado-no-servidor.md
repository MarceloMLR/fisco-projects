---
status: aceita
date: 2026-10-20
decision-makers: Marcelo Ribeiro
---

# ADR-0096: Meu trabalho montado no servidor por um módulo próprio, com a janela de 7 dias e o atraso do item mais atrasado

Detalha o ADR-0070 nos pontos que ele não decide.

## Contexto e problema

O ADR-0070 decidiu o Meu trabalho como uma fila única dos chamados, das tarefas e dos termos de aceite da pessoa, agrupada em Atrasado, Aguardando você, Hoje e o próximo dia útil, e Próximos 7 dias, com as visões "Para mim", "Abertos por mim" e "Que acompanho", uma ação por linha, os projetos da pessoa com a faixa de fases e o atraso em dias úteis, e os próximos marcos. O canvas Main (Meu trabalho) mostra a fila, os projetos e os marcos.

Ficam quatro perguntas:

- Onde a fila é montada? Os itens vêm de três módulos (`Tickets`, `Projects` e `Documents`), e os grupos dependem dos dias úteis do calendário da instalação (ADR-0018, ADR-0025).
- O que acontece com um item cujo prazo passa dos 7 dias?
- "Próximos 7 dias" conta dias corridos ou dias úteis?
- O que é o atraso de um projeto em dias úteis?

## Fatores de decisão

- Uma regra de urgência num lugar só, testada com fins de semana e feriados (ADR-0070).
- O servidor é a fonte da verdade das datas e dos dias úteis (ADR-0051).
- Os módulos só se conhecem pelos contratos (ADR-0047).
- A tela responde "o que depende de mim agora", sem repetir as listas completas de cada módulo.

## Opções consideradas

Onde a fila é montada:

- A1. A tela pede as listas de cada módulo e agrupa no navegador
- A2. O módulo `Projects` monta a fila, com os chamados e os termos por contratos que o `Tickets` e o `Documents` implementam
- A3. Um módulo `MyWork`, sem tabelas, monta a fila; cada módulo entrega os itens da pessoa por um contrato do `MyWork.Contracts` (`IMyWorkSource`)

Prazo depois dos 7 dias:

- B1. O item fica fora do Meu trabalho e continua nas telas do módulo (Chamados, Cronograma, Termos de aceite)
- B2. Um quinto grupo, "Mais adiante"

Próximos 7 dias:

- C1. Até 7 dias corridos depois de hoje
- C2. Até 7 dias úteis depois de hoje

Atraso do projeto:

- D1. Os dias úteis de atraso da tarefa ou do marco mais atrasado, como a tarefa-resumo (ADR-0088)
- D2. O desvio do fim do projeto contra a linha de base (ADR-0021)

## Resultado da decisão

Opções escolhidas:

- **A3.** A regra de urgência fica num lugar só, no servidor, com o calendário da instalação; cada módulo continua dono das regras dos seus itens (quem vê, quem atende, o prazo), e o `MyWork` não lê tabelas de ninguém.
- **B1.** A fila mostra o que pede ação nos próximos dias; o grupo novo mudaria a tela aprovada e repetiria as listas completas, que já existem.
- **C1.** O nome do grupo diz "dias", e a tela diz "dias úteis" sempre que conta dias úteis; os fins de semana e os feriados já contam no grupo "Hoje e o próximo dia útil".
- **D1.** Todo projeto com cronograma tem esse número, com ou sem linha de base, e ele é o mesmo atraso que o cronograma mostra em cada item. O desvio contra a linha de base fica na visão geral do projeto (ADR-0073).

Regras:

| Item | Definição |
| --- | --- |
| Para mim | Os chamados de que a pessoa é responsável, enquanto não aguardam o solicitante; os chamados que ela abriu e aguardam a resposta dela; os chamados sem responsável dos times dela (o gerente e o líder técnico, os dois times; o colaborador, Funcional; o desenvolvedor, Desenvolvimento, ADR-0068); as tarefas dela; os pedidos de aceite pendentes para ela; e os pedidos pendentes que ela fez, enquanto ela gerencia os termos do projeto |
| Abertos por mim | Os chamados que a pessoa abriu e os pedidos de aceite pendentes que ela fez |
| Que acompanho | Os chamados que a pessoa acompanha (ADR-0026) |
| Prazo de cada item | Chamado: a primeira resposta, enquanto não foi dada, e depois a resolução, sem prazo enquanto aguarda o solicitante (ADR-0025). Tarefa: o fim. Pedido que a pessoa fez: o dia do marco |
| Atrasado | Prazo antes de hoje, no fuso da instalação |
| Aguardando você | O chamado aberto pela pessoa que aguarda a resposta dela e o pedido de aceite pendente para ela, qualquer que seja o prazo |
| Hoje e o próximo dia útil | Prazo de hoje até o próximo dia útil do calendário da instalação |
| Próximos 7 dias | Prazo depois do próximo dia útil, até 7 dias corridos depois de hoje |
| Fora da fila | Prazo depois dos 7 dias, chamado resolvido ou fechado, chamado que aguarda outra pessoa, tarefa concluída ou com subtarefas (as subtarefas entram), projeto concluído ou cancelado e projeto que a pessoa não vê |
| Ação | Responder: o chamado e o pedido que aguardam a pessoa. Atribuir: o chamado sem responsável, só para quem troca o responsável (o gerente, o líder técnico e o administrador, ADR-0068). Abrir: os outros chamados. Iniciar: a tarefa em 0%. Concluir: a tarefa atrasada, de hoje ou do próximo dia útil. Atualizar: as outras tarefas. Lembrar: o pedido que a pessoa fez |
| Ordem | Pelo prazo, o mais antigo primeiro; em Aguardando você, pela espera mais antiga |
| Meus projetos | Os projetos abertos de que a pessoa participa, como na barra lateral, com a faixa de fases, a porcentagem concluída e o atraso do item mais atrasado |
| Próximos marcos | Os 10 primeiros marcos não atingidos dos mesmos projetos, de hoje em diante, com o dia, os dias úteis que faltam e "aceite pedido" quando há pedido pendente para o marco |

### Consequências

- Bom, porque a urgência tem uma regra só, testada no servidor com o calendário da instalação.
- Bom, porque cada módulo continua decidindo quem vê e quem atende os seus itens, e o `MyWork` só junta e agrupa.
- Ruim, porque é um módulo a mais, sem tabelas, e cada módulo de itens implementa mais um contrato.
- Ruim, porque um chamado de prioridade baixa, com 10 dias úteis para resolver, só entra na fila quando faltam 7 dias; até lá, fica na página Chamados.

### Confirmação

Testes cobrindo: cada item no grupo certo, inclusive na sexta-feira, no fim de semana e na véspera de feriado; um item com prazo depois dos 7 dias fica fora; "Atribuir" só para quem troca o responsável; só itens de projetos que a pessoa vê; o atraso do projeto pelo item mais atrasado.

## Prós e contras das opções

### A1. Agrupar no navegador

- Bom, porque não precisa de rota nova.
- Ruim, porque o navegador não conhece os dias úteis, e a regra de urgência ficaria fora do servidor.

### A2. O Projects monta a fila

- Bom, porque não cria módulo.
- Ruim, porque o módulo de projetos passaria a conhecer a fila de chamados e termos da pessoa.

### A3. Módulo MyWork

- Bom, porque separa a síntese da pessoa dos módulos donos dos itens.
- Ruim, porque é um módulo a mais.

### B1. Fora da fila

- Bom, porque a fila fica curta e mostra o que é urgente.
- Ruim, porque o item aparece só quando o prazo chega perto.

### B2. Grupo "Mais adiante"

- Bom, porque mostra tudo.
- Ruim, porque muda a tela aprovada e repete as listas de cada módulo.

### C1. Dias corridos

- Bom, porque é o que o nome do grupo diz.
- Ruim, porque, numa semana com feriado, o grupo tem menos dias úteis.

### C2. Dias úteis

- Bom, porque acompanha os prazos.
- Ruim, porque o grupo diria "7 dias" contando mais de uma semana.

### D1. Item mais atrasado

- Bom, porque vale para todo cronograma e repete o número de cada item.
- Ruim, porque um atraso pequeno numa tarefa sem folga pesa igual ao de uma tarefa no caminho crítico.

### D2. Contra a linha de base

- Bom, porque mede o projeto inteiro.
- Ruim, porque só existe depois de salvar a linha de base.
