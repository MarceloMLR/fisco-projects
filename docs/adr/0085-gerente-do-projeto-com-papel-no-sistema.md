---
status: proposta
date: 2026-10-19
decision-makers: Marcelo Ribeiro
---

# ADR-0085: Gerente do projeto sempre com o papel no sistema Gerente de projeto ou Administrador

Complementa o ADR-0042 e estende a regra do ADR-0072 da criação para a aba Membros.

## Contexto e problema

Pelo ADR-0072, o gerente escolhido na criação do projeto precisa ter o papel no sistema Gerente de projeto ou Administrador. Depois da criação, as pessoas do projeto são geridas na aba Membros (ADR-0042), que não diz quem pode receber o papel Gerente de projeto num projeto. O ADR-0040 separa o papel no sistema (quem cria projetos) do papel no projeto (quem gerencia um projeto). Na aba Membros, qualquer pessoa pode virar gerente de um projeto?

## Fatores de decisão

- A mesma regra para o gerente na criação e depois dela.
- Quem gerencia projetos é escolhido pela Administração, que dá o papel no sistema.
- O projeto nunca fica sem gerente (ADR-0042).

## Opções consideradas

- A. Mesma regra da criação: só quem tem o papel no sistema Gerente de projeto ou Administrador recebe o papel Gerente de projeto num projeto
- B. Qualquer pessoa ativa: o papel no projeto independe do papel no sistema

## Resultado da decisão

Opção escolhida: **A**, por escolha de Marcelo, porque mantém uma regra só para o gerente e deixa com a Administração a decisão de quem gerencia projetos.

Regras:

- Incluir uma pessoa como gerente ou trocar o papel de alguém para gerente só aceita quem tem, naquele momento, o papel no sistema Gerente de projeto ou Administrador e está ativo.
- Uma pessoa incluída pelo diretório, que ainda não entrou, tem o papel Usuário (ADR-0006) e não pode ser incluída como gerente.
- Trocar depois o papel no sistema de um gerente para Usuário não tira a pessoa dos projetos nem muda o papel dela nos projetos.
- As regras do ADR-0042 continuam: o líder técnico não inclui, não remove nem promove gerentes, e o projeto sempre tem ao menos um gerente.

### Consequências

- Bom, porque a criação e a aba Membros seguem a mesma regra.
- Bom, porque a Administração controla quem pode gerenciar projetos.
- Ruim, porque, para entregar um projeto a alguém com o papel Usuário, o administrador precisa antes dar a essa pessoa o papel Gerente de projeto.

### Confirmação

Testes cobrindo: incluir como gerente uma pessoa com o papel Gerente de projeto ou Administrador; recusar como gerente uma pessoa com o papel Usuário e uma pessoa incluída pelo diretório; trocar o papel de um desenvolvedor para gerente só com o papel no sistema; o gerente que perde o papel no sistema continua gerente nos projetos.

## Prós e contras das opções

### A. Mesma regra da criação

- Bom, porque é uma regra só.
- Ruim, porque exige um passo na Administração para quem tem o papel Usuário.

### B. Qualquer pessoa ativa

- Bom, porque separa por completo o papel no sistema do papel no projeto (ADR-0040).
- Ruim, porque a criação e a aba Membros teriam regras diferentes para o mesmo papel.
