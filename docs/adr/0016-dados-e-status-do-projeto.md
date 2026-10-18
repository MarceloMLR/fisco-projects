---
status: aceita (item "Excluir projeto" substituído pelo ADR-0041)
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0016: Dados e status do projeto, com datas e progresso calculados pelo cronograma

## Contexto e problema

O MVP tem código, nome, status, progresso e próximo marco. Falta definir os campos do projeto, quais são digitados e quais são calculados, e o ciclo de vida (status). O cliente é opcional (ADR-0015).

## Fatores de decisão

- Informação sempre coerente com o cronograma.
- Preservar histórico de projetos encerrados.
- Escopo enxuto na v1.

## Opções consideradas

- A. Datas, progresso e próximo marco calculados a partir do cronograma
- B. Datas e progresso digitados pelo gestor

## Resultado da decisão

Opção escolhida: **A. Calculados a partir do cronograma**, porque evita informação desatualizada e mantém uma única fonte para datas e progresso.

Escopo:

| Item | Classificação |
| --- | --- |
| Código curto e único (ex.: ERP-FIN), nome e cliente (opcional, ADR-0015) | v1 |
| Descrição do projeto, em texto longo | v1 |
| Datas de início e fim, progresso e próximo marco calculados a partir do cronograma; sem cronograma, exibe "sem cronograma" | v1 |
| Status: Planejamento, Em andamento, Pausado, Concluído e Cancelado, alterado pelo gestor do projeto | v1 |
| Projeto concluído ou cancelado fica somente leitura e sai da lista padrão (visível pelo filtro); o gestor pode reabrir | v1 |
| Excluir projeto | fora (só cancelar, para preservar o histórico) |
| Campos personalizados por empresa | fora |
| Orçamento e custos | fora |

### Consequências

- Bom, porque datas e progresso nunca divergem do cronograma.
- Bom, porque nenhum projeto é apagado e o histórico fica preservado.
- Ruim, porque um projeto sem cronograma não mostra datas nem progresso.
- Ruim, porque projetos encerrados exigem regra de somente leitura em todos os módulos.

### Confirmação

Testes cobrindo: código duplicado é recusado; datas e progresso refletem o cronograma; projeto concluído ou cancelado não aceita alterações e sai da lista padrão; reabertura pelo gestor devolve a edição; não existe caminho para excluir projeto.

## Prós e contras das opções

### A. Calculados pelo cronograma

- Bom, porque é sempre coerente.
- Ruim, porque depende de o cronograma existir.

### B. Digitados pelo gestor

- Bom, porque é flexível.
- Ruim, porque fica desatualizado com facilidade e diverge do cronograma.
