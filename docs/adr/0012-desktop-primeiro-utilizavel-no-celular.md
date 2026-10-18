---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0012: Desktop em primeiro lugar, com uso no celular para as tarefas do dia a dia

## Contexto e problema

O FiscoProjects será usado principalmente no computador, mas há usos reais no celular: responder a um chamado, dar um aceite, consultar um documento. A barra lateral é fixa e sempre expandida (ADR-0011), o que não cabe em telas pequenas. O quanto cada tela deve se adaptar a outros dispositivos?

## Fatores de decisão

- Atender os usos reais no celular.
- Esforço proporcional: o uso principal é no computador.
- Telas complexas, como o Gantt, não precisam de edição completa no celular.

## Opções consideradas

- A. Desktop em primeiro lugar, com o restante utilizável no celular
- B. Só desktop
- C. Pensado primeiro para o celular ou aplicativo instalável

## Resultado da decisão

Opção escolhida: **A. Desktop em primeiro lugar, com o restante utilizável no celular**, porque atende os usos reais no celular sem exigir edição completa de telas complexas em telas pequenas.

Escopo:

| Item | Classificação |
| --- | --- |
| Computador: layout completo com a barra lateral fixa (ADR-0011) | v1 |
| Celular e tablet em pé: layout adaptado; a barra lateral vira um menu aberto por botão (única exceção à barra fixa) | v1 |
| Painel, chamados, termos de aceite e documentos totalmente usáveis no celular | v1 |
| Gantt no celular: só visualização com rolagem; arrastar e redimensionar barras só no computador | v1 |
| Aplicativo instalável (PWA) ou nativo | fora |
| Notificações push no celular | fora (avisos por e-mail, a decidir no bloco de e-mail) |

### Consequências

- Bom, porque quem precisa responder a um chamado ou dar um aceite consegue pelo celular.
- Bom, porque o Gantt não precisa de interação completa em telas pequenas.
- Ruim, porque cada tela precisa ser desenhada e testada em dois tamanhos.

### Confirmação

O design system terá cada página em versão de computador e de celular. Testes de interface verificam, em largura de celular, o menu da barra lateral, o painel, a página do chamado e a resposta a um termo de aceite.

## Prós e contras das opções

### A. Desktop em primeiro lugar, utilizável no celular

- Bom, porque cobre os usos reais no celular.
- Ruim, porque exige layout e testes em dois tamanhos.

### B. Só desktop

- Bom, porque reduz o trabalho de layout e testes.
- Ruim, porque quem só precisa responder algo pelo celular fica sem opção.

### C. Celular primeiro ou aplicativo instalável

- Bom, porque dá a melhor experiência no celular.
- Ruim, porque é um esforço grande para um uso secundário.
