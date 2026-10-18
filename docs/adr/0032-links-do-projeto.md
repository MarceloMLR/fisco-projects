---
status: aceita
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0032: Links do projeto com categorias de uma lista configurável

## Contexto e problema

O MVP guarda, em cada projeto, os endereços que o time usa (sistemas, ambientes, repositórios, painéis), separados por categoria, com opções de copiar e editar. Os links são dados do FiscoProjects, guardados no banco da aplicação, e não arquivos no SharePoint (ADR-0030). De onde vêm as categorias e o que o módulo tem na v1?

## Fatores de decisão

- Categorias padronizadas entre os projetos.
- Nada específico de uma empresa fixo no código (ADR-0003).
- Módulo simples.

## Opções consideradas

- A. Categorias de uma lista configurável na Administração
- B. Categoria em texto livre
- C. Sem categoria, só lista com busca

## Resultado da decisão

Opção escolhida: **A. Categorias de uma lista configurável na Administração**, porque padroniza as categorias e deixa cada empresa ajustar a lista.

Escopo:

| Item | Classificação |
| --- | --- |
| Links do projeto com título, endereço, categoria e descrição, guardados no banco do FiscoProjects | v1 |
| Abrir em nova aba e copiar o endereço | v1 |
| Categorias de uma lista configurável na Administração (padrão: Sistemas, Ambientes, Repositórios, Painéis e Outros) | v1 |
| Busca por título e endereço | v1 |
| Links que já vêm com o modelo de projeto | depois |
| Verificar automaticamente se o link está no ar | fora |

Quem cadastra e edita os links será definido na matriz de permissões.

### Consequências

- Bom, porque os links ficam organizados da mesma forma em todos os projetos.
- Bom, porque cada empresa usa as próprias categorias.
- Ruim, porque acrescenta mais uma lista na Administração.

### Confirmação

Testes cobrindo: link sem título ou com endereço inválido é recusado; categoria inativa não aparece para novos links; a busca encontra por título e endereço; links só aparecem para quem tem acesso ao projeto.

## Prós e contras das opções

### A. Lista configurável

- Bom, porque padroniza e é configurável.
- Ruim, porque exige uma lista a mais na Administração.

### B. Texto livre

- Bom, porque é flexível.
- Ruim, porque a mesma categoria aparece com grafias diferentes.

### C. Sem categoria

- Bom, porque é o mais simples.
- Ruim, porque projetos com muitos links ficam difíceis de percorrer.
