# Tasks

## 1. Domínio do cronograma

- [x] 1.1 Criar `Schedule`, `SchedulePhase`, `ScheduleItem` (tarefa e marco) e `ScheduleDependency` no domínio do `Projects`, com fases, início em dia útil, fim calculado pelo `IBusinessDays`, responsável, progresso com data de conclusão e exclusão em cascata pelo domínio; verificar com os testes unitários "Criar e renomear fase", "Excluir fase com tarefas", "Criar tarefa", "Marco sem duração", "Início num sábado", "Concluir tarefa guarda a data" e "Voltar abaixo de 100%"
- [x] 1.2 Implementar as dependências (primeiro dia possível, ciclo recusado) e o empurrão em ordem topológica, com a contagem de itens empurrados; verificar com "Tarefa depois da anterior", "Dependência circular", "Atrasar empurra a cadeia", "Antecipar não puxa" e "Excluir a tarefa anterior"
- [x] 1.3 Implementar `ScheduleFigures` (datas e progresso ponderado das fases e do projeto, atraso, fase atual, conclusão e marcos) e `Schedule.Unassign`; verificar com "Progresso ponderado pela duração", "Tarefa vencida", "Tarefa concluída ou do dia não fica atrasada" e os testes da fase atual
- [x] 1.4 Incluir `CanEditSchedule` em `ProjectPermissions` (administrador e gerente) e verificar com a tabela de permissões do domínio

## 2. Persistência e auditoria

- [ ] 2.1 Configurar as tabelas `schedules`, `schedule_phases`, `schedule_items` e `schedule_dependencies` (cascata só da raiz, índices, datas como `date`), as marcas da auditoria (rótulos, referências, contexto `ProjectId` e `Kind`, `Position` fora, raiz fora) e criar a migração `Schedule`; verificar com `dotnet ef migrations` sem mudanças pendentes e o teste de migração do desenvolvimento
- [ ] 2.2 Criar `IScheduleStore` (`FindAsync`, `GetOrAddAsync`, leitura das fases e itens de vários projetos) e as fontes de nomes `SchedulePhase` e `ScheduleItem` (ADR-0084), com as constantes em `ProjectsAudit`; verificar com testes de integração do repositório

## 3. Casos de uso e API do cronograma

- [ ] 3.1 Criar `InstallationClock` (hoje no fuso da instalação) e os casos de uso `GetSchedule`, `AddSchedulePhase`, `RenameSchedulePhase`, `RemoveSchedulePhase`, `AddScheduleItem`, `ChangeScheduleItem` e `RemoveScheduleItem`, na transação serializável, com validação dos campos, responsável do projeto que não é leitor, 403, 404 e 409; verificar com os testes de casos de uso, inclusive "Fase sem nome", "Tarefa inválida", "Responsável do projeto", "Leitor ou pessoa de fora como responsável", "Alterar tarefa num projeto concluído" e duas edições ao mesmo tempo
- [ ] 3.2 Mapear as rotas `/api/projects/{id}/schedule` (GET, fases e itens), com Problem Details e o tipo de problema do ciclo; verificar com os testes da API gerados da tabela de permissões ("Gerente edita o cronograma", "Administrador fora do projeto edita o cronograma", "Líder técnico só vê o cronograma", "Colaborador, desenvolvedor e leitor só veem o cronograma", "Cronograma de um projeto de fora", "Cronograma sem sessão")
- [ ] 3.3 Deixar as tarefas sem responsável ao remover a pessoa ou trocá-la para leitor; verificar com "Remover o responsável de tarefas" e "Responsável passa a leitor" (casos de uso e API, com a auditoria)
- [ ] 3.4 Incluir o resumo do cronograma na lista de projetos (`schedule` e `today`) e as datas e `canEditSchedule` no projeto; verificar com testes da API da lista e do projeto
- [ ] 3.5 Verificar a auditoria pela API com "Tarefa empurrada na auditoria" e "Dependência na auditoria", e atualizar o contrato (`npm run api:update`), conferindo o teste do contrato

## 4. Componentes da interface

- [ ] 4.1 Criar `ProgressBar` (fp-progress) e `ScheduleStrip` (fp-strip, com fases, parte concluída, fases atrasadas, marcos e hoje pelos tokens do cronograma); verificar com testes de componente e a regra de cores
- [ ] 4.2 Criar `MultiSelect` (Autocomplete múltiplo no visual fp-field) e a opção `groupRowsChoosable` do `DataTable`; verificar com testes de componente

## 5. Lista de projetos e cabeçalho

- [ ] 5.1 Mostrar na lista de Projetos a faixa de fases com o progresso e a fase atual, "Começa em", a conclusão, o prazo final e as tarefas atrasadas, ou "Sem cronograma"; verificar com "Cronograma na lista", "Projeto que ainda não começou" e "Projeto sem cronograma na lista"
- [ ] 5.2 Mostrar no cabeçalho as datas do cronograma e a aba Cronograma, entre Visão geral e Membros; verificar com "Datas do cronograma no cabeçalho" e o teste das abas

## 6. Aba Cronograma

- [ ] 6.1 Criar a rota `projects/:projectId/schedule` com a Lista: fases em linhas de grupo, itens recuados, bandeira do marco, "Atrasada" com ícone e cor, progresso, responsável, dependências, o resumo e o estado vazio; verificar com "Cronograma pela tela" e "Cronograma vazio"
- [ ] 6.2 Criar "Nova fase", o diálogo da fase (renomear e excluir com confirmação), "Nova tarefa" e o detalhe na gaveta com o formulário único do item, a mensagem de itens empurrados e o modo só leitura; verificar com "Nova tarefa pela tela", "Empurrar pela tela", "Excluir fase pela tela" e "Cronograma sem editar pela tela"
- [ ] 6.3 Avisar na confirmação da remoção de uma pessoa que as tarefas dela ficam sem responsável e invalidar o cronograma depois das mudanças de pessoas; verificar com o teste da tela Membros
- [ ] 6.4 Incluir os textos da auditoria (títulos de tarefa e marco, campos, valores e os três tipos no filtro); verificar com os testes de `auditText`

## 7. Documentação

- [ ] 7.1 Atualizar o glossário, o `CLAUDE.md`, o guia `src/Modules/README.md` (se a auditoria ou os módulos mudarem) e o roadmap; verificar lendo os trechos alterados

## 8. Verificação

- [ ] 8.1 Rodar `scripts/ci` (build, testes, formato, frontend e specs) sem erros nem avisos
- [ ] 8.2 Conferir as telas no navegador, nos temas claro e escuro, no computador e no celular (Projetos, cabeçalho, aba Cronograma, diálogo e gaveta), e guardar as capturas para o resumo

## Workflow follow-up

- Abrir o PR, atualizar o roadmap para "PR aberto" e avisar Marcelo com as decisões e as capturas.
- Depois da aprovação, arquivar a change com a sincronização das specs, marcar a Cronograma-1 como concluída e esperar o merge.
