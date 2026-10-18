---
status: aceita
date: 2026-10-17
decision-makers: Marcelo Ribeiro
---

# ADR-0070: Meu trabalho como fila única por urgência, com a saúde dos projetos e os próximos marcos

Substitui, no ADR-0010, as seções separadas "Pendências para mim" e "Minhas tarefas", os cards de "Meus projetos" e os atalhos "Abrir chamado" e "Novo projeto".

## Contexto e problema

O ADR-0010 definiu a tela inicial como o painel pessoal "Meu trabalho", com seções por tipo de item (pendências, tarefas, projetos) e atalhos para abrir chamado e criar projeto. Na revisão das telas, Marcelo achou os resumos bagunçados e sem foco estratégico: o produto é para gerentes de projeto e desenvolvedores que trabalham em um ou vários projetos ao mesmo tempo. Decidiu também que "Abrir chamado" fica só nas telas de chamados (ADR-0068). Como organizar o Meu trabalho?

## Fatores de decisão

- Responder de relance "o que depende de mim agora".
- Dar ao gerente a saúde dos seus projetos sem abrir cada um.
- Uma ação direta por item, sem precisar procurar.

## Opções consideradas

- A. Seções por tipo de item, como no ADR-0010
- B. Fila única ordenada por urgência, mais a saúde dos projetos e os próximos marcos

## Resultado da decisão

Opção escolhida: **B. Fila única por urgência**, aprovada por Marcelo na revisão do design.

Escopo:

| Item | Classificação |
| --- | --- |
| Fila única com chamados, tarefas e termos de aceite da pessoa, agrupada em Atrasado, Aguardando você, Hoje e o próximo dia útil, e Próximos 7 dias | v1 |
| Uma ação direta por linha (Concluir, Abrir, Responder, Iniciar, Atualizar; Atribuir só para gerente e líder técnico) | v1 |
| Visões "Para mim", "Abertos por mim" e "Que acompanho" | v1 |
| Meus projetos com a faixa de fases (ADR-0071), a porcentagem concluída e o atraso em dias úteis | v1 |
| Próximos marcos dos meus projetos, com a data em destaque e quantos dias úteis faltam | v1 |
| Seções vazias com mensagem amigável | v1 (mantém o ADR-0010) |
| Atalhos "Abrir chamado" e "Novo projeto" | fora ("Novo projeto" fica na página Projetos e "Abrir chamado" nas telas de chamados, ADR-0068) |
| Indicadores gerenciais (chamados por status, projetos atrasados, prazos estourados) | depois (mantém o ADR-0010) |
| Atividade recente dos meus projetos | depois (mantém o ADR-0010) |
| Personalizar o painel | fora (mantém o ADR-0010) |

### Consequências

- Bom, porque a pessoa vê primeiro o que está atrasado ou esperando por ela, seja chamado, tarefa ou aceite.
- Bom, porque o gerente acompanha a saúde de todos os seus projetos na tela inicial.
- Ruim, porque a ordenação por urgência depende dos prazos em dias úteis de cada módulo (ADR-0018, ADR-0025) e precisa de testes cuidadosos.

### Confirmação

Testes cobrindo: cada item cai no grupo certo conforme o prazo em dias úteis (incluindo fins de semana e feriados); "Aguardando você" reúne chamados aguardando a resposta da pessoa e aceites pendentes para ela; só itens da pessoa logada e de projetos a que ela tem acesso aparecem; "Atribuir" só aparece para gerente e líder técnico; a tela inicial não tem "Abrir chamado" nem "Novo projeto".

## Prós e contras das opções

### A. Seções por tipo de item

- Bom, porque separa claramente o que é chamado, tarefa e aceite.
- Ruim, porque obriga a percorrer várias seções para saber o que é mais urgente.

### B. Fila única por urgência

- Bom, porque mostra a próxima ação de relance.
- Ruim, porque mistura tipos de item na mesma lista; o ícone e o código do projeto em cada linha compensam.
