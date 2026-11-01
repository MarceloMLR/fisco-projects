## 1. Decisão e roadmap

- [x] 1.1 ADR-0142 e GanttB-5 em andamento no roadmap

## 2. Configuração

- [x] 2.1 `WorkHoursPerDay` nas configurações da instalação, com a migração, o contrato e a auditoria; testes "Horas por dia padrão", "Trocar as horas por dia" e "Horas por dia inválidas"

## 3. Domínio e API

- [x] 3.1 `TeamLoad` no domínio das horas; testes "Tarefa espalhada pelas semanas", "Carga sobre um feriado", "Capacidade com férias", "Capacidade com feriado", "Estados da carga" e "Carga na semana toda de férias"
- [x] 3.2 O contrato `ITeamWork` e a implementação no `Projects`; o `GetTeamLoad` e a rota; testes "Carga entre projetos", "O que não pesa", "Pessoa livre na carga", "Pessoa desativada fora da carga" e "Permissões na carga por papel"

## 4. Tela

- [x] 4.1 `npm run api:update`; o campo nas Configurações gerais
- [x] 4.2 A página Carga da equipe e a gaveta; testes "Carga da equipe na tela", "Com sobrecarga pela tela", "Próximas semanas pela tela" e "Detalhe da carga"
- [x] 4.3 A demonstração; teste "Carga na demonstração"
- [x] 4.4 Conferência no navegador, no computador e no celular, nos temas claro e escuro

## 5. Documentação

- [x] 5.1 `CLAUDE.md` e `docs/glossario.md` com a carga da equipe
