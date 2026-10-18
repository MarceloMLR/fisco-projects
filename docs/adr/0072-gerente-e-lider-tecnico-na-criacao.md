---
status: aceita
date: 2026-10-17
decision-makers: Marcelo Ribeiro
---

# ADR-0072: Gerente e líder técnico definidos na criação do projeto e mostrados no cabeçalho

Substitui, no ADR-0040, a regra "quem cria vira gerente do projeto criado", que passa a valer como padrão, com a opção de escolher outro gerente na criação.

## Contexto e problema

Pelo ADR-0040, quem cria o projeto vira o gerente, e o líder técnico só entra depois, pela tela de Membros (ADR-0042). Com o ADR-0068, o líder técnico passa a distribuir os chamados de Desenvolvimento, e a tela de Novo projeto aprovada na revisão do design já pede o gerente e o líder técnico. Quem é definido na criação do projeto?

## Fatores de decisão

- O projeto nasce com quem responde por ele e com quem distribui os chamados técnicos.
- Um PMO ou um administrador pode criar o projeto para outro gerente.
- Regras de membros do ADR-0042 preservadas depois da criação.

## Opções consideradas

- A. Manter o ADR-0040: quem cria é o gerente; o líder técnico entra depois, em Membros
- B. Gerente e líder técnico escolhidos na criação, mostrados no cabeçalho do projeto

## Resultado da decisão

Opção escolhida: **B**, aprovada por Marcelo.

Regras:

- Gerente: obrigatório, já preenchido com quem cria. Pode ser trocado por outra pessoa com o papel de sistema "Gerente de projeto" (ou administrador). Nesse caso, quem criou não entra automaticamente no projeto.
- Líder técnico: opcional, escolhido entre as pessoas da empresa (ADR-0005); entra no projeto com o papel Líder técnico e no time de Desenvolvimento (ADR-0040, ADR-0068).
- O cabeçalho do projeto mostra o gerente e o líder técnico em todas as abas, ao lado do código, do cliente e das datas.
- Depois da criação, incluir, trocar ou remover gerente e líder técnico segue o ADR-0042, em Membros (o projeto nunca fica sem gerente).
- A criação fica registrada na auditoria (ADR-0039), com quem criou e quem foi definido como gerente.

### Consequências

- Bom, porque o projeto já nasce com quem distribui os chamados de Desenvolvimento.
- Bom, porque permite criar projetos para outros gerentes sem passos extras.
- Ruim, porque quem cria para outra pessoa perde o acesso ao projeto, se não for incluído depois; o formulário deixa isso visível ao trocar o gerente.

### Confirmação

Testes cobrindo: o gerente vem preenchido com quem cria; trocar o gerente na criação só aceita pessoas com o papel de sistema Gerente de projeto ou administrador; com outro gerente, quem criou não vira membro; o líder técnico escolhido entra com o papel Líder técnico e no time de Desenvolvimento; o cabeçalho mostra gerente e líder técnico; a auditoria registra quem criou e quem é o gerente.

## Prós e contras das opções

### A. Manter o ADR-0040

- Bom, porque é o mais simples.
- Ruim, porque o projeto nasce sem líder técnico e só quem cria pode ser o gerente.

### B. Gerente e líder técnico na criação

- Bom, porque o projeto começa completo e permite criar para outro gerente.
- Ruim, porque acrescenta dois campos e uma regra de acesso na criação.
