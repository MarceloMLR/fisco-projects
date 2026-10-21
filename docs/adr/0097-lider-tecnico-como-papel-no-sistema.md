---
status: aceita
date: 2026-10-20
decision-makers: Marcelo Ribeiro
---

# ADR-0097: Líder técnico também como papel no sistema

Substitui em parte o ADR-0040 (os papéis no sistema) e o ADR-0072 (o líder técnico do diretório na criação), e estende ao líder técnico a regra do ADR-0085.

## Contexto e problema

Pelo ADR-0040, os papéis no sistema são Administrador, Gerente de projeto e Usuário, e o Líder técnico existe só em cada projeto. Por isso, quem lidera tecnicamente os projetos aparece como "Usuário" na tela Usuários e em Meu perfil, e qualquer pessoa ativa pode ser escolhida como líder técnico, inclusive alguém do diretório que nunca entrou (ADR-0072). Marcelo quer o papel Líder técnico também no sistema, e que só quem o tem possa liderar tecnicamente um projeto. O que esse papel permite, e como fica a relação com o papel no projeto?

## Fatores de decisão

- Ver no sistema quem é líder técnico.
- A Administração decide quem pode liderar tecnicamente, como já decide quem gerencia projetos (ADR-0085).
- A mesma regra na criação do projeto e na aba Membros.
- Nada muda para quem já está nos projetos.

## Opções consideradas

- A. Papel no sistema que identifica, é sugerido nas telas e é exigido para ser líder técnico num projeto, como o gerente pelo ADR-0085
- B. Papel no sistema só para identificar e sugerir, sem restringir o papel no projeto
- C. Papel no sistema que também cria projetos, como o Gerente de projeto

## Resultado da decisão

Opção escolhida: **A**, por escolha de Marcelo, porque a Administração passa a decidir quem lidera tecnicamente, com a mesma regra já usada para o gerente.

Regras:

- Os papéis no sistema passam a ser Administrador, Gerente de projeto, Líder técnico e Usuário. O Líder técnico no sistema não cria projetos nem dá outra permissão além de poder ser líder técnico num projeto.
- Ser escolhido líder técnico na criação do projeto, ser incluído como líder técnico ou ter o papel trocado para líder técnico só aceita quem está ativo e tem, naquele momento, o papel no sistema Líder técnico ou Administrador; os outros recebem 400.
- Uma pessoa do diretório que ainda não entrou tem o papel Usuário (ADR-0006) e não pode ser escolhida como líder técnico, nem na criação: a opção do ADR-0072 de criar o projeto com um líder técnico do diretório deixa de existir.
- As telas sugerem o papel: na criação, o campo do líder técnico busca só quem pode ser líder técnico; em "Incluir pessoa", quem tem o papel no sistema Líder técnico já vem com o papel Líder técnico, e o papel fica indisponível para quem não pode; o mesmo vale para trocar o papel na aba Membros.
- Como no ADR-0085, trocar depois o papel no sistema não tira a pessoa dos projetos nem muda o papel dela nos projetos.
- Nas instalações que já têm líderes técnicos com o papel Usuário, o administrador dá o papel na tela Usuários; nada é trocado sozinho. A demonstração já carrega o líder técnico com o papel.

### Consequências

- Bom, porque a tela Usuários e Meu perfil mostram quem é líder técnico.
- Bom, porque gerente e líder técnico seguem a mesma regra, decidida pela Administração.
- Ruim, porque, para fazer de alguém líder técnico num projeto, o administrador precisa antes dar o papel no sistema, inclusive a quem ainda não entrou, que primeiro precisa entrar ou ser incluído com outro papel.

### Confirmação

Testes cobrindo: o papel Líder técnico na tela Usuários, no filtro e no menu; a busca de pessoas que só traz quem pode ser líder técnico; criar o projeto com um líder técnico com o papel no sistema e recusar um Usuário ou uma pessoa do diretório; incluir e trocar o papel para líder técnico só com o papel no sistema ou de administrador; o líder técnico que perde o papel no sistema continua líder técnico nos projetos; a tela sugerindo o papel e deixando-o indisponível.

## Prós e contras das opções

### A. Identifica, sugere e restringe

- Bom, porque a Administração controla quem lidera tecnicamente.
- Ruim, porque exige um passo na Administração antes de escolher alguém como líder técnico.

### B. Só identifica e sugere

- Bom, porque não muda quem pode ser escolhido.
- Ruim, porque o papel no sistema e o papel no projeto podem não combinar.

### C. Também cria projetos

- Bom, porque o líder técnico abriria projetos sem depender do gerente.
- Ruim, porque mistura o papel com o do Gerente de projeto.
