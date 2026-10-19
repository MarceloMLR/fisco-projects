# Proposta: dados-de-demonstracao (Cronograma-3.1)

## Why

O roadmap pede que os dados fictícios de demonstração cresçam a cada change, mas eles só existiram no ambiente do agente, criados por scripts fora do repositório. Na máquina de Marcelo, as telas de clientes, projetos, membros e cronograma aparecem vazias depois de atualizar o código. Marcelo pediu para incluí-los.

## What Changes

- **Comando `npm run demo:data`** (em `web/`), que carrega pela API e pela entrada local (ADR-0078):
  - as pessoas do diretório de demonstração, com Marina e Beatriz como gerentes de projeto;
  - os clientes Alfa Distribuidora, Beta Alimentos e Gama Têxtil;
  - os projetos ERP-FIN, SPED-27, TRIB-RS, PORTAL-NF e WMS-ALFA (este concluído), com gerente, líder técnico e membros;
  - os cronogramas com fases, tarefas, marcos, dependências, progresso, marcos atingidos e uma tarefa atrasada.
- **Quem faz cada ação:** o administrador cria clientes e projetos, a gerente monta o cronograma e cada responsável atualiza o progresso da própria tarefa, como na tela.
- **Datas relativas ao dia em que roda:** o ERP-FIN começa na segunda-feira de duas semanas antes; o servidor ajusta as datas aos dias úteis.
- **Não repete:** com o ERP-FIN já lá, o comando avisa e não muda nada. Sem a entrada local (produção), ele para.
- **ADR-0089** (novo, proposta): dados de demonstração por um comando que usa a própria API.
- **Regra:** cada change que cria telas ou dados acrescenta os dela ao comando, e o `CLAUDE.md` passa a dizer isso.

## Non-goals

- Apagar ou recarregar os dados: para começar de novo, o banco local é recriado (`docker compose down -v`).
- Dados de telas que ainda não existem (chamados, documentos, acessos); entram com as changes delas.
- Subtarefas: entram com a Cronograma-4.

## Capabilities

### New Capabilities

- `dados-de-demonstracao`: o comando que carrega os dados fictícios de demonstração e as datas relativas ao dia em que roda.

### Modified Capabilities

Nenhuma.

## Impact

- **Frontend:** `web/scripts/demo/` com o plano dos dados e a carga pela API, tipada pelo contrato (`src/api/schema.ts`), o script `demo:data` e os testes da carga com a API simulada.
- **Dependência de desenvolvimento:** `@types/node` da linha 24, para os tipos do Node no comando.
- **Documentação:** ADR-0089, `CLAUDE.md` (como carregar e a regra de manter os dados), `docs/instalacao.md` e roadmap (Cronograma-3.1).
