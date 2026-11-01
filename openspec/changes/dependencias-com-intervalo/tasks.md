## 1. Decisão e roadmap

- [x] 1.1 ADR-0137 e as notas nos ADR-0019 e 0135; GanttA-4 em andamento no roadmap

## 2. Domínio e API

- [ ] 2.1 `ScheduleLinkType` e `Lag` na dependência (migração), a regra em `ScheduleLinks`, o início, o empurrão e o recálculo; testes "Intervalo entre as tarefas", "Começa junto", "Termina junto" e "Empurrar pelo início"
- [ ] 2.2 O caminho crítico com os tipos e o intervalo; o desfazer e o modelo com eles
- [ ] 2.3 `links` no pedido e na resposta do item, com a validação; teste "Tipo e intervalo inválidos"
- [ ] 2.4 O MS Project com os três tipos e o intervalo; testes "Tipos no MS Project", "O que não cabe no FiscoProjects" e "Ida e volta pelo MS Project"

## 3. Tela

- [ ] 3.1 `npm run api:update`; o tipo e o intervalo no detalhe e na coluna "Depende de"; teste "Tipo e intervalo no detalhe"
- [ ] 3.2 As setas de "começa junto" e "termina junto" na Linha do tempo; teste "Setas dos tipos na Linha do tempo"
- [ ] 3.3 As siglas no Excel; teste "Dependências no Excel"
- [ ] 3.4 Uma dependência de cada tipo na demonstração; conferência no navegador, no computador e no celular, nos temas claro e escuro

## 4. Documentação

- [ ] 4.1 `CLAUDE.md` e `docs/glossario.md` com os tipos e o intervalo
