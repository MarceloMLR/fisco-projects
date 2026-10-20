## 1. Contrato dos projetos

- [x] 1.1 `IProjectAccess`, `ProjectAccess`, `ProjectMemberInfo` e `ProjectMemberRole` no `Projects.Contracts`, com a implementação no `Projects`

## 2. Módulo Tickets

- [x] 2.1 Projetos `FiscoProjects.Tickets` e `FiscoProjects.Tickets.Contracts`, registro no host e na solução
- [x] 2.2 Domínio: `Ticket`, `TicketTeam`, `TicketPriority`, `TicketStatus` e as regras de `TicketAccess`
- [x] 2.3 Persistência: `TicketsDbContext`, a sequência dos números, a configuração com a auditoria e a migração
- [x] 2.4 Casos de uso: abrir, listar, contar, escolhas e o chamado pelo número
- [x] 2.5 Rotas, problemas e tipos de resposta

## 3. Testes do backend

- [x] 3.1 Testes do domínio (`TicketAccess`, validação do chamado)
- [x] 3.2 Testes da API para cada cenário da spec `chamados`, com a matriz de abrir e ver

## 4. Frontend

- [x] 4.1 Contrato e tipos (`npm run api:update`), consultas e textos
- [x] 4.2 Página Chamados com as visões, os filtros e a tabela
- [x] 4.3 Aba Chamados do projeto
- [x] 4.4 Abrir chamado, pela página e pela aba
- [x] 4.5 Página do chamado
- [x] 4.6 Tipo "Chamado" na Auditoria
- [x] 4.7 Testes das telas para os cenários da spec

## 5. Demonstração e documentação

- [x] 5.1 Chamados no plano de demonstração, com o teste
- [x] 5.2 `CLAUDE.md`, glossário e roadmap
