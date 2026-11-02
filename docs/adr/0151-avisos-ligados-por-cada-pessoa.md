---
status: proposta
date: 2026-11-02
decision-makers: Marcelo Ribeiro
---

# ADR-0151: Avisos na hora por e-mail e pelo Teams ligados por cada pessoa, com a escolha na auditoria

Substitui do ADR-0149:

- a escolha entre E-mail, Teams e Os dois, com Os dois para quem nunca escolheu, que passa a ser duas escolhas independentes, com o e-mail ligado e o Teams desligado para quem nunca escolheu;
- a volta ao e-mail quando o aviso não chega pelo Teams, que deixa de existir;
- a escolha fora da auditoria, que passa a ser registrada.

Substitui do ADR-0037 só a regra de que os avisos na hora não se desligam: eles continuam sendo só os que pedem ação, e cada pessoa decide se recebe por e-mail. O resumo diário continua como está, uma escolha da pessoa, agora também registrada na auditoria.

## Contexto e problema

Os avisos que pedem ação saem na hora por e-mail (ADR-0037) e, com o Teams ligado na instalação, pela aba Atividade do Teams (ADR-0149). Cada pessoa escolhe em Meu perfil entre E-mail, Teams e Os dois; quem nunca escolheu recebe pelos dois, e sem o Teams os avisos na hora vão sempre por e-mail. O sino do FiscoProjects (ADR-0038) e o Meu trabalho (ADR-0096) mostram tudo o que pede ação, qualquer que seja o canal.

Em 02/11/2026, ao testar o Teams, Marcelo pediu que cada pessoa possa desligar o e-mail, o Teams ou os dois, ou deixar só um ligado; que o e-mail venha ligado e o Teams desligado, para quem quiser ligar; e que fique registrado na auditoria quem ligou ou desligou cada um.

Por onde os avisos na hora chegam a cada pessoa, quem decide e onde isso fica registrado?

## Fatores de decisão

- A pessoa decide por onde recebe, inclusive por nenhum canal além do sino.
- Nada que pede ação se perde: o sino e o Meu trabalho continuam mostrando tudo.
- Quem nunca escolheu recebe por e-mail, como antes do Teams.
- A administração vê, na auditoria, quem mudou a própria escolha e quando.
- A caixa de envio continua sendo configurada pelo administrador: ela serve a quem recebe por e-mail, ao resumo diário e ao teste.

## Opções consideradas

- A. Duas escolhas independentes, e-mail e Teams, com o e-mail ligado e o Teams desligado para quem nunca escolheu, na auditoria
- B. A escolha de hoje com mais uma opção, "Só no FiscoProjects"
- C. Desligar o e-mail para a instalação toda, pelo administrador

## Resultado da decisão

Opção escolhida: **A**, pedida por Marcelo em 02/11/2026.

- **Duas escolhas:** "Por e-mail" e "Pelo Teams", cada uma ligada ou desligada pela pessoa em Meu perfil, gravadas na hora. Com as duas desligadas, os avisos na hora ficam só no sino e no Meu trabalho.
- **Quem nunca escolheu:** e-mail ligado e Teams desligado. As escolhas já gravadas viram as duas: E-mail liga só o e-mail, Teams liga só o Teams e Os dois liga os dois.
- **Teams:** só aparece e só vale com os avisos pelo Teams ligados na instalação e para quem pode recebê-los (com o envio pelo Graph, quem entra pelo Entra ID), como no ADR-0149. Sem isso, a escolha fica guardada, sem efeito.
- **Sem volta ao e-mail:** quem desligou o e-mail não recebe e-mail nem quando o aviso não chega pelo Teams (aplicativo não instalado, recusa ou fim das tentativas); quem deixou o e-mail ligado já recebeu o e-mail. O cartão diz que o Teams só recebe com o aplicativo do FiscoProjects instalado.
- **Auditoria:** ligar e desligar o e-mail, o Teams e o resumo diário entram na auditoria, com a pessoa e quem mudou.
- **Sino e Meu trabalho:** sempre, sem escolha.
- **Administrador:** nada muda. A caixa de envio e "Avisar pelo Teams" continuam nas Configurações gerais.

### Consequências

- Bom, porque cada pessoa recebe só pelo canal que quer, e o Teams entra só para quem o ligou.
- Bom, porque a escolha fica registrada, e a administração sabe por que alguém deixou de receber e-mails.
- Bom, porque a regra fica mais simples: cada canal ligado recebe, sem a volta ao e-mail.
- Ruim, porque quem desliga os dois só vê o que pede ação ao abrir o FiscoProjects e pode perder um prazo; o cartão diz que os avisos continuam no sino e no Meu trabalho.
- Ruim, porque quem liga só o Teams sem o aplicativo instalado não recebe nada fora do sino; o cartão avisa que o aplicativo é preciso.
- Neutro, porque quem nunca escolheu deixa de receber pelo Teams até ligá-lo.

### Confirmação

Testes cobrindo:

- cada combinação dos dois canais, inclusive os dois desligados, com o sino sempre;
- o e-mail ligado e o Teams desligado para quem nunca escolheu, e a conversão das escolhas gravadas;
- o Teams sem efeito com os avisos pelo Teams desligados ou para quem entra sem o Entra ID;
- nenhum e-mail quando o Teams falha para quem desligou o e-mail;
- a mudança de cada escolha, e do resumo diário, na auditoria;
- o cartão Avisos de Meu perfil com as duas caixas e o resumo diário.

## Prós e contras das opções

### A. Duas escolhas independentes, na auditoria

- Bom, porque cobre todas as combinações que Marcelo pediu.
- Bom, porque o Teams fica desligado até a pessoa querer.
- Ruim, porque muda a forma da escolha e da API do ADR-0149.

### B. Mais uma opção, "Só no FiscoProjects"

- Bom, porque muda pouco.
- Ruim, porque continua sendo uma lista de combinações, e cada canal novo multiplica as opções.
- Ruim, porque o Teams continuaria ligado para quem nunca escolheu.

### C. E-mail desligado pelo administrador

- Bom, porque é uma configuração só.
- Ruim, porque tira a escolha de cada pessoa, que era o pedido.
