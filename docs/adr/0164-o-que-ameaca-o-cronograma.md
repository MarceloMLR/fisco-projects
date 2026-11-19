---
status: aceita
date: 2026-11-19
decision-makers: Marcelo Ribeiro
---

# ADR-0164: O que ameaça o cronograma: chamados ligados a tarefas e marcos, e a projeção do atraso

Completa o ADR-0022 (chamado sempre num projeto), o ADR-0028 (chamados em fornecedores), o ADR-0073 (visão geral do projeto) e a folga das tarefas (GanttB-1), sem mudar o ADR-0051: o servidor continua a fonte das datas, e nada aqui muda uma data sozinho. Aprovado por Marcelo em 19/11/2026.

## Contexto e problema

Num projeto de implantação, o que mais preocupa o gerente e o cliente é o que pode atrasar o go-live. Hoje o GerenWare sabe muito disso, mas em lugares separados: o cronograma tem as dependências, o caminho crítico e a folga (ADR-0019, ADR-0137); o chamado tem o prazo, o andamento e o registro no fornecedor (ADR-0025, ADR-0028); e a Visão geral lista as tarefas atrasadas com as que dependem delas ("bloqueia Configurar plano de contas"). Nada diz "este chamado, parado no fornecedor, ameaça o marco Go-live", nem "esta tarefa atrasada empurra o Go-live em 3 dias úteis". O gerente descobre olhando as setas da Linha do tempo e escreve isso à mão no status report.

Em 19/11/2026, Marcelo trouxe o pedido de um gerente de projetos: no MS Project ele vê a data prevista e a efetiva de cada tarefa e o que cada tarefa ou chamado aberto impacta no cronograma ("esse chamado impacta no go-live", "essa tarefa atrasada impacta na X"). Trouxe também um status report de exemplo, cujo segundo slide é exatamente um chamado no fornecedor com "as etapas que dependem da resolução". Como ligar um chamado ao cronograma, o que o sistema calcula a partir disso e o que fazer com a tarefa atrasada?

## Fatores de decisão

- O gerente vê, num lugar só, o que ameaça cada marco, sem montar à mão.
- O servidor continua a fonte das datas (ADR-0051): o cronograma só muda por quem o edita, e o desfazer continua sabendo de quem é cada mudança (ADR-0091).
- O prazo do chamado é um compromisso de atendimento (ADR-0025), não uma previsão do plano.
- As permissões continuam as de hoje (ADR-0043, ADR-0044): todos do projeto veem o cronograma e os chamados dele.
- É a base do relatório de status (ADR-0165).

## Opções consideradas

Chamado e cronograma:

- A1. O chamado aponta as tarefas e os marcos que ele impacta, e o cronograma calcula o que depende deles.
- A2. O chamado vira uma dependência do cronograma: o item não termina antes de o chamado ser resolvido, e o prazo de resolução empurra as datas.
- A3. Um campo de texto "Impacta" no chamado.

Tarefa atrasada:

- B1. Projeção sem gravar: as datas que a tarefa atrasada e as que dependem dela teriam, mostradas como aviso, e quem edita decide.
- B2. Reprogramar sozinho: a tarefa atrasada passa para depois de hoje e empurra as seguintes, como o "reprogramar trabalho não concluído" do MS Project.
- B3. Continuar como hoje: o atraso de cada tarefa e as que dependem dela, sem dizer o que acontece com os marcos.

## Resultado da decisão

Opções escolhidas: **A1** e **B1**, aprovadas por Marcelo. A2 misturaria o prazo de atendimento com o plano e mudaria datas sem ninguém editar; A3 não liga nada e não aparece no cronograma. B2 mudaria o cronograma sem a decisão de quem o edita, fora do desfazer de alguém; B1 mostra a consequência e deixa a decisão com quem edita, que já pode mover a tarefa ou o cronograma inteiro (ADR-0140). B3 é o que existe e não responde a pergunta.

Regras do chamado ligado ao cronograma (Status-1):

| Item | Definição |
| --- | --- |
| A ligação | O chamado aponta de 1 a 10 tarefas (também a tarefa-resumo e a subtarefa) ou marcos do cronograma do projeto dele. Fase não: a fase agrupa, e quem impacta é o item |
| Quem liga e desliga | Quem atende o chamado (o administrador, os gestores e quem atende o time, ADR-0044), com o chamado não fechado, como o registro no fornecedor (ADR-0028). Os outros recebem 403 |
| O que depende | A partir dos itens ligados, o servidor segue as dependências, de qualquer tipo (ADR-0137), até os marcos ainda não atingidos que vêm depois, e diz se algum item ligado está no caminho crítico, quando um atraso nele atrasa o fim do projeto. Calculado a cada leitura, com as datas atuais |
| Quando conta | Só o chamado em aberto (nem resolvido nem fechado) ameaça o cronograma. O resolvido e o fechado guardam a ligação, sem contar |
| No chamado | A seção "No cronograma" do painel: os itens ligados, com as datas e o atraso, os marcos ameaçados e o aviso do caminho crítico, e "Alterar" para quem atende |
| No cronograma | A marca do chamado ao lado do nome, na Lista e no Quadro, com a quantidade de chamados em aberto ligados ao item; no detalhe do item, a seção "Chamados", com os ligados a ele; no detalhe do marco, também os que o ameaçam pelas dependências |
| Na Visão geral | Em "Precisa de atenção", os chamados em aberto que ameaçam um marco não atingido, com o marco e o prazo do chamado |
| Histórico | Ligar e desligar entram na conversa como evento e na auditoria, com os itens pelo nome atual (ADR-0084) |
| Item excluído | A ligação fica guardada pelo id e volta com o desfazer da exclusão, como os comentários (ADR-0157); enquanto o item não existe, ela não aparece |
| Exclusão do projeto | As ligações saem com ele (ADR-0111) |

