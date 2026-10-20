## 1. Contrato dos projetos

- [ ] 1.1 `IProjectAccess`, `ProjectAccess`, `ProjectMemberInfo` e `ProjectMemberRole` no `Projects.Contracts`, com a implementação no `Projects`

## 2. Módulo Tickets

- [ ] 2.1 Projetos `FiscoProjects.Tickets` e `FiscoProjects.Tickets.Contracts`, registro no host e na solução
- [ ] 2.2 Domínio: `Ticket`, `TicketTeam`, `TicketPriority`, `TicketStatus` e as regras de `TicketAccess`
- [ ] 2.3 Persistência: `TicketsDbContext`, a sequência dos números, a configuração com a auditoria e a migração
- [ ] 2.4 Casos de uso: abrir, listar, contar, escolhas e o chamado pelo número
- [ ] 2.5 Rotas, problemas e tipos de resposta

## 3. Testes do backend

- [ ] 3.1 Testes do domínio (`TicketAccess`, validação do chamado)
- [ ] 3.2 Testes da API para cada cenário da spec `chamados`, com a matriz de abrir e ver

## 4. Frontend

- [ ] 4.1 Contrato e tipos (`npm run api:update`), consultas e textos
- [ ] 4.2 Página Chamados com as visões, os filtros e a tabela
- [ ] 4.3 Aba Chamados do projeto
- [ ] 4.4 Abrir chamado, pela página e pela aba
- [ ] 4.5 Página do chamado
- [ ] 4.6 Tipo "Chamado" na Auditoria
- [ ] 4.7 Testes das telas para os cenários da spec

## 5. Demonstração e documentação

- [ ] 5.1 Chamados no plano de demonstração, com o teste
- [ ] 5.2 `CLAUDE.md`, glossário e roadmap
