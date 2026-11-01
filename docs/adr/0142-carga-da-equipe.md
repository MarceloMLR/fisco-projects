---
status: aceita
date: 2026-11-01
decision-makers: Marcelo Ribeiro
---

# ADR-0142: Carga da equipe por semana, pelas horas previstas e pelas ausências

Usa o ADR-0138 (horas previstas por tarefa, só do administrador) e o ADR-0141 (ausências das pessoas), e completa o ADR-0128 (horas do projeto), cujas regras de quem vê as horas continuam valendo.

## Contexto e problema

O administrador põe as horas previstas de cada tarefa (ADR-0138) e a gestão cadastra as férias e as folgas de cada pessoa (ADR-0141), mas ninguém vê, entre os projetos, quanto cada pessoa tem para fazer em cada semana: o Diego tem a migração dos saldos de um projeto e a integração de outro na mesma semana em que sai de férias, e isso só aparece quando as duas atrasam. Marcelo pediu, no grupo 15 (GanttB-5), a carga de cada pessoa por semana, pelas horas previstas e pelas ausências, com o aviso de sobrecarga. Quem vê, como as horas de uma tarefa se espalham pelas semanas e quantas horas cabem numa semana?

## Fatores de decisão

- Achar a pessoa sobrecarregada, e a semana, antes do atraso, entre todos os projetos em aberto.
- Usar só o que o sistema já tem: as horas previstas, os responsáveis, as datas do cronograma, o calendário da instalação e as ausências.
- Uma regra simples e previsível, num lugar só, que a tela possa explicar numa frase.
- Não criar dado pessoal novo (LGPD, ADR-0058) e manter as horas com quem já as vê (ADR-0128, ADR-0138).

## Opções consideradas

Quem vê a carga:

- A1. Só o administrador, como as horas previstas.
- A2. O administrador e os gerentes, com as pessoas dos projetos deles.

Como as horas de uma tarefa se espalham:

- B1. As horas previstas da tarefa em partes iguais pelos dias úteis dela, do início ao fim.
- B2. As horas que faltam (as previstas vezes o que falta do progresso) pelos dias úteis que faltam, a partir de hoje.

Quantas horas cabem num dia:

- C1. Um número da instalação, "Horas por dia de trabalho", nas Configurações gerais, 8 de início.
- C2. Um número fixo de 8 horas.
- C3. Um número por pessoa (meio período, estagiário).

## Resultado da decisão

Opções escolhidas: **A1**, **B1** e **C1**.

A1 porque a carga só existe pelas horas previstas, que o ADR-0138 deixou com o administrador por decisão de Marcelo: mostrar a carga ao gerente seria mostrar as previstas por outro caminho. B1 porque é a regra que a tela explica numa frase ("as horas previstas de cada tarefa em partes iguais pelos dias úteis dela") e não muda de um dia para o outro sem ninguém mexer no cronograma; B2 pede o progresso em dia e joga numa semana só as horas de uma tarefa atrasada. C1 porque a jornada varia por empresa (ADR-0003) e a mesma para todos não cria dado pessoal; C3 pediria um cadastro por pessoa, com contrato de trabalho, fora da v2.

Regras:

