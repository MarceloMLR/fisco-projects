---
status: aceita
date: 2026-10-19
decision-makers: Marcelo Ribeiro
---

# ADR-0083: Pontos facultativos calculados pela portaria federal, contados como dia inteiro e escolhidos para todos os anos

## Contexto e problema

O ADR-0018 decidiu que o cronograma e os prazos contam dias úteis. Os feriados nacionais são calculados pelo sistema e os feriados locais são cadastrados, com a opção "repete todo ano". A empresa escolhe se considera cada ponto facultativo, e o administrador pode desconsiderar um feriado. Ficaram em aberto cinco perguntas:

- quais pontos facultativos o sistema calcula e quais ficam de fora;
- o que vale enquanto a empresa não escolhe;
- como contar os pontos facultativos de meio período, se as durações são em dias inteiros;
- até onde vale desconsiderar um feriado nacional;
- a partir de quando vale um feriado local que repete todo ano.

## Fatores de decisão

- Fonte oficial: a portaria anual do Ministério da Gestão e da Inovação (Portaria MGI nº 11.460/2025, para 2026).
- Nada específico de uma empresa fixo no código (ADR-0003).
- Durações e prazos em dias inteiros (ADR-0018, ADR-0025).
- Uma tela simples para o administrador, com uma escolha por data e sem regras escondidas.

## Opções consideradas

Para os pontos facultativos calculados:

- A. Os que se repetem todo ano na portaria: segunda e terça de Carnaval, Quarta-feira de Cinzas, Corpus Christi, Dia do Servidor Público, véspera de Natal e véspera de Ano-Novo. As emendas de cada ano entram como feriado local.
- B. Só Carnaval e Corpus Christi, os exemplos do ADR-0018.
- C. Nenhum: todo ponto facultativo cadastrado como feriado local.

Para o padrão de um ponto facultativo:

- D. Desconsiderado até a empresa escolher, ou seja, dia útil.
- E. Considerado até a empresa escolher, ou seja, dia de folga.

## Resultado da decisão

Opções escolhidas: **A** e **D**.

- **A**, porque cobre tudo o que a portaria repete todo ano, sem cadastro. As emendas (como 20/04 e 05/06 em 2026) mudam todo ano e não têm regra para calcular.
- **D**, porque a lei não obriga a folga num ponto facultativo. Um dia útil a mais só antecipa uma data, enquanto uma folga indevida esconderia um atraso.

Regras:

- **Feriados nacionais calculados:**
  - Confraternização Universal (01/01), Paixão de Cristo (Páscoa − 2), Tiradentes (21/04), Dia Mundial do Trabalho (01/05), Independência do Brasil (07/09), Nossa Senhora Aparecida (12/10), Finados (02/11), Proclamação da República (15/11) e Natal (25/12);
  - Dia Nacional de Zumbi e da Consciência Negra (20/11), a partir de 2024 (Lei nº 14.759/2023);
  - todos considerados até o administrador desconsiderar.
- **Pontos facultativos calculados:**
  - segunda e terça-feira de Carnaval (Páscoa − 48 e − 47), Quarta-feira de Cinzas (Páscoa − 46) e Corpus Christi (Páscoa + 60);
  - Dia do Servidor Público (28/10), véspera de Natal (24/12) e véspera de Ano-Novo (31/12);
  - todos desconsiderados até a empresa considerar.
- **Meio período:** a Quarta-feira de Cinzas e as vésperas são de meio período na portaria, mas contam como dia inteiro. Considerado, o dia todo deixa de ser útil; desconsiderado, o dia todo é útil.
- **Escolha para todos os anos:** considerar ou desconsiderar um feriado nacional ou um ponto facultativo vale em todos os anos. Uma exceção de um ano só é um feriado local sem repetição.
- **Feriado local:**
  - tem nome, data, "repete todo ano" e a escolha de ser considerado, que começa marcada;
  - quando repete, vale no mesmo dia e mês a partir do ano da data, e 29/02 só nos anos bissextos;
  - desconsiderar não apaga o feriado, e excluir o apaga.
- **Dias de trabalho:** pelo menos um dia da semana fica marcado (ADR-0018).
- **Interface:** o cálculo fica atrás de uma interface, como pede o ADR-0018. A Páscoa é a do calendário gregoriano.

### Consequências

- Bom, porque a empresa só cadastra o que é dela: feriados da cidade, do estado, emendas e recessos.
- Bom, porque cada data tem uma escolha só, igual em todos os anos.
- Ruim, porque um ponto facultativo mudado de dia pelo governo (o Dia do Servidor Público já foi transferido algumas vezes) pede desconsiderar a data calculada e cadastrar a nova como feriado local.
- Ruim, porque a tarde livre das vésperas e a manhã de trabalho da Quarta-feira de Cinzas não aparecem nas datas, que só contam dias inteiros.

### Confirmação

Testes cobrindo:

- as datas móveis pela Páscoa em vários anos;
- a Consciência Negra só a partir de 2024;
- os pontos facultativos desconsiderados por padrão;
- a escolha valendo em todos os anos;
- o feriado local que repete a partir do ano da data, inclusive 29/02;
- a recusa de deixar a semana sem dia de trabalho.

## Prós e contras das opções

### A. Os pontos facultativos que a portaria repete

- Bom, porque segue a fonte oficial e evita cadastro.
- Ruim, porque o Dia do Servidor Público interessa pouco a empresas privadas, que o deixam desconsiderado.

### B. Só Carnaval e Corpus Christi

- Bom, porque a lista é menor.
- Ruim, porque a Quarta-feira de Cinzas e as vésperas, comuns nas empresas, ficariam como cadastro manual a cada ano.

### C. Nenhum calculado

- Bom, porque não há regra no código.
- Ruim, porque o Carnaval e o Corpus Christi mudam de data todo ano e precisariam de cadastro todo ano.

### D. Desconsiderado por padrão

- Bom, porque segue a lei, em que o ponto facultativo não é folga obrigatória.
- Ruim, porque a empresa que folga no Carnaval precisa marcar os dias uma vez.

### E. Considerado por padrão

- Bom, porque muitas empresas folgam no Carnaval.
- Ruim, porque datas calculadas antes da escolha esconderiam dias de trabalho.
