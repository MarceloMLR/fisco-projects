---
status: aceita
date: 2026-11-01
decision-makers: Marcelo Ribeiro
---

# ADR-0145: Fim digitado no detalhe da tarefa, calculado pelo servidor

Complementa o ADR-0051 (o servidor é a fonte da verdade das datas) e o ADR-0071 (o cronograma na tela): o detalhe da tarefa passa a aceitar o fim, além da duração.

## Contexto e problema

No detalhe da tarefa e em "Nova tarefa", a pessoa digita o início e a duração em dias úteis, e o fim aparece só como "Termina em 13/10/2026" embaixo da duração, depois de salvar. Quem planeja costuma saber o fim ("até o dia 30"), não quantos dias úteis cabem até lá, e por isso Marcelo perguntou por que a duração é obrigatória. Na Lista, o modo "Editar na tabela" (ADR-0143) já aceita o fim, pelo `PUT .../dates`. Marcelo pediu, em 01/11/2026, o campo "Fim" ao lado da duração, com um calculando o outro. Quem calcula os dias úteis enquanto a pessoa digita, e o que vai ao salvar?

## Fatores de decisão

- Os dias úteis, os feriados e os dias de trabalho da instalação ficam num lugar só, no servidor (ADR-0018, ADR-0051).
- O outro campo aparece enquanto a pessoa digita, antes de salvar.
- Salvar não pode depender de a conta da tela ter chegado: vale o que a pessoa digitou por último.
- A tarefa continua com uma duração de 1 a 999 dias úteis; o fim é outro jeito de dizê-la.

## Opções consideradas

- D1. A tela conta os dias úteis pelos dias não úteis que o `GET /schedule` já traz para o sombreado.
- D2. Uma rota de leitura do servidor faz a conta (início e duração dão o fim; início e fim dão a duração), chamada enquanto a pessoa digita, e o pedido de incluir e editar a tarefa aceita o fim no lugar da duração.
- D3. O fim só ao salvar: o pedido aceita o fim, sem a conta enquanto a pessoa digita.

## Resultado da decisão

Opção escolhida: **D2**, porque a conta fica no servidor, com o calendário da instalação, e a tela só mostra o resultado (ADR-0051). D1 repetiria a regra na tela, e os dias não úteis do cronograma cobrem só os meses em volta das datas dele: um cronograma vazio não traz nenhum, e um fim mais distante cairia fora. D3 não mostra o outro campo antes de salvar, que é o pedido.

Regras:

- **Os campos.** No detalhe e em "Nova tarefa", a tarefa com datas próprias (não a tarefa-resumo, ADR-0088, nem o marco) tem o início, o fim e a duração em dias úteis. Vale o último que a pessoa digitou: digitar o fim calcula a duração; digitar a duração calcula o fim; mudar o início mantém o que vale e recalcula o outro, como o arrastar mantém a duração (ADR-0019).
- **A conta.** A duração é a quantidade de dias úteis do início, levado ao dia útil seguinte quando não é um, até o fim, os dois incluídos (`Schedule.DurationBetween`, a mesma do `PUT .../dates`), de 1 a 999; o fim é o último dia útil dessa duração. O fim antes do início, ou longe demais, é recusado no campo do fim.
- **A rota.** `GET /api/projects/{id}/schedule/item-dates?start=&duration=` ou `?start=&end=` devolve o início no dia útil, o fim e a duração, sem gravar nada, para quem vê o projeto (404 para quem não o vê); 400 nos campos inválidos.
- **Ao salvar.** O pedido de incluir e editar item aceita `end`: com ele, a duração da tarefa sai do início e do fim, e a `duration` do pedido não é olhada; sem ele, nada muda. O marco e a tarefa-resumo recusam o fim (400 em `end`).
- **As dependências.** A conta da tela usa o início digitado; ao salvar, as dependências ainda podem empurrar a tarefa (ADR-0019), e o detalhe volta com as datas que o servidor gravou.

### Consequências

- Bom, porque quem sabe o fim o digita, sem contar os dias úteis.
- Bom, porque a regra dos dias úteis continua num lugar só, e o detalhe e a edição na tabela calculam igual.
- Ruim, porque a tela faz uma chamada ao servidor a cada data ou duração completa digitada; a resposta é pequena e só leitura.

### Confirmação

Testes cobrindo: a rota com a duração e com o fim (início num sábado, fim num feriado), o fim antes do início e a duração fora de 1 a 999 recusados, a rota para quem não vê o projeto; o item incluído e editado com o fim, a duração calculada pelo calendário, o fim recusado no marco e na tarefa-resumo; e, na tela, digitar o fim calculando a duração, digitar a duração calculando o fim, mudar o início mantendo o que vale e salvar com o fim digitado (ADR-0056).

## Prós e contras das opções

### D1. A tela conta pelos dias não úteis do cronograma

- Bom, porque não chama o servidor.
- Ruim, porque repete a regra dos dias úteis e não tem os dias fora do período do cronograma.

### D2. O servidor calcula, pela rota de leitura e pelo pedido

- Bom, porque a conta é a mesma em todo lugar e salvar vale pelo que a pessoa digitou.
- Ruim, porque a tela depende de uma chamada para mostrar o outro campo.

### D3. O fim só ao salvar

- Bom, porque é a menor mudança.
- Ruim, porque a pessoa só vê a duração depois de salvar.
