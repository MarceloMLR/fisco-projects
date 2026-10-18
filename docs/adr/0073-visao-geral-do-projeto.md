---
status: aceita
date: 2026-10-17
decision-makers: Marcelo Ribeiro
---

# ADR-0073: Visão geral do projeto responde "está no prazo?"

## Contexto e problema

O ADR-0011 incluiu a aba Visão geral no projeto, mas nenhum ADR definiu o seu conteúdo. Na revisão das telas, Marcelo pediu resumos com foco estratégico para gerentes de projeto e desenvolvedores. O que a Visão geral mostra?

## Fatores de decisão

- O gerente decide o que fazer sem abrir cada aba.
- Números que se calculam com os dados da v1, sem depender de histórico acumulado.
- Coerência com a tela inicial (ADR-0070) e com o cronograma (ADR-0071).

## Opções consideradas

- A. Painel com gráficos (chamados por status, evolução do progresso no tempo)
- B. Tela enxuta que responde "está no prazo?": quatro números, fases e marcos, o que precisa de atenção e a equipe

## Resultado da decisão

Opção escolhida: **B**, aprovada por Marcelo.

Escopo:

| Item | Classificação |
| --- | --- |
| Andamento real contra o previsto; o previsto é quanto deveria estar concluído hoje pelo cronograma, ou pela linha de base quando houver (ADR-0020, ADR-0021) | v1 |
| Prazo final do projeto, com o desvio em dias úteis contra a linha de base | v1 |
| Chamados abertos, com quantos estão vencidos e quantos aguardam o solicitante | v1 |
| Próximo marco, com a data e quantos dias úteis faltam | v1 |
| Fases e marcos numa linha do tempo menor que se ajusta ao espaço, com link para o cronograma (ADR-0071) | v1 |
| "Precisa de atenção": tarefas atrasadas, chamados perto do prazo ou sem responsável, aceites pendentes e chamados de fornecedor sem retorno (ADR-0028) | v1 |
| Equipe por time (Gestão, Funcional, Desenvolvimento), com quantas pessoas e quantos chamados abertos, e link para Membros | v1 |
| Gráficos de evolução (progresso no tempo, chamados abertos e fechados por semana) | depois (com os indicadores gerenciais do ADR-0010) |

### Consequências

- Bom, porque o gerente sabe de relance se o projeto está no prazo e o que fazer.
- Bom, porque tudo vem de dados que a v1 já tem.
- Ruim, porque o "previsto" depende de o cronograma estar atualizado; sem cronograma, a aba mostra "sem cronograma" nos números de prazo (ADR-0016).

### Confirmação

Testes cobrindo: o andamento previsto é calculado pelas datas do cronograma (ou da linha de base) em dias úteis; o desvio do prazo final aparece em dias úteis; "Precisa de atenção" lista só itens do projeto e dentro do acesso da pessoa; projeto sem cronograma mostra "sem cronograma".

## Prós e contras das opções

### A. Painel com gráficos

- Bom, porque impressiona em demonstrações.
- Ruim, porque depende de histórico acumulado e não aponta a próxima ação.

### B. Tela enxuta

- Bom, porque responde a pergunta do gerente e leva à ação.
- Ruim, porque não mostra tendências no tempo na v1.
