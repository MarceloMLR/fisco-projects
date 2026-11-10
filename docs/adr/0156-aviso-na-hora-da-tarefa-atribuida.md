---
status: aceita
date: 2026-11-10
decision-makers: Marcelo Ribeiro
---

# ADR-0156: Aviso na hora da tarefa atribuída

Completa o ADR-0037 (avisos por e-mail na hora e resumo diário), cuja tabela dos avisos na hora ganha uma linha, e segue o ADR-0038 (sino e canais), o ADR-0149 (Teams) e o ADR-0151 (canais de cada pessoa).

## Contexto e problema

Quem passa a ser responsável por uma tarefa do cronograma não fica sabendo: das tarefas, só a mudança das datas entra no resumo diário do responsável. No chamado, quem é atribuído recebe o aviso na hora, por e-mail, pelo Teams e no sino. Em 10/11/2026, ao pedir o quadro das tarefas (ADR-0155), Marcelo pediu também o aviso das tarefas no Teams, e aprovou o plano da Tarefas-2. Quando o aviso sai, para quem e por onde?

## Fatores de decisão

- A pessoa sabe na hora que tem trabalho novo, como no chamado.
- Os canais e as regras de quem recebe continuam num lugar só (ADR-0038, ADR-0151).
- Nada de dezenas de avisos de uma vez quando o projeto nasce cheio de tarefas.

## Opções consideradas

- A. Aviso na hora, a quem passa a ser responsável, ao incluir a tarefa ou trocar o responsável
- B. A atribuição no resumo diário, como as datas mudadas
- C. Aviso na hora também a quem deixou de ser responsável

## Resultado da decisão

Opção escolhida: **A**, aprovada por Marcelo. A atribuição pede ação, como o chamado atribuído (B chega no dia seguinte); quem deixou de ser responsável não tem o que fazer (C só manda mais e-mail).

Regras:

| Item | Definição |
| --- | --- |
| Quando sai | Ao incluir uma tarefa com uma pessoa como responsável e ao trocar o responsável para uma pessoa, por qualquer mudança do cronograma de quem o edita |
| Quem recebe | Só o novo responsável, com as regras de sempre: nunca quem fez a ação, quem está desativado ou quem não vê o projeto |
| Por onde | Na hora: o e-mail e o Teams conforme a escolha da pessoa em Meu perfil (ADR-0151), e o sino sempre |
| O que diz | Quem atribuiu, a tarefa, o projeto e o fim; o link abre a tarefa no cronograma |
| Sem aviso | O cliente como responsável (ADR-0102); quem deixou de ser responsável; o desfazer e o refazer (ADR-0091); as tarefas criadas com o projeto por um modelo ou pelo MS Project (ADR-0017, ADR-0135) |
| Teams | Um tipo novo de atividade no pacote do aplicativo, cuja versão sobe: o Administrador do Teams sobe o pacote novo, como o guia de instalação já diz |

### Consequências

- Bom, porque a pessoa fica sabendo na hora, pelo canal que escolheu.
- Bom, porque nenhuma regra nova de quem recebe nem de canal.
- Ruim, porque incluir várias tarefas para a mesma pessoa manda um aviso por tarefa; o sino guarda todos, e o e-mail se desliga em Meu perfil.
- Ruim, porque o pacote do Teams precisa ser subido de novo; até lá, o aviso no Teams fica Falhou, e o e-mail e o sino seguem.

### Confirmação

Testes da API: o aviso ao incluir com responsável, só ao novo responsável na troca, nada para quem se atribui, o aviso no Teams, e nada para o cliente, no desfazer e no projeto criado pelo MS Project. Teste do texto do sino.

## Prós e contras das opções

### A. Aviso na hora ao novo responsável

- Bom, porque é o mesmo comportamento do chamado.
- Ruim, porque é mais um e-mail na hora.

### B. Atribuição no resumo diário

- Bom, porque junta tudo num e-mail só.
- Ruim, porque a pessoa só fica sabendo no dia seguinte.

### C. Aviso também a quem saiu

- Bom, porque ninguém é pego de surpresa.
- Ruim, porque avisa quem não tem mais o que fazer.
