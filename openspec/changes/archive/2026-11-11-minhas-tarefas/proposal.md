## Why

Quem trabalha em vários projetos precisa abrir o cronograma de cada um para ver as próprias tarefas: o Meu trabalho mostra só o que vence nos próximos 7 dias (ADR-0096), e o filtro "Minhas tarefas" do cronograma é de um projeto por vez. O Jira, o Monday e o Planner têm uma página com as tarefas da pessoa de todos os projetos. Tarefas-4, do grupo 16 (Tarefas), a pedido de Marcelo em 11/11/2026, com as decisões aprovadas por ele no ADR-0158.

## What Changes

- A página "Minhas tarefas" (`/my-tasks`), na barra lateral entre Início e Projetos, para todos: as tarefas com datas próprias de que a pessoa é responsável nos projetos em aberto de que participa.
- Agrupada por projeto de início (com o nome e o atalho para o cronograma) ou por data (Atrasadas, Hoje, Esta semana, Próxima semana, Depois e Concluídas).
- Lista e Quadro, como no cronograma, com o código do projeto em cada linha e cartão; o Quadro move o cartão pelo progresso.
- "Mostrar concluídas": as concluídas nos últimos 30 dias.
- O detalhe abre na própria página: o projeto e a fase, as datas, o progresso, a nota, os comentários e "Abrir no cronograma".
- `GET /api/my-tasks` (`ListMyTasks`), só com as tarefas da pessoa na sessão; nenhuma permissão muda.

## Capabilities

### New Capabilities

- `minhas-tarefas`: a página Minhas tarefas, o que ela traz, os agrupamentos, a Lista, o Quadro e o detalhe.

### Modified Capabilities

- `estrutura-das-telas`: o item Minhas tarefas na barra lateral e no menu do celular.

## Non-goals

- Mudar o que cada papel vê nos projetos: o cronograma continua inteiro para todos do projeto (ADR-0043).
- Tarefas de outras pessoas na página, marcos e a tarefa-resumo.
- Mudar a fila do Meu trabalho.

## Impact

- `src/Modules/Projects`: o caso de uso `ListMyTasks`, a consulta dos projetos com tarefas da pessoa no `IScheduleStore`, a rota e o aquecimento do módulo.
- `web/src`: a página `MyTasksPage` (`web/src/my-tasks`), o item da barra lateral, a rota, a consulta, o Quadro aberto a cartões de vários projetos (`TaskBoard`), o detalhe e os textos.
- Testes da API e de tela; o ADR-0158, `CLAUDE.md`, `docs/glossario.md` e o roadmap.
