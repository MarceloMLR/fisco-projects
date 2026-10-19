# Proposta: calendário da instalação (Projetos-1)

## Why

O cronograma e os prazos dos chamados contam dias úteis, pulando fins de semana, feriados e os pontos facultativos que a empresa considera (ADR-0018, ADR-0025). Antes de criar projetos e cronogramas, a instalação precisa de um calendário único, num lugar só: os dias de trabalho, os feriados nacionais calculados, a escolha dos pontos facultativos e os feriados locais, com uma conta de dias úteis que os outros módulos reaproveitam.

## What Changes

- **Novo módulo `Calendar`** (schema `calendar`), dono do calendário da instalação:
  - **dias de trabalho** da semana, com o padrão de segunda a sexta e pelo menos um dia marcado;
  - **feriados nacionais e pontos facultativos calculados** pelo próprio sistema, atrás de uma interface (ADR-0018). A Páscoa é calculada, e a lista e os padrões seguem o ADR-0083 (proposto): os feriados nacionais começam considerados e os pontos facultativos, desconsiderados;
  - **a escolha de considerar** cada feriado nacional ou ponto facultativo, que vale para todos os anos;
  - **feriados locais** cadastrados, com nome, data, "Repete todo ano" e a escolha de considerar. Podem ser incluídos, alterados e excluídos;
  - **a conta de dias úteis** no contrato `IWorkCalendar` (é dia útil, somar e contar dias úteis), para o cronograma e os prazos que vêm depois.
- **API, só do administrador** (ADR-0046), sob `/api/calendar`:
  - o calendário de um ano;
  - os dias de trabalho;
  - a escolha de um feriado calculado;
  - incluir, alterar e excluir feriados locais.
- **Tela Calendário e feriados** na seção Instalação da Administração, como no canvas (desenho de 19/10/2026, a aprovar):
  - "Dias de trabalho" no topo;
  - a lista do ano escolhido, com o tipo, a repetição e a coluna "Considerado", que grava na hora;
  - o diálogo "Novo feriado", a edição e a exclusão com confirmação;
  - no celular, uma lista no lugar da tabela.
- **Auditoria:** os dias de trabalho, as escolhas e os feriados locais entram na Auditoria, com textos próprios.

## Non-goals

- Usar o calendário no cronograma e nos prazos: isso vem com Cronograma-1 e Chamados-4, pelo contrato `IWorkCalendar`.
- Destacar fins de semana e feriados na linha do tempo (Cronograma-3).
- Calendário próprio por projeto (ADR-0018, "depois").
- Carga automática de feriados estaduais e municipais (ADR-0018, "fora").
- Férias e ausências de cada pessoa (ADR-0018, "fora").
- Meio período e horas: os prazos e as durações contam dias inteiros (ADR-0018, ADR-0025 e ADR-0083).
- Leitura do calendário por quem não é administrador. Ela entra quando uma tela de projeto precisar dela.

## Capabilities

### New Capabilities

- `calendario`: o calendário da instalação, que inclui:
  - os dias de trabalho;
  - os feriados nacionais e pontos facultativos calculados e a escolha de considerá-los;
  - os feriados locais;
  - a conta de dias úteis;
  - a tela Calendário e feriados e a auditoria dessas mudanças.

### Modified Capabilities

Nenhuma.

## Impact

- **Backend:**
  - módulo novo `FiscoProjects.Calendar` e `FiscoProjects.Calendar.Contracts`, com migração inicial (dias de trabalho e escolhas dos feriados calculados);
  - registro no host;
  - testes do módulo e da API, com a matriz de permissões.
- **Contrato OpenAPI** e tipos do frontend (`npm run api:update`).
- **Frontend:**
  - rota `/admin/calendar` e item "Calendário e feriados" na coluna da Administração;
  - consultas e alterações pelo TanStack Query;
  - diálogos com React Hook Form e Zod;
  - textos da auditoria, traduções e testes.
- **Documentação:**
  - `docs/glossario.md`: calendário, dias de trabalho, feriado nacional, ponto facultativo, feriado local e considerado;
  - `CLAUDE.md`: módulo `Calendar` no mapa;
  - `src/Modules/README.md`, se preciso;
  - `docs/roadmap.md`.
- **ADRs:**
  - 0018 (dias úteis e feriados) e 0083 (novo, proposto: pontos facultativos, dia inteiro e escolha para todos os anos);
  - 0025 (o mesmo calendário nos prazos);
  - 0003 (o que varia por empresa é cadastro), 0046 (só o administrador), 0039 (auditoria) e 0047 (módulo próprio, com contrato);
  - 0066 e 0081 (estrutura das telas).
