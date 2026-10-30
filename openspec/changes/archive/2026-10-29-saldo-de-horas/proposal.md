## Why

A Horas-1 deu a cada pessoa o apontamento das próprias horas, mas o administrador ainda não sabe se um projeto gastou mais horas do que as contratadas: faltam as horas contratadas, as adicionais e a visão gerencial que o ADR-0128 descreve. Esta é a segunda fatia das horas do projeto (Horas-2, `saldo-de-horas`), com as telas aprovadas no canvas "Horas do projeto" em 29/10/2026.

## What Changes

- O administrador cadastra as horas contratadas de cada projeto (ou as tira, e o projeto fica sem saldo) e inclui, corrige e apaga as horas adicionais, cada uma com o dia, as horas e o motivo (o pedido fora do escopo). Toda mudança vai para a auditoria.
- O saldo de cada projeto: as contratadas e as adicionais menos as apontadas, a parte consumida e quatro estados, por ícone, cor e texto (ADR-0065): acima do contratado, perto do limite (a partir de 90% num projeto em aberto), dentro e sem horas contratadas. A regra fica num lugar só, no domínio.
- API só do administrador em `/api/admin/project-hours`: a lista dos projetos com o saldo (Em aberto, Encerrados e Todos, com busca por código ou nome), o detalhe de um projeto (as adicionais, as horas de cada pessoa e a parte dela) e as alterações das contratadas e das adicionais.
- A lista de apontamentos (`GET /api/time-entries`) aceita, só para o administrador, o projeto (`projectId`), para ele ver os apontamentos de todas as pessoas de um projeto; cada apontamento passa a trazer a pessoa.
- Contrato novo do `Projects.Contracts` com os projetos, a situação e o cliente (`IProjectCatalog`), que o `Projects` implementa, para a lista mostrar a situação e o cliente sem ler as tabelas do `Projects`.
- Tela Horas dos projetos (`/admin/project-hours`), na seção nova Acompanhamento, a primeira da coluna da Administração: a lista com o saldo e o consumo, "Baixar planilha" e o detalhe na gaveta, com "Alterar" as contratadas, "Incluir horas adicionais", o "⋮" de cada adicional, "Por pessoa" e "Ver apontamentos", que abre os apontamentos do projeto por mês, com corrigir e apagar.
- As horas contratadas e adicionais saem com o projeto excluído.
- Dados de demonstração com horas contratadas e adicionais, com um projeto acima do contratado, um perto do limite, um dentro e um sem horas contratadas.

## Capabilities

### New Capabilities
- Nenhuma: a capacidade `horas-do-projeto` já existe.

### Modified Capabilities
- `horas-do-projeto`: as horas contratadas e adicionais, o saldo, a visão gerencial do administrador, os apontamentos de um projeto pelo administrador e as telas da Administração.
- `projetos`: a exclusão do projeto apaga também as horas contratadas e adicionais.
- `dados-de-demonstracao`: horas contratadas e adicionais na demonstração.

## Non-goals

- Valores em reais, custo ou preço por hora, orçamento e faturamento (ADR-0016, ADR-0128).
- O gerente ou o líder técnico vendo o saldo ou cadastrando horas adicionais (ADR-0128, C1).
- Aprovação das horas, fechamento do mês, horas planejadas por tarefa e cronômetro (ADR-0128, depois).
- Aviso por e-mail ou no sino quando um projeto passa do contratado.
- A tela Horas dos projetos no celular: a Administração não tem versão de celular no canvas, e a tela segue o que as outras seções fazem.

## Impact

- `src/Modules/TimeTracking`: entidades `ContractedHours` e `AdditionalHours`, a regra do saldo (`HoursBalance`), os casos de uso e as rotas da Administração, a migração, o aquecimento e a exclusão do projeto.
- `Projects.Contracts`: `IProjectCatalog`, implementado no `Projects`.
- Telas: `web/src/admin` (página, gaveta, diálogos, planilha e regras do saldo), a seção Acompanhamento no `AdminLayout`, a rota, as consultas, os textos em `pt-BR.json`, a auditoria e os tipos da API; o `ProgressBar` ganha os tons do design system (fp-progress--info, --warning e --danger) e a linha de rótulo.
- `web/scripts/demo/plan.ts`: horas contratadas e adicionais.
- `docs/roadmap.md`, `docs/glossario.md`, `CLAUDE.md`.
- ADRs: 0013, 0016, 0039, 0046, 0056, 0065, 0066, 0081, 0099, 0110, 0111, 0128.