Regras da projeção do atraso (Status-2):

| Item | Definição |
| --- | --- |
| Tarefa atrasada | Em aberto, com datas próprias, que já devia ter terminado, ou começado com 0% (ADR-0020) |
| A projeção | O que falta da tarefa (a duração vezes a parte que falta pelo progresso, em dias úteis, arredondada para cima, no mínimo 1 dia) a partir de hoje, ou do próximo dia útil quando hoje não é útil; as que dependem dela andam pelas regras das dependências (ADR-0137), só quando não cabem mais. Nada é gravado |
| O que mostra | Para cada tarefa atrasada, os marcos não atingidos que mudariam, em quantos dias úteis, e o fim do projeto, quando muda: "4 dias úteis atrasada; empurra o marco Go-live em 3 dias úteis" |
| Onde | No detalhe da tarefa, em "Precisa de atenção" da Visão geral, no lugar do "bloqueia ..." de hoje, e no relatório de status (ADR-0165) |
| Quem decide | Quem edita o cronograma: muda a tarefa, move o cronograma (ADR-0140) ou deixa como está |

Fora:

| Item | Classificação |
| --- | --- |
| Projetar pelo prazo do chamado | fora: o prazo é compromisso de atendimento (ADR-0025) |
| Reprogramar o cronograma sozinho | fora (B2) |
| Ligar o chamado a uma fase | fora |
| Ligar o chamado já na abertura | depois |
| Aviso por e-mail, Teams e sino quando um chamado passa a ameaçar um marco | depois |
| Ligar chamados ao cronograma de outro projeto | fora (ADR-0019) |

### Consequências

- Bom, porque o gerente e o cliente veem o que ameaça cada marco sem montar à mão, e o status report sai pronto (ADR-0165).
- Bom, porque nenhuma data muda sozinha: a projeção é um aviso, e o cronograma continua sendo o que quem edita decidiu.
- Bom, porque responde a objeção "card faz o mesmo": o card não sabe o que ele atrasa.
- Ruim, porque a ligação depende de quem atende se lembrar de fazê-la; sem ela, o chamado não aparece no cronograma.
- Ruim, porque a projeção supõe que o que falta segue o ritmo planejado; um bloqueio externo pode atrasar mais.

### Confirmação

Testes da API: ligar e desligar por quem atende e a recusa dos outros (403), no chamado fechado (409) e com item de outro projeto ou fase (400); os marcos ameaçados pelas dependências de cada tipo e o aviso do caminho crítico; o resolvido e o fechado fora da contagem; o evento na conversa e a auditoria com o nome atual; o item excluído e o desfazer; a exclusão do projeto. Testes do domínio para a projeção: a tarefa atrasada com e sem progresso, a dependência que absorve o atraso pela folga, o fim do projeto que muda. Testes de tela: a seção do chamado, a marca e a seção no cronograma e a linha em "Precisa de atenção". A matriz de quem liga é coberta por inteiro (ADR-0056).

## Prós e contras das opções

### A1. O chamado aponta os itens que impacta

- Bom, porque usa o que o cronograma já sabe (dependências, caminho crítico) para dizer o que vem depois.
- Bom, porque o chamado continua com as próprias regras e permissões.
- Ruim, porque é mais uma ação para quem atende.

### A2. O chamado como dependência do cronograma

- Bom, porque o cronograma reage sozinho ao chamado.
- Ruim, porque o prazo de atendimento não é previsão do plano, e as datas mudariam sem ninguém editar.

### A3. Texto "Impacta"

- Bom, porque é o mais simples.
- Ruim, porque não aparece no cronograma nem calcula nada.

### B1. Projeção sem gravar

- Bom, porque mostra a consequência e deixa a decisão com quem edita.
- Ruim, porque o cronograma pode continuar mostrando datas que já não vão acontecer, até alguém agir.

### B2. Reprogramar sozinho

- Bom, porque as datas ficam sempre realistas.
- Ruim, porque muda o plano sem decisão, fora do desfazer, e esconde do cliente o que foi combinado.

### B3. Como hoje

- Bom, porque não muda nada.
- Ruim, porque não responde o que acontece com os marcos.
