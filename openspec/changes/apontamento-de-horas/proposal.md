## Why

A v1 deixou as horas trabalhadas fora (ADR-0016, ADR-0020), e sem elas o administrador não sabe se um projeto gastou mais horas do que as contratadas. Em 29/10/2026, com a v1 concluída, Marcelo pediu as horas do projeto, só em horas e com a visão gerencial só do administrador (ADR-0128). Esta é a primeira fatia (Horas-1, fora do roadmap aprovado em 17/10/2026): cada pessoa aponta as próprias horas. A segunda fatia (Horas-2, `saldo-de-horas`) traz as horas contratadas e adicionais e a visão gerencial.

## What Changes

- Novo módulo `TimeTracking` (schema `time_tracking`), dono dos apontamentos de horas: o dia, as horas (de 0,25 a 24, em quartos de hora), a descrição, o projeto e, opcionalmente, uma tarefa do cronograma do projeto ou um chamado do projeto que a pessoa vê.
- Quem aponta: as pessoas do projeto, menos o leitor, e o administrador em qualquer projeto; o projeto encerrado não recebe apontamentos nem correções; o dia não passa de hoje, no fuso da instalação; a soma da pessoa num dia não passa de 24 horas.
- Cada pessoa vê, corrige e apaga só as próprias horas; o administrador vê, corrige e apaga as de qualquer pessoa, pela API (a tela dele vem na Horas-2). Toda mudança vai para a auditoria.
- API em `/api/time-entries`: a lista da pessoa por período, com o total; incluir, corrigir e apagar; e as opções para apontar (os projetos em que a pessoa aponta, as tarefas e os chamados de um projeto).
- Os outros módulos entregam o que o apontamento precisa pelos contratos: as tarefas pelo `IScheduleItems` do `Projects.Contracts` e os chamados do projeto que a pessoa vê por um contrato novo do `Tickets.Contracts`.
- As horas saem com o projeto excluído (`IProjectDeletion`) e aparecem com o nome atual de quem apontou, também depois da anonimização.
- Tela Minhas horas (`/hours`), na barra lateral e no menu do celular, entre Chamados e Administração: a semana com os apontamentos agrupados por dia e o total de cada dia e da semana, "Apontar horas" num diálogo e o "⋮" com corrigir e apagar.
- Dados de demonstração com horas apontadas pelas pessoas dos projetos.

## Capabilities

### New Capabilities
- `horas-do-projeto`: apontamento das horas de cada pessoa nos projetos, com as regras de quem aponta, vê e corrige, e a tela Minhas horas; a Horas-2 acrescenta aqui as horas contratadas, as adicionais e a visão gerencial.

### Modified Capabilities
- `estrutura-das-telas`: o item Minhas horas na barra lateral e no menu do celular.
- `projetos`: a exclusão do projeto apaga também as horas apontadas nele.
- `dados-de-demonstracao`: horas apontadas na demonstração.

## Non-goals

- Horas contratadas, horas adicionais, saldo e visão gerencial do administrador (Horas-2).
- Valores em reais, custo ou preço por hora e faturamento (ADR-0016, ADR-0128).
- Aprovação das horas pelo gerente, fechamento do mês, cronômetro e horas planejadas por tarefa (ADR-0128, depois).
- O gerente ou o líder técnico vendo as horas de outras pessoas.
- Apontar a partir da página do chamado ou do detalhe da tarefa, e as horas no Meu trabalho.

## Impact

- Novo módulo `src/Modules/TimeTracking` (`FiscoProjects.TimeTracking` e `FiscoProjects.TimeTracking.Contracts`), com migração própria, aquecimento (`IWarmUp`), exclusão do projeto (`IProjectDeletion`) e testes de arquitetura, unidade, API e permissões.
- `Tickets.Contracts`: contrato novo com os chamados de um projeto que a pessoa vê; `Tickets` o implementa.
- Telas: `web/src/hours` (página, diálogo e regras), item na barra lateral e no menu do celular, rota, consultas, textos em `pt-BR.json` e tipos da API.
- `web/scripts/demo/plan.ts`: horas na demonstração.
- `docs/adr/0128-horas-do-projeto.md`, `docs/roadmap.md`, `docs/glossario.md`, `docs/discovery.md`, `CLAUDE.md`, `openspec/config.yaml`.
- ADRs: 0013, 0016, 0020, 0039, 0040, 0043, 0047, 0056, 0058, 0065, 0066, 0110, 0111, 0128.
