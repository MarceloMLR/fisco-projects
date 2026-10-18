---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0015: Cadastro de clientes, com cliente opcional no projeto

## Contexto e problema

O ideia.md não deixa claro para quem os projetos são feitos: os exemplos do MVP sugerem projetos internos (áreas da empresa), mas os acessos a portais vêm de um Excel "de todos os clientes", e a importação filtra por cliente. Uma empresa que adquira o código pode estar em qualquer um dos dois casos (ADR-0003). Os projetos devem ter cliente?

## Fatores de decisão

- Atender empresas com projetos internos e com projetos para clientes.
- Filtros e relatórios confiáveis por cliente.
- Sustentar a importação de acessos por cliente e o futuro acesso de externos.

## Opções consideradas

- A. Cadastro de clientes, com cliente opcional no projeto
- B. Campo de texto livre "Cliente ou área" no projeto
- C. Sem cliente

## Resultado da decisão

Opção escolhida: **A. Cadastro de clientes, com cliente opcional no projeto**, porque atende os dois perfis de empresa e mantém filtros confiáveis.

Escopo:

| Item | Classificação |
| --- | --- |
| Cadastro de clientes na Administração: nome (obrigatório), CNPJ (opcional, validado e único), logo (PNG ou JPG, até 1 MB) e observação | v1 |
| Campo "Cliente" no projeto, opcional (vazio em projetos internos) | v1 |
| Logo do cliente exibido na lista de projetos, nos cards do painel e no cabeçalho do projeto | v1 |
| Filtro por cliente na lista de projetos | v1 |
| Cliente com projetos não pode ser excluído, só inativado; cliente sem projetos pode ser excluído | v1 |
| Contatos do cliente (nome, e-mail, telefone) | depois (dados pessoais, ligados ao futuro acesso de externos) |

O local onde os logos são guardados segue a decisão de armazenamento de arquivos (bloco de documentos).

### Consequências

- Bom, porque o mesmo sistema serve a projetos internos e para clientes.
- Bom, porque filtros e importações por cliente usam um cadastro único.
- Ruim, porque acrescenta um cadastro e uma tela na Administração.

### Confirmação

Testes cobrindo: CNPJ inválido ou repetido é recusado; projeto sem cliente é aceito; cliente com projetos não pode ser excluído; logo acima do tamanho ou fora do formato é recusado.

## Prós e contras das opções

### A. Cadastro de clientes, cliente opcional

- Bom, porque atende os dois perfis de empresa.
- Bom, porque permite filtrar e relatar por cliente.
- Ruim, porque exige um cadastro a mais.

### B. Texto livre

- Bom, porque é o mais simples.
- Ruim, porque o mesmo cliente aparece com grafias diferentes.

### C. Sem cliente

- Bom, porque tem menos campos.
- Ruim, porque não atende empresas que fazem projetos para clientes.
