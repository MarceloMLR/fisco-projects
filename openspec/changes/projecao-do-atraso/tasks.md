## 1. Roadmap

- [x] 1.1 Status-2 "Em andamento" no `docs/roadmap.md`

## 2. A projeção no domínio do cronograma

- [x] 2.1 `ScheduleProjections` no domínio do `Projects`: quem tem projeção (`IsBehind`), a tarefa projetada a partir de hoje ou do próximo dia útil, o empurrão dos itens que vêm depois em cópias, com o concluído parado, e o resultado (o fim e os dias úteis, os marcos e o fim do projeto), com os testes do domínio dos cenários de "Tarefa atrasada para a projeção", "Projeção do atraso" e "O que a tarefa atrasada empurra" (também "começa junto" e "termina junto")

## 3. A projeção na API

- [x] 3.1 `ScheduleView.Projections` no `GetSchedule`, fora do projeto encerrado, e `projection` em cada item do `GET /schedule`, com os testes da API dos cenários "Projeção pela API" e "Projeto encerrado não projeta"
- [x] 3.2 A atenção da visão geral com as tarefas atrasadas e as que têm projeção, cada uma com `start` e `projection` no lugar de `blocks`, com os testes da API dos cenários "Tarefa atrasada na atenção" e "Tarefa que devia ter começado na atenção"

## 4. Tela

- [x] 4.1 Seção "Projeção do atraso" nos dois detalhes da tarefa, com os textos em `schedule.ts`, nos temas claro e escuro, no computador e no celular, com os testes de tela dos cenários de "Projeção do atraso pela tela"
- [x] 4.2 Linha da tarefa em "Precisa de atenção" com o que ela empurra e a tarefa que devia ter começado, com os testes de `overview.ts` dos cenários de "Projeção do atraso na atenção"
- [x] 4.3 Contrato da API atualizado (`npm run api:update`), os simuladores dos testes de tela e `npm run lint`, `npm run typecheck` e `npm test` limpos

## 5. Documentação e verificação

- [x] 5.1 `CLAUDE.md` e `docs/glossario.md` com a projeção do atraso, e `openspec validate --all --strict` limpo
- [ ] 5.2 Verificação completa: `dotnet test GerenWare.slnx`, `dotnet format GerenWare.slnx --verify-no-changes` e a demonstração carregada mostrando a projeção no detalhe e na Visão geral

## Workflow follow-up

- PR com a aprovação de Marcelo, archive na mesma branch, roadmap "Concluída" e o merge por Marcelo.
