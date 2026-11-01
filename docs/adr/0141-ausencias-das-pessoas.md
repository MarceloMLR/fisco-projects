---
status: aceita
date: 2026-11-01
decision-makers: Marcelo Ribeiro
---

# ADR-0141: Ausências das pessoas, cadastradas pela gestão, só com as datas

Completa o ADR-0018 (calendário único da instalação), que trata dos dias de trabalho de todos, não de cada pessoa, e o ADR-0058 (dados pessoais).

## Contexto e problema

As férias e as folgas da equipe ficam fora do FiscoProjects: a gerente monta o cronograma e só descobre que a tarefa da Carla cai nas férias dela quando a tarefa atrasa. O calendário da instalação (ADR-0018) conta os feriados de todos, não as ausências de cada um. Marcelo pediu, no grupo 15 (GanttB-4), as ausências de cada pessoa com o aviso da tarefa que cai nelas, base também da carga da equipe (GanttB-5). Ausência é dado pessoal (ADR-0058): quem cadastra, o que se guarda, quem vê e por quanto tempo?

## Fatores de decisão

- Ver, no cronograma, a tarefa cujo responsável está ausente durante ela.
- Guardar o mínimo de dado pessoal, pelo menor tempo útil (LGPD, ADR-0058).
- Quem planeja os projetos cadastra; quem trabalha neles sabe quem está fora.
- Não mudar as datas do cronograma sozinho: o servidor é a fonte da verdade das datas, mas a decisão de replanejar é da gestão (ADR-0019, ADR-0051).

## Opções consideradas

Quem cadastra:

- A1. O administrador, para qualquer pessoa, e o gerente, para as pessoas dos projetos que gerencia.
- A2. Cada pessoa cadastra as próprias ausências.
- A3. Só o administrador.

O que se guarda:

- B1. Só o início e o fim.
- B2. As datas e o motivo (férias, licença, folga).

Efeito no cronograma:

- C1. Só o aviso na tarefa; as datas não mudam.
- C2. A ausência conta como dia não útil da pessoa e empurra a tarefa.

## Resultado da decisão

Opções escolhidas: **A1**, **B1** e **C1**. A1 por decisão de Marcelo em 01/11/2026 ("gerente e administrador"). B1 porque o motivo de uma ausência (uma licença médica, por exemplo) é dado sensível e não muda o planejamento: as datas bastam. C1 porque replanejar continua sendo decisão da gestão, que agora tem o aviso e o "Mover o cronograma" (ADR-0140).

Regras:

- **Ausência.** O início e o fim de um período em que a pessoa não trabalha, de um dia a 366 dias corridos, sem se sobrepor a outra ausência da mesma pessoa. Sem motivo, sem observação.
- **Quem cadastra e apaga.** Pela aba Membros de um projeto em aberto: o administrador (em qualquer projeto, então para qualquer pessoa de um projeto) e o gerente do projeto, para as pessoas dele (`ProjectPermissions.CanManagePeople`, ADR-0043). A ausência não se edita: apaga-se e cadastra-se de novo.
- **Quem vê.** As pessoas dos mesmos projetos veem que a pessoa está ausente e quando: no detalhe da pessoa em Membros, na lista de Membros ("Ausente até 14/11") e no aviso do cronograma.
- **Aviso no cronograma.** A tarefa em aberto com datas próprias cujo responsável (uma pessoa) tem ausência durante ela traz os períodos que se cruzam com ela; a Lista e o detalhe mostram o aviso. As datas da tarefa não mudam.
- **Retenção.** A ausência é apagada sozinha um ano depois do fim, numa limpeza em segundo plano, e na anonimização da pessoa (ADR-0110).
- **Auditoria.** A inclusão e a exclusão vão para a auditoria pelo nome da pessoa e de quem fez, sem as datas, que ficam como segredo (`IsAuditSecret`): a auditoria, guardada por anos (ADR-0058), não estende o prazo das datas.

Fora:

| Item | Classificação |
| --- | --- |
| Motivo, tipo ou observação da ausência | fora (dado sensível sem uso no planejamento) |
| A pessoa cadastrando a própria ausência | depois, se a equipe pedir |
| A ausência empurrando as tarefas | fora (decisão da gestão, ADR-0140) |
| Aviso por e-mail ou no sino quando a ausência cai numa tarefa | depois |
| Meio dia de ausência | fora |

### Consequências

- Bom, porque a gestão vê, no cronograma, a tarefa que cai nas férias do responsável.
- Bom, porque o dado pessoal guardado é o mínimo e some sozinho depois de um ano.
- Ruim, porque a pessoa depende da gestão para registrar a própria ausência.
- Ruim, porque a auditoria não mostra as datas de uma ausência apagada.

### Confirmação

Testes cobrindo: a ausência incluída e apagada pelo administrador e pelo gerente, e recusada (403) ao líder técnico e às outras pessoas do projeto, e 404 para quem não está nele; as datas inválidas e a sobreposição recusadas; a ausência na lista e no detalhe de Membros; o aviso na tarefa em aberto cujo responsável está ausente durante ela, e nenhum aviso na concluída; a auditoria sem as datas; a limpeza um ano depois do fim; e a anonimização apagando as ausências. A matriz de permissões das ausências é coberta por inteiro (ADR-0056).

## Prós e contras das opções

### A1. O administrador e o gerente

- Bom, porque quem planeja cadastra, como Marcelo decidiu.
- Ruim, porque a pessoa pede à gestão.

### A2. Cada pessoa

- Bom, porque quem sai é quem sabe.
- Ruim, porque a gestão não controla o que muda o planejamento.

### A3. Só o administrador

- Bom, porque concentra um dado pessoal.
- Ruim, porque a gerente, que monta o cronograma, não registra.

### B1. Só as datas

- Bom, porque não guarda dado sensível.
- Ruim, porque não diz se é férias ou licença.

### B2. As datas e o motivo

- Bom, porque diferencia férias de licença.
- Ruim, porque guarda dado sensível (saúde) sem uso no planejamento.

### C1. Só o aviso

- Bom, porque não muda datas sem a gestão.
- Ruim, porque a gestão replaneja à mão.

### C2. A ausência empurra a tarefa

- Bom, porque as datas já saem certas.
- Ruim, porque muda o cronograma por um cadastro de pessoas, sem a gestão decidir.
