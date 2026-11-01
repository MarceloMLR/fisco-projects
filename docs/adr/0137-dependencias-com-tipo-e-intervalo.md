---
status: aceita
date: 2026-11-01
decision-makers: Marcelo Ribeiro
---

# ADR-0137: Dependências com tipo e intervalo em dias úteis

Muda, no ADR-0019, o item "Outros tipos de dependência (começa junto, termina junto) e intervalo entre tarefas", que era "depois", e, no ADR-0135, o que a importação do MS Project faz com esses tipos e intervalos.

## Contexto e problema

Hoje toda dependência é "termina, então começa", sem intervalo: a tarefa seguinte começa no dia útil depois do fim da anterior (o marco, no mesmo dia). Nos projetos fiscais isso não cobre casos comuns: os testes começam junto com a parametrização, o treinamento termina junto com a migração, a homologação do cliente começa 5 dias úteis depois da entrega para dar tempo de ele montar o ambiente. Sem isso, a gestão desenha datas à mão e as perde no próximo empurrão; o arquivo do MS Project que tem esses tipos e intervalos chega com eles trocados por "termina, então começa" e um aviso (ADR-0135). Marcelo pediu os tipos e o intervalo no grupo 14 (GanttA-4), sem perder nada na ida e volta pelo MS Project.

## Fatores de decisão

- Datas coerentes com o que a equipe combina, sem trabalho manual.
- Continuar empurrando só para frente: antecipar continua sendo decisão da gestão (ADR-0019).
- Ida e volta pelo MS Project sem perder o tipo nem o intervalo.
- Cálculo e testes proporcionais: o mesmo calendário de dias úteis (ADR-0018) em tudo.

## Opções consideradas

- A. Os quatro tipos do MS Project, com intervalo em dias úteis, em horas ou em porcentagem
- B. Três tipos (termina, então começa; começa junto; termina junto), com intervalo em dias úteis, positivo ou negativo
- C. Só o intervalo, no tipo "termina, então começa"

## Resultado da decisão

Opção escolhida: **B. Três tipos, com intervalo em dias úteis, positivo ou negativo**, porque cobre os casos da equipe, cabe no cálculo em dias úteis que o cronograma já faz e vai e volta do MS Project sem perda.

| Item | Classificação |
| --- | --- |
| "Termina, então começa" (a de hoje): a seguinte começa no dia útil depois do fim da anterior, mais o intervalo | v2 |
| "Começa junto" (início com início): a seguinte começa no dia útil do início da anterior, mais o intervalo | v2 |
| "Termina junto" (término com término): a seguinte termina no dia útil do fim da anterior, mais o intervalo | v2 |
| Intervalo em dias úteis do calendário da instalação, de -99 a 999: positivo espera, negativo adianta | v2 |
| O marco segue as mesmas regras, com o dia dele como início e fim; em "termina, então começa", o marco fica no dia do fim da anterior e a tarefa no dia útil seguinte, como hoje | v2 |
| Empurrar só para frente, em cadeia; antecipar não puxa (ADR-0019) | mantido |
| Caminho crítico, recálculo das datas (ADR-0126), modelos de projeto (ADR-0017) e desfazer (ADR-0091) com o tipo e o intervalo | v2 |
| Exportar e importar o MS Project com os três tipos e o intervalo em dias (`Type` 0, 1 e 3; `LinkLag` em décimos de minuto, `LagFormat` 7) | v2 |
| "Começa, então termina" (o quarto tipo do MS Project) | fora: na importação vira "termina, então começa", com o aviso de hoje |
| Intervalo em horas, em dias corridos ou em porcentagem | fora: na importação vira dias úteis, arredondado, com aviso |
| Dependências entre projetos diferentes | fora (ADR-0019) |

### Consequências

- Bom, porque a gestão desenha o que combina com a equipe e o cliente, e o empurrão mantém isso.
- Bom, porque o arquivo do MS Project vem e volta com os tipos e os intervalos que a equipe usa.
- Ruim, porque o cálculo do início, do caminho crítico e do recálculo ganha três regras e o intervalo negativo, com mais testes.
- Ruim, porque o pedido de incluir e editar item passa a levar o tipo e o intervalo de cada dependência (`links`), além da lista de anteriores de sempre.

### Confirmação

Testes do domínio para cada tipo, com intervalo positivo e negativo, com marcos, feriados e fins de semana; o empurrão em cadeia e o "antecipar não puxa"; o caminho crítico; o recálculo; o desfazer; o modelo; e a ida e volta pelo XML do MS Project.

## Prós e contras das opções

### A. Os quatro tipos, com intervalo em horas e porcentagem

- Bom, porque é o que o MS Project tem.
- Ruim, porque "começa, então termina" quase não se usa e o intervalo em horas ou porcentagem não cabe no cronograma em dias úteis (ADR-0018).

### B. Três tipos, com intervalo em dias úteis

- Bom, porque cobre os casos comuns e continua no mesmo calendário.
- Ruim, porque o quarto tipo do MS Project continua virando "termina, então começa" na importação.

### C. Só o intervalo

- Bom, porque é o menor cálculo.
- Ruim, porque não resolve "começa junto" e "termina junto", que são o pedido principal.
