## Why

O administrador põe as horas previstas de cada tarefa (ADR-0138) e a gestão cadastra as ausências das pessoas (ADR-0141), mas ninguém vê, entre os projetos, quanto cada pessoa tem para fazer em cada semana: a sobrecarga só aparece quando as tarefas atrasam. GanttB-5, do grupo 15 (Aprimoramento do Gantt 2), a pedido de Marcelo; as decisões ficam no ADR-0142.

## What Changes

- A carga de cada pessoa por semana, entre todos os projetos em aberto: as horas previstas de cada tarefa abaixo de 100% com a pessoa como responsável, em partes iguais pelos dias úteis da tarefa.
- A capacidade da semana: os dias úteis menos os dias de ausência, vezes as horas por dia de trabalho; o estado de cada semana (acima da capacidade, perto do limite, dentro, livre).
- "Horas por dia de trabalho" nas Configurações gerais, de 1 a 12, 8 de início.
- Administração → Acompanhamento → "Carga da equipe", só do administrador: oito semanas, a busca, a visão "Com sobrecarga" e o detalhe da pessoa com as tarefas e as ausências de cada semana.
- API: `GET /api/admin/team-load?week=`; `workHoursPerDay` em `GET` e `PUT /api/installation`.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `horas-do-projeto`: a carga da equipe, a capacidade, os estados, as permissões e as telas.
- `configuracao-da-instalacao`: as horas por dia de trabalho.
- `dados-de-demonstracao`: horas previstas que mostram uma pessoa acima da capacidade.

## Non-goals

- Capacidade por pessoa (meio período).
- A carga para o gerente ou para a própria pessoa.
- Aviso por e-mail ou no sino; nivelamento automático.
- Carga pelas horas apontadas.

## Impact

- `src/Modules/Projects`: o contrato `ITeamWork` do `Projects.Contracts` (as tarefas com responsável dos projetos em aberto, as ausências e as pessoas dos projetos em aberto) e a implementação.
- `src/Modules/TimeTracking`: a regra da carga no domínio (`TeamLoad`), o caso de uso `GetTeamLoad` e a rota; a referência ao `Calendar.Contracts`.
- `src/Modules/Administration`: `WorkHoursPerDay` nas configurações (migração) e no `InstallationInfo`.
- `web/src`: a página "Carga da equipe", a gaveta, o campo nas Configurações gerais, os textos, os tipos gerados e os testes; a demonstração.
- Documentação: o ADR-0142, `CLAUDE.md`, `docs/glossario.md` e o roadmap.
