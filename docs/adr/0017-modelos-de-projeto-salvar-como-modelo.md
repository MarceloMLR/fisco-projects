---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0017: Modelos de projeto criados a partir de um cronograma ("Salvar como modelo")

## Contexto e problema

O ideia.md prevê que, ao criar um projeto, o cronograma seja gerado a partir de um modelo. O MVP tinha três modelos fixos no código (Implantação de sistema, Infraestrutura e Em branco), o que contraria o ADR-0003: cada empresa precisa ter os próprios modelos. Como os modelos são criados e editados?

## Fatores de decisão

- Nenhum modelo fixo no código (ADR-0003).
- Reaproveitar o editor de cronograma em vez de construir um segundo editor.
- Pouco esforço para um desenvolvedor sozinho.

## Opções consideradas

- A. Editor próprio de modelos na Administração
- B. "Salvar como modelo" a partir do cronograma de um projeto
- C. Modelos fixos no código, como no MVP

## Resultado da decisão

Opção escolhida: **B. "Salvar como modelo" a partir do cronograma de um projeto**, porque reaproveita o editor do cronograma, dispensa um segundo editor e deixa cada empresa montar os próprios modelos.

Escopo:

| Item | Classificação |
| --- | --- |
| "Salvar como modelo" a partir do cronograma de um projeto: fases, tarefas, marcos, dependências e durações (sem datas nem responsáveis) | v1 |
| Lista de modelos na Administração: renomear, descrever, inativar e excluir | v1 |
| Criar projeto escolhendo um modelo, ou "Em branco", e a data de início: o cronograma é gerado na hora | v1 |
| Modelos de exemplo, fictícios, só nos dados de demonstração | v1 |
| Editor próprio de modelos (opção A) | depois |
| Modelos que também trazem pastas de documentos, links e outros itens | a decidir nos blocos desses módulos |

### Consequências

- Bom, porque não há um segundo editor para construir e testar.
- Bom, porque cada empresa monta os modelos a partir dos próprios projetos.
- Ruim, porque mudar a estrutura de um modelo exige criar um projeto a partir dele, ajustar o cronograma e salvar de novo.
- Neutro, porque as durações seguem a regra de contagem de dias do cronograma, decidida em ADR próprio.
- Neutro, porque quem pode salvar e gerenciar modelos será definido na matriz de permissões (pendência do discovery).

### Confirmação

Testes cobrindo: salvar como modelo copia fases, tarefas, marcos, dependências e durações, sem datas nem responsáveis; criar projeto a partir de um modelo com data de início gera o cronograma com datas coerentes; modelo inativo não aparece na criação de projeto; "Em branco" cria o projeto sem cronograma. Revisão de código: nenhum modelo fixo no código; os exemplos existem apenas nos dados de demonstração.

## Prós e contras das opções

### A. Editor próprio de modelos

- Bom, porque separa modelos de projetos.
- Ruim, porque é praticamente um segundo editor de cronograma.

### B. "Salvar como modelo"

- Bom, porque reaproveita o editor do cronograma.
- Ruim, porque editar a estrutura de um modelo é indireto.

### C. Modelos fixos no código

- Bom, porque é o mais simples.
- Ruim, porque viola o ADR-0003.

## Mais informações

- Os modelos do MVP (Implantação de sistema, Infraestrutura) podem virar exemplos fictícios nos dados de demonstração (ADR-0002).
