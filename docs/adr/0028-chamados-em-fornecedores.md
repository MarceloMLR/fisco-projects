---
status: aceita
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0028: Chamados em fornecedores registrados manualmente, com destaque para o que está parado

## Contexto e problema

No MVP, quando o problema depende de um fornecedor, quem atende registra dentro do nosso chamado o chamado aberto no fornecedor (número, data de abertura, status, link e observação). Os dois correm em paralelo: o nosso segue o fluxo normal, e o do fornecedor aparece na trilha de etapas, na linha do tempo, na lista e no filtro "Com fornecedor". Há mais de um fornecedor com o mesmo padrão de registro (por exemplo, Avalara e oobj), e um mesmo chamado pode ter chamados abertos em fornecedores diferentes. Pelos ADR-0002 e ADR-0003, nenhum fornecedor fica fixo no código. Como o status do lado do fornecedor se mantém atualizado?

## Fatores de decisão

- Funcionar com qualquer fornecedor, cadastrado pela empresa.
- Evitar informação desatualizada sobre o andamento no fornecedor.
- Esforço proporcional à v1.

## Opções consideradas

- A. Registro manual, como no MVP
- B. Registro manual, com destaque para chamado de fornecedor sem atualização
- C. Integração automática com o sistema de cada fornecedor (API ou leitura de e-mails)

## Resultado da decisão

Opção escolhida: **B. Registro manual, com destaque para o que está parado**, porque funciona com qualquer fornecedor e reduz a informação desatualizada sem integrações específicas.

Escopo:

| Item | Classificação |
| --- | --- |
| Cadastro de fornecedores na Administração: nome, endereço do portal de suporte (opcional) e ativo ou inativo | v1 |
| Registrar um ou mais chamados de fornecedor dentro do nosso chamado, inclusive de fornecedores diferentes: fornecedor, número, data de abertura, status, link e observação | v1 |
| Status do lado do fornecedor: Aberto, Em análise, Aguardando nosso retorno, Resolvido e Fechado, atualizado com anotação | v1 |
| Chamado do fornecedor na trilha de etapas (com o nome do fornecedor), na linha do tempo, na lista e no filtro "Com fornecedor" | v1 |
| Destaque para chamado de fornecedor sem atualização há 5 dias úteis (valor configurável) | v1 |
| O nosso prazo continua correndo enquanto o fornecedor analisa | v1 |
| Pausar o nosso prazo enquanto aguarda o fornecedor (exigiria o status "Aguardando fornecedor") | depois |
| Integração automática com o sistema do fornecedor | fora |

Os dados de demonstração usam fornecedores fictícios (ADR-0002).

### Consequências

- Bom, porque qualquer fornecedor é atendido pelo mesmo registro.
- Bom, porque o destaque aponta chamados de fornecedor esquecidos.
- Ruim, porque o status depende de alguém atualizar manualmente.
- Ruim, porque quem atende pode ser cobrado pelo prazo enquanto o fornecedor analisa.

### Confirmação

Testes cobrindo: um chamado aceita registros de fornecedores diferentes; cada atualização de status gera evento na linha do tempo; o filtro "Com fornecedor" mostra só chamados com registro de fornecedor; o destaque aparece após o prazo configurado sem atualização, em dias úteis; fornecedor inativo não aparece para novos registros. Revisão de código: nenhum nome de fornecedor fixo no código.

## Prós e contras das opções

### A. Registro manual

- Bom, porque é simples e genérico.
- Ruim, porque o status fica desatualizado com facilidade.

### B. Registro manual com destaque

- Bom, porque mantém a simplicidade e reduz informação velha.
- Ruim, porque acrescenta uma regra de tempo sem atualização.

### C. Integração automática

- Bom, porque o status estaria sempre atualizado.
- Ruim, porque cada fornecedor funciona de um jeito e muitos não oferecem API de chamados.
- Ruim, porque o esforço seria grande e específico por fornecedor.