- **Tarefas que contam.** As tarefas (também a resumo e a subtarefa, cada uma com as próprias previstas, ADR-0138) abaixo de 100%, com uma pessoa como responsável (não o cliente, ADR-0102) e com horas previstas, dos projetos não concluídos nem cancelados. O marco não tem horas.
- **Carga do dia.** As horas previstas da tarefa divididas pelos dias úteis dela (calendário da instalação, ADR-0018), em cada dia útil do início ao fim; a carga da semana é a soma dos dias dela, de segunda a domingo, de todas as tarefas da pessoa, em todos os projetos.
- **Capacidade da semana.** Os dias úteis da semana menos os dias úteis em que a pessoa está ausente (ADR-0141), vezes as horas por dia de trabalho. A ausência não tira a carga: a tarefa que cai nela pesa numa semana de capacidade menor, e é isso que o aviso mostra.
- **Estado de cada semana.** Acima da capacidade (a carga passa da capacidade, também com capacidade zero), perto do limite (a partir de 90% da capacidade, o mesmo limite do saldo, ADR-0128), dentro e livre (sem carga). Um lugar só no domínio das horas.
- **Horas por dia de trabalho.** Nas Configurações gerais, de 1 a 12 horas, em quartos de hora, 8 de início, na auditoria como as outras configurações.
- **Pessoas.** As pessoas ativas dos projetos em aberto, em qualquer papel, mesmo sem carga, para mostrar quem está livre; quem tem carga sem estar mais num projeto em aberto também aparece.
- **Onde aparece.** Só para o administrador, em Administração → Acompanhamento → "Carga da equipe": oito semanas a partir da atual, com as anteriores e as seguintes, a busca pelo nome e a visão "Com sobrecarga"; o detalhe da pessoa mostra, em cada semana, as tarefas que pesam nela (o projeto, a tarefa e as horas na semana) e as ausências.

Fora:

| Item | Classificação |
| --- | --- |
| Capacidade por pessoa (meio período) | depois, com ADR novo |
| Carga para o gerente ou para a própria pessoa | fora (ADR-0138) |
| Aviso por e-mail ou no sino da sobrecarga | depois |
| Nivelamento automático (mover tarefas para tirar a sobrecarga) | fora: a decisão de replanejar é da gestão (ADR-0019) |
| Carga pelas horas apontadas | fora: a carga olha para a frente; o apontado está no saldo (ADR-0128) |

### Consequências

- Bom, porque o administrador acha a semana sobrecarregada de cada pessoa entre todos os projetos, antes do atraso, e vê o que pesa nela.
- Bom, porque a regra usa só dados que já existem e cabe numa frase.
- Ruim, porque a tarefa sem horas previstas não pesa: a carga é tão boa quanto as previstas que o administrador põe.
- Ruim, porque B1 continua espalhando as horas pelos dias que já passaram de uma tarefa em andamento, mesmo que o trabalho tenha ficado para depois.

### Confirmação

Testes cobrindo: a carga de uma tarefa espalhada pelos dias úteis dela, também sobre um feriado e entre duas semanas; a soma entre projetos; a capacidade com as ausências; os quatro estados; as tarefas concluídas, do cliente, sem previstas e dos projetos encerrados de fora; as horas por dia de trabalho nas configurações, com os limites e a auditoria; e a matriz de permissões da rota nova, só do administrador (ADR-0056).

## Prós e contras das opções

### A1. Só o administrador

- Bom, porque segue o ADR-0138 sem abrir as horas previstas.
- Ruim, porque o gerente, que monta o cronograma, não vê a carga das pessoas dele.

### A2. O administrador e os gerentes

- Bom, porque quem planeja vê a carga.
- Ruim, porque mostraria as horas previstas, que o ADR-0138 deixou só com o administrador, e a carga das pessoas em projetos de outros gerentes.

### B1. As previstas pelos dias úteis da tarefa

- Bom, porque é simples, previsível e explicável.
- Ruim, porque ignora o progresso.

### B2. O que falta pelos dias que faltam

- Bom, porque olha o que falta de fato.
- Ruim, porque depende do progresso em dia e põe numa semana só todas as horas de uma tarefa atrasada.

### C1. Um número da instalação

- Bom, porque cada empresa tem a própria jornada e nenhum dado novo de pessoa é guardado.
- Ruim, porque o meio período aparece como sobrecarregado ou livre demais.

### C2. Oito horas fixas

- Bom, porque não há configuração.
- Ruim, porque fixa no código algo que varia por empresa (ADR-0003).

### C3. Por pessoa

- Bom, porque é exato.
- Ruim, porque pede um cadastro por pessoa, com dado de contrato de trabalho.
