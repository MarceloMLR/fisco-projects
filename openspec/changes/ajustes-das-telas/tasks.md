## 1. Dias de atraso na API

- [x] 1.1 `ScheduleItem.DaysLate` e `ScheduleFigures.LateDaysOf`, com a tarefa-resumo, e os testes de domínio "Dias de atraso da tarefa", "Atraso que começou num dia não útil" e "Dias de atraso do marco"
- [x] 1.2 `GetSchedule` com os dias de atraso pelo calendário já lido, `lateDays` na resposta do item, contrato OpenAPI e tipos do frontend, e o teste da API

## 2. Dias de atraso na tela

- [x] 2.1 Textos "1 dia útil de atraso" e "N dias úteis de atraso"; a coluna Fim da Lista (embaixo da data da tarefa e sozinho no marco), o cabeçalho do detalhe e a dica do ícone na Linha do tempo, com "Atrasada" na tarefa quando o atraso é zero
- [x] 2.2 Testes de tela: "Cronograma pela tela", "Estados do marco pela tela", "Atraso desde um dia não útil pela tela" e "Linha do tempo pela tela"

## 3. Linha do tempo, abas e CNPJ

- [x] 3.1 `ScheduleGantt` limpa a seleção quando o detalhe fecha, e a aba informa a fase aberta; teste "Reabrir o detalhe pela linha do tempo"
- [x] 3.2 Abas do projeto sem rolagem vertical; teste "Abas sem barra de rolagem"
- [x] 3.3 `cnpjProblem` e as mensagens do motivo no `ClientDialog`; testes "CNPJ inválido" e "Motivo do CNPJ inválido"

## 4. Subtarefas mais claras

- [x] 4.1 Seta de recolher e mostrar as subtarefas na Lista, com o recuo de 24 px; teste "Recolher subtarefas na lista" e "Subtarefas na lista"
- [x] 4.2 Seção Subtarefas no detalhe, com "Nova subtarefa" para quem edita, e o rodapé com os botões do mesmo tamanho; testes "Nova subtarefa pela tela" e "Detalhe da tarefa-resumo"

## 5. Dados de demonstração e documentação

- [x] 5.1 O fim da carga de demonstração com o endereço das telas; testes "Carregar dados de demonstração" e "Dados já carregados"
- [x] 5.2 `CLAUDE.md` (o `lateDays` e a seção Subtarefas) e o roadmap da Cronograma-4.1
- [ ] 5.3 Build sem avisos, testes, format, frontend e specs; conferir no navegador, nos temas claro e escuro, no computador e no celular
