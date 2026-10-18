---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0046: Administração só do administrador, com cadastro de cliente na criação do projeto

## Contexto e problema

Última parte da matriz de permissões. A Administração reúne usuários, auditoria, configurações da instalação e cadastros: clientes, fornecedores, filas e seus atendentes, feriados, dias de trabalho e pontos facultativos, categorias de links, pastas padrão, prazos por prioridade e a lista de modelos de projeto. Pelo ADR-0011, ela é só do administrador. O atrito mais comum seria criar um projeto para um cliente ainda não cadastrado. Quem cuida da Administração?

## Fatores de decisão

- Controle centralizado da configuração e dos cadastros.
- Não travar a criação de projetos por falta de cadastro de cliente.
- Evitar papéis extras sem demanda.

## Opções consideradas

- A. Tudo só do administrador, com exceção: o gerente de projeto cadastra um cliente novo na criação do projeto
- B. Tudo só do administrador, sem exceção
- C. Papel intermediário que cuida só dos cadastros

## Resultado da decisão

Opção escolhida: **A. Tudo só do administrador, com a exceção do cliente novo**, porque tira o atrito mais comum e mantém o restante centralizado.

Regras:

- Usuários, auditoria, configurações e todos os cadastros da Administração: só o administrador.
- O gerente de projeto (papel no sistema) pode cadastrar um cliente novo direto no formulário de criação do projeto, sem acessar a Administração; editar ou inativar clientes existentes continua com o administrador.
- O CNPJ único (ADR-0015) evita clientes duplicados.

Escopo:

| Item | Classificação |
| --- | --- |
| Administração só do administrador | v1 |
| Cadastro de cliente novo pelo gerente de projeto, na criação do projeto | v1 |
| Papel intermediário só para cadastros | depois |

### Consequências

- Bom, porque a configuração do sistema fica sob controle de um papel.
- Bom, porque projetos para clientes novos não dependem do administrador.
- Ruim, porque mais pessoas cadastram clientes, com risco de cadastros incompletos.

### Confirmação

Testes cobrindo: só o administrador acessa a Administração; o gerente de projeto cadastra um cliente na criação do projeto, mas não edita nem inativa clientes existentes; CNPJ repetido é recusado também nesse caminho.

## Prós e contras das opções

### A. Só administrador, com exceção do cliente

- Bom, porque equilibra controle e fluidez.
- Ruim, porque abre o cadastro de clientes a mais pessoas.

### B. Só administrador, sem exceção

- Bom, porque dá controle total.
- Ruim, porque cada cliente novo depende do administrador.

### C. Papel intermediário

- Bom, porque tira carga do administrador.
- Ruim, porque acrescenta um papel sem demanda clara na v1.
