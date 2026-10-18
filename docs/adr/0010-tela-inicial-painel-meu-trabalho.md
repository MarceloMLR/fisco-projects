---
status: aceita (seções, cards de projetos e atalhos substituídos pelo ADR-0070)
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0010: Tela inicial como painel pessoal "Meu trabalho"

## Contexto e problema

O MVP não tinha uma tela inicial definida; a pessoa caía direto nos módulos. É a primeira tela após o login e define o que cada pessoa vê ao abrir o sistema todos os dias. O que ela deve mostrar?

## Fatores de decisão

- Levar cada pessoa à próxima ação sem precisar procurar.
- Atender perfis diferentes (quem atende chamados, quem toca tarefas, quem responde aceites, gestores).
- Esforço proporcional à v1, sem depender de dados acumulados.

## Opções consideradas

- A. Painel pessoal "Meu trabalho"
- B. Lista de projetos
- C. Painel gerencial com indicadores

## Resultado da decisão

Opção escolhida: **A. Painel pessoal "Meu trabalho"**, porque mostra o que pede ação de cada pessoa e serve a todos os perfis.

Escopo:

| Item | Classificação |
| --- | --- |
| Pendências para mim: chamados sob minha responsabilidade em aberto, chamados aguardando minha resposta e termos de aceite pendentes para mim | v1 (substituído pelo ADR-0070: fila única por urgência) |
| Minhas tarefas atrasadas ou que vencem nos próximos 7 dias | v1 (substituído pelo ADR-0070: na mesma fila) |
| Meus projetos em cards, com status, progresso e próximo marco | v1 (substituído pelo ADR-0070: lista com faixa de fases e atraso, mais Próximos marcos) |
| Atalhos "Abrir chamado" e "Novo projeto" (este só para quem pode criar projetos) | v1 (substituído pelo ADR-0070: sem atalhos na tela inicial) |
| Seções vazias com mensagem amigável | v1 |
| Indicadores gerenciais (chamados por status, projetos atrasados, prazos estourados) | depois |
| Atividade recente dos meus projetos | depois |
| Personalizar o painel (mover ou ocultar seções) | fora |

Se um módulo sair do escopo, a seção correspondente sai junto.

### Consequências

- Bom, porque cada pessoa vê primeiro o que depende dela.
- Bom, porque não depende de histórico de dados para ser útil.
- Ruim, porque a tela depende de vários módulos e precisa acompanhar as decisões de escopo deles.
- Ruim, porque gestores não têm visão consolidada na v1.

### Confirmação

Testes cobrindo: cada seção mostra apenas itens da pessoa logada; itens de projetos sem acesso não aparecem; o atalho "Novo projeto" só aparece para quem pode criar projetos (após o ADR-0070, não há atalhos na tela inicial); seções vazias exibem a mensagem.

## Prós e contras das opções

### A. Painel pessoal "Meu trabalho"

- Bom, porque leva direto à próxima ação.
- Ruim, porque acompanha o escopo de vários módulos.

### B. Lista de projetos

- Bom, porque é a mais simples.
- Ruim, porque quem trabalha na fila de chamados precisa de cliques extras.

### C. Painel gerencial com indicadores

- Bom, porque dá visão consolidada e ajuda em demonstrações.
- Ruim, porque exige mais trabalho e depende de dados acumulados.
