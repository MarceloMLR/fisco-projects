---
status: aceita (abertura sem responsável e sem fila, atalhos e visões substituídos pelo ADR-0068)
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0029: Número único na instalação, abertura e lista de chamados

## Contexto e problema

O MVP numera os chamados em sequência (#103), tem um formulário de abertura e uma lista com filtros rápidos. Falta definir como os chamados são numerados e fechar o escopo da abertura e da lista, considerando os destinos (ADR-0023), os prazos (ADR-0025) e os chamados em fornecedores (ADR-0028).

## Fatores de decisão

- Referência curta e fácil de citar.
- Encontrar rapidamente os chamados de cada pessoa e situação.
- Escopo enxuto na v1.

## Opções consideradas

- A. Número sequencial único na instalação (#103)
- B. Número por projeto, com o código do projeto (ERP-FIN-12)

## Resultado da decisão

Opção escolhida: **A. Número sequencial único na instalação**, porque é curto, fácil de citar e não depende do código do projeto.

Escopo:

| Item | Classificação |
| --- | --- |
| Número sequencial único na instalação (#103), exibido junto com o código do projeto | v1 |
| Abertura com projeto, destino, responsável opcional, prioridade, título, descrição com formatação básica e anexos | v1 (substituído pelo ADR-0068: projeto, time, prioridade, título, descrição e anexos, sem responsável) |
| Abrir chamado pela tela inicial, pela lista geral e pela aba Chamados do projeto (com o projeto já preenchido) | v1 (substituído pelo ADR-0068: só pela lista geral e pela aba Chamados do projeto) |
| Filtros rápidos (Todos, Abertos por mim, Atribuídos a mim e Com fornecedor), mais destino ou fila, status, prioridade e prazo vencido | v1 (no ADR-0068, mais "Sem responsável nos meus times" e "Que acompanho"; filtro por time em vez de destino ou fila) |
| Busca por número, título e descrição | v1 |
| Ordenação por prazo, prioridade, data de abertura e última atualização | v1 |
| Tipo de chamado (dúvida, erro, solicitação) | depois |
| Salvar filtros personalizados | depois |
| Exportar a lista para Excel | depois |
| Vincular chamados relacionados | depois |

### Consequências

- Bom, porque o número é curto e não muda se o código do projeto mudar.
- Bom, porque os filtros cobrem as situações do dia a dia (meus chamados, vencidos, com fornecedor).
- Ruim, porque o número sozinho não identifica o projeto; as telas mostram o código do projeto ao lado.

### Confirmação

Testes cobrindo: números sequenciais sem repetição, mesmo com aberturas simultâneas; abertura pela aba do projeto já traz o projeto preenchido; cada filtro retorna só os chamados correspondentes e dentro do acesso da pessoa; a busca encontra por número, título e descrição.

## Prós e contras das opções

### A. Número único na instalação

- Bom, porque é curto e estável.
- Ruim, porque não mostra o projeto no número.

### B. Número por projeto

- Bom, porque identifica o projeto no próprio número.
- Ruim, porque a referência é mais longa e depende do código do projeto.
