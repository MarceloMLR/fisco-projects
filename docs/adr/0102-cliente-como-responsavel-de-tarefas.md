---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0102: O cliente do projeto como responsável de tarefas do cronograma

Complementa o ADR-0020 (um responsável por tarefa), o ADR-0043 (quem é responsável) e o ADR-0007 (só pessoas da empresa entram).

## Contexto e problema

O responsável de uma tarefa é uma pessoa do projeto que não é leitora (ADR-0020, ADR-0043). Muitas etapas, porém, são do cliente: os testes da equipe dele, a validação de um relatório, o envio de um arquivo. Hoje elas ficam sem responsável ou em nome de alguém da equipe, o que confunde quem lê o cronograma. Marcelo perguntou se o cliente pode ser responsável. O cliente não entra no sistema (ADR-0007): quem atualiza o progresso dessas tarefas, quem recebe os avisos, e como o cliente aparece?

## Fatores de decisão

- O cronograma diz de quem é cada etapa, inclusive as do cliente.
- Nenhum acesso novo: o cliente continua fora do sistema (ADR-0007).
- Simples de escolher e de ler.

## Opções consideradas

- A. "O cliente do projeto" como uma opção do responsável, sem pessoa
- B. Um responsável em texto livre (um nome qualquer)
- C. Contatos do cliente cadastrados, escolhidos como responsáveis

## Resultado da decisão

Opção escolhida: **A**, porque responde ao pedido com o cadastro que já existe (o cliente do projeto) e sem acesso novo; os contatos do cliente (C) ficam para depois, se a empresa precisar dizer quem, no cliente, é o responsável.

Regras:

- Uma tarefa pode ter como responsável uma pessoa do projeto (como hoje) ou o cliente do projeto, nunca os dois; um marco continua sem responsável. Só um projeto com cliente oferece a opção.
- O progresso de uma tarefa do cliente é atualizado por quem edita o cronograma (o administrador e o gerente), como uma tarefa sem responsável; o cliente não recebe avisos nem aparece em Meu trabalho, porque não entra no sistema.
- As telas mostram o cliente com o logo e o nome, com "(cliente)"; se o projeto perder o cliente, a tarefa mostra "O cliente".
- A exportação para o MS Project traz o cliente como um recurso, com as tarefas dele.
- O desfazer e o refazer, a auditoria ("Responsável: o cliente") e a cópia para a tarefa seguinte seguem como os outros campos.

### Consequências

- Bom, porque o cronograma mostra as etapas do cliente sem inventar um responsável da equipe.
- Bom, porque não abre acesso ao cliente.
- Ruim, porque não diz quem, no cliente, cuida da tarefa.

### Confirmação

Testes cobrindo: tarefa com o cliente como responsável num projeto com cliente; recusa sem cliente, com uma pessoa ao mesmo tempo e num marco; progresso só por quem edita; desfazer; auditoria; o recurso do cliente na exportação para o MS Project; a opção na tela e o nome do cliente na lista.

## Prós e contras das opções

### A. O cliente do projeto

- Bom, porque usa o cliente já cadastrado e não pede nada novo.
- Ruim, porque fala do cliente como um todo.

### B. Texto livre

- Bom, porque aceita qualquer nome.
- Ruim, porque o texto não se liga a nada e erra fácil.

### C. Contatos do cliente

- Bom, porque diz a pessoa exata no cliente.
- Ruim, porque pede um cadastro novo de contatos, com dados pessoais de quem não usa o sistema (ADR-0058).
