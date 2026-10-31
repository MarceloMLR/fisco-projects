## 1. Decisão e roadmap

- [x] 1.1 ADR-0134 aceito no índice dos ADRs e o grupo 13 (Linha do tempo própria) no roadmap

## 2. Desenho

- [x] 2.1 `ganttRows` sem as conversões para o Syncfusion, com o nível e o recolher; posição dos dias e cabeçalhos da escala (`timelineGeometry`, `scaleTiers`), com testes sem tela
- [x] 2.2 Tabela em árvore acessível: as colunas de hoje (compacta, completa e celular), o atraso, a seta de recolher e o teclado
- [x] 2.3 Gráfico: a escala ajustada ao espaço, os dias não úteis, hoje, as fases, as tarefas com o progresso, os marcos, a linha de base, o caminho crítico, as setas, a escolha e o `goToToday`
- [x] 2.4 Rolagem e divisória: a vertical junta, a lateral só no gráfico, os cabeçalhos presos e a divisória que se arrasta, também pelo teclado

## 3. Interações

- [x] 3.1 Mover e puxar a borda pelos eventos de ponteiro, em dias inteiros, com o rascunho até a API responder e a volta na recusa
- [x] 3.2 Ligar pela ponta da barra, só entre itens com datas próprias
- [x] 3.3 Clique sem movimento escolhe a linha; nada se arrasta para quem não edita nem no celular

## 4. Troca e limpeza

- [x] 4.1 `ScheduleTimeline` sem a licença e `ProjectSchedulePage` sem a espera do Expandir
- [x] 4.2 Sem `@syncfusion/*`: pacotes, CSS, paleta, `vite.config.ts`; o lint e um teste recusam o Syncfusion
- [x] 4.3 Testes de tela com o componente real (Linha do tempo, Expandir, caminho crítico, linha de base, detalhe, recusa), cobrindo os cenários das specs do cronograma

## 5. Documentação e verificação

- [x] 5.1 Design system (componente Gantt) e `CLAUDE.md`
- [x] 5.2 Conferência no navegador contra o design system, nos dois temas, no computador e no celular
- [ ] 5.3 Lint, tipos, testes, build e specs
