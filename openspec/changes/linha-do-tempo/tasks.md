# Tasks

## 1. Backend

- [ ] 1.1 Incluir `Schedule.Reschedule` (início e, numa tarefa, o fim, contando os dias úteis); verificar com os testes de domínio "Mudar o início pela API", "Mudar o fim pela API" e "Fim antes do início"
- [ ] 1.2 Criar `RescheduleScheduleItem` e a rota `PUT /schedule/items/{itemId}/dates`; verificar com os testes de casos de uso e da API, incluindo a ação na tabela de permissões
- [ ] 1.3 Devolver `nonWorkingDays` no `GET /schedule`; verificar com "Dias não úteis no cronograma"
- [ ] 1.4 Criar `GET /api/client-settings` com a chave `Syncfusion:LicenseKey`, só com sessão; verificar com os testes da API
- [ ] 1.5 Atualizar o contrato (`npm run api:update`)

## 2. Componente ScheduleGantt

- [ ] 2.1 Instalar os pacotes do Syncfusion, restringir o import a `web/src/schedule-gantt` pelo lint, tirar a fonte do Google do CSS e liberar as licenças `@syncfusion/`; verificar com o lint, o build e o `frontend.sh`
- [ ] 2.2 Criar `ganttRows.ts` e `ganttLayout.ts` (linhas, datas, escala e ajuste ao espaço); verificar com testes unitários de "Período ajustado ao espaço", "Período que não cabe" e "Mudar a escala"
- [ ] 2.3 Criar o `ScheduleGantt` com o visual do design system nos dois temas, os dias não úteis, a linha de hoje, o arrastar, a borda e a ligação, e a chave da licença; verificar com o teste do componente e as capturas

## 3. Aba Cronograma

- [ ] 3.1 Criar a barra de ferramentas (vista, escala, Hoje, resumo, Expandir, Nova fase, Nova tarefa), a vista no endereço e a legenda; verificar com "Alternar Linha do tempo e Lista" e "Linha do tempo pela tela"
- [ ] 3.2 Ligar o arrastar, a borda, a ligação e a escolha de item ou fase à API e ao detalhe; verificar com "Arrastar uma tarefa", "Puxar a borda de uma tarefa", "Arrastar sem permissão", "Ligar duas barras", "Ligação circular pela linha do tempo" e "Detalhe pela linha do tempo"
- [ ] 3.3 Criar Expandir com a tabela completa; verificar com "Expandir o cronograma"

## 4. Documentação e verificação

- [ ] 4.1 Atualizar o glossário, o `CLAUDE.md`, `docs/instalacao.md` (a chave) e o roadmap; verificar lendo os trechos alterados
- [ ] 4.2 Rodar `scripts/ci` sem erros nem avisos e conferir a Linha do tempo nos temas claro e escuro, em 1440×900, 1920×1080 e no celular, guardando as capturas para o resumo

## Workflow follow-up

- Abrir o PR, atualizar o roadmap para "PR aberto" e avisar Marcelo com as decisões e as capturas.
- Depois da aprovação, arquivar a change, aceitar o ADR-0087, marcar a Cronograma-3 como concluída e esperar o merge.
