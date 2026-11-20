---
status: aceita
date: 2026-11-20
decision-makers: Marcelo Ribeiro
---

# ADR-0167: Dia da conclusão e do marco atingido informado por quem conclui

Completa, no ADR-0020, a linha "Data real de conclusão registrada quando a tarefa chega a 100%" e, no ADR-0086, o "Marcar como atingido" com o dia de hoje: o dia de hoje continua sendo o padrão, e quem conclui pode informar outro, de hoje para trás. O resto dos dois continua. Aprovado por Marcelo em 20/11/2026.

## Contexto e problema

A tarefa guarda o dia em que chegou a 100% (ADR-0020), e o marco, o dia em que foi marcado como atingido (ADR-0086), sempre o dia de hoje no fuso da instalação. Na Status-3, esses dias aparecem como o "Fim real", ao lado do fim previsto da linha de base, como no MS Project. Quem atualiza o cronograma com alguns dias de atraso (a tarefa terminou na sexta, e o responsável marca na segunda) deixa o fim real errado, sem como corrigir. Na demonstração, a carga conclui as tarefas no dia em que roda, e uma tarefa planejada para 23/10 aparece terminada em 19/11, como se tudo tivesse atrasado. Quem informa o dia, qual dia vale e o que fica como está?

## Fatores de decisão

- O fim real precisa dizer o dia em que a tarefa terminou, não o dia em que alguém atualizou o sistema.
- As permissões continuam as de hoje: quem atualiza o progresso (ADR-0043) e quem marca o marco (o administrador e o gerente, ADR-0086).
- O caso comum continua com um clique ("Concluir", "Marcar como atingido", o Quadro e o Meu trabalho, ADR-0086, ADR-0155).
- Ninguém informa uma conclusão no futuro.

## Opções consideradas

- D1. Manter o dia de hoje, sem informar outro.
- D2. Quem conclui informa o dia, de hoje para trás, ao concluir pelo diálogo do progresso ou ao corrigir o dia de uma tarefa concluída ou de um marco atingido; o clique rápido continua com o dia de hoje.
- D3. O fim real como um campo livre do detalhe, que quem edita muda a qualquer momento, também sem a tarefa estar a 100%.

## Resultado da decisão

Opção escolhida: **D2**, pela decisão de Marcelo. D1 deixa o fim real errado sempre que o sistema é atualizado depois; D3 desliga o fim real da conclusão e deixa uma tarefa em aberto com fim real.

Regras:

| Item | Definição |
| --- | --- |
| Tarefa | Ao passar para 100%, ou já a 100%, quem atualiza o progresso dela (ADR-0043) pode informar o dia da conclusão; sem informar, vale a regra do ADR-0020 (o dia de hoje ao chegar a 100%, o mesmo dia ao salvar de novo) |
| Marco | Ao marcar como atingido, ou já atingido, o administrador e o gerente podem informar o dia (ADR-0086); sem informar, vale o dia de hoje |
| Dia aceito | De 01/01/2000 até hoje, no fuso da instalação; dia no futuro, ou informado abaixo de 100% ou sem marcar o marco, recebe 400 |
| Tarefa-resumo e marco atingido pelo termo | Sem mudança: a tarefa-resumo fica com o dia da última subtarefa (ADR-0088), e o termo de aceite atinge o marco no dia em que é assinado (ADR-0129) |
| Desfazer e auditoria | Como qualquer mudança do progresso ou do marco (ADR-0039, ADR-0091) |
| Tela | O diálogo do progresso pede o dia ao salvar 100%, com o dia de hoje; a tarefa concluída e o marco atingido ganham "Alterar dia" no detalhe |
| Demonstração | A carga informa, nas tarefas concluídas e nos marcos atingidos, o fim planejado, sem passar do dia em que roda |

### Consequências

- Bom, porque o fim real mostra o dia em que a tarefa terminou, também quando o sistema é atualizado depois.
- Bom, porque o clique rápido continua igual.
- Ruim, porque o fim real passa a ser informado por quem conclui, e não só registrado pelo sistema; a auditoria mostra quem mudou e quando.

### Confirmação

Testes do domínio e da API: concluir com o dia informado, corrigir o dia de uma tarefa concluída e de um marco atingido, o dia no futuro e o dia sem concluir recusados, o clique rápido com o dia de hoje e as permissões de hoje (ADR-0056). Testes de tela do diálogo do progresso e do "Alterar dia".

## Prós e contras das opções

### D1. Só o dia de hoje

- Bom, porque não muda nada.
- Ruim, porque o fim real fica errado sempre que alguém atualiza o sistema depois, e a demonstração mostra tudo atrasado.

### D2. Dia informado ao concluir

- Bom, porque corrige o fim real sem perder o clique rápido.
- Ruim, porque o diálogo do progresso ganha um campo.

### D3. Fim real como campo livre

- Bom, porque é o mais flexível.
- Ruim, porque desliga o fim real da conclusão: uma tarefa em aberto poderia ter fim real.
