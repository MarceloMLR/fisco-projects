---
status: aceita
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0037: Avisos por e-mail imediatos só para o que pede ação, com resumo diário para o restante

## Contexto e problema

O e-mail é o canal de notificação da v1 (ADR-0036). Há muitos eventos possíveis: abertura, status, mensagens, menções, trocas de responsável e de solicitante, prazos, fornecedores, cronograma (inclusive datas empurradas em cadeia, ADR-0019), projetos e termos de aceite. Se cada evento gerar um e-mail imediato, quem acompanha muitos chamados recebe dezenas de e-mails por dia e passa a ignorá-los. Quais avisos são enviados e com que frequência?

## Fatores de decisão

- O que pede ação chega na hora.
- Evitar excesso de e-mails, para que as pessoas não ignorem os avisos do sistema.
- Complexidade proporcional à v1.

## Opções consideradas

- A. Um e-mail imediato por evento, para todos os eventos
- B. Imediato só para o que pede ação; o restante num resumo diário
- C. Só um resumo diário

## Resultado da decisão

Opção escolhida: **B. Imediato para o que pede ação, resumo diário para o restante**, porque o urgente chega na hora e o informativo vem em um único e-mail por dia.

Escopo:

| Item | Classificação |
| --- | --- |
| E-mail imediato para o que pede ação da pessoa: chamado novo para ela ou para o destino que ela atende, resposta ou pedido de informação num chamado em que está envolvida, menção, troca de responsável ou de solicitante para ela, chamado resolvido aguardando a confirmação dela, pedido de aceite e prazo de chamado vencendo ou vencido | v1 |
| Resumo diário, num horário configurável, com o restante: mudanças de status nos chamados que acompanha, atualizações de fornecedor, datas alteradas no cronograma (inclusive as empurradas em cadeia), tarefas que vencem nos próximos dias, aceites ainda pendentes, inclusão em projetos e mudanças de papel ou de status do projeto | v1 |
| Quem fez a ação não recebe o e-mail dela | v1 |
| Todo e-mail tem um link direto para o item no FiscoProjects | v1 |
| A pessoa pode desligar o resumo diário; os e-mails imediatos não podem ser desligados | v1 |
| Escolher, evento por evento, o que receber | depois |
| Juntar num só e-mail vários eventos do mesmo chamado em poucos minutos | depois |

Regras complementares:

- Resumo sem novidades não é enviado.
- Os textos dos e-mails ficam em arquivos de tradução (ADR-0013).
- Nenhum e-mail contém senha (ADR-0033).

### Consequências

- Bom, porque os e-mails imediatos são poucos e relevantes.
- Bom, porque mudanças em cadeia no cronograma viram um item do resumo, e não dezenas de e-mails.
- Ruim, porque o informativo chega com até um dia de atraso.
- Ruim, porque exige um processo diário e uma preferência por pessoa.

### Confirmação

Testes cobrindo: cada evento imediato gera e-mail só para os destinatários corretos; eventos do resumo não geram e-mail imediato e aparecem no resumo seguinte; quem fez a ação não recebe o próprio aviso; quem desligou o resumo não o recebe, mas continua recebendo os imediatos; resumo sem novidades não é enviado; todo e-mail tem link para o item.

## Prós e contras das opções

### A. Um e-mail por evento

- Bom, porque é o mais simples.
- Ruim, porque gera excesso de e-mails e leva as pessoas a ignorá-los.

### B. Imediato para ação, resumo para o restante

- Bom, porque equilibra urgência e volume.
- Ruim, porque exige classificar eventos e manter um processo diário.

### C. Só resumo diário

- Bom, porque gera o mínimo de e-mails.
- Ruim, porque o urgente chega tarde.
