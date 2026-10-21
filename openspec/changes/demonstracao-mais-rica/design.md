## Context

O `loadDemo` (`web/scripts/demo/load.ts`) entra como cada pessoa do diretório de demonstração e monta, pela API, o plano de `plan.ts`. Ele para se o ERP-FIN existe. Todos os projetos têm Marina Costa como gerente, e nenhum é criado a partir de um modelo.

## Goals / Non-Goals

**Goals:**
- Acrescentar dados novos a uma demonstração já carregada, sem apagar nada.
- Projetos em todos os status, de mais clientes e de outra gerente, com chamados e os demais itens.

**Non-Goals:**
- Pessoas novas no diretório de demonstração (pediria reiniciar a API).
- Chamados vencidos: os chamados abrem no dia da carga, pela própria API.

## Decisions

- **Só o que falta**: o comando lê os projetos (todas as situações), os clientes e os fornecedores que existem; cria os clientes e fornecedores do plano que faltam e carrega só os projetos cujo código não existe, na ordem do plano. Com todos lá, avisa e não muda nada, como antes. Os papéis no sistema são dados de novo, sem efeito quando já estão certos.
- **Gerente por projeto**: `manager` no projeto do plano (padrão Marina). A gerente monta o cronograma, muda o status e atualiza o progresso das tarefas sem responsável.
- **Projeto a partir de um modelo**: `fromTemplate` com o nome do modelo e o dia do início; o comando busca o modelo pelo nome em `GET /api/project-templates` e cria o projeto com `templateId` e `scheduleStart`, que gera o cronograma no servidor. O modelo vem de um projeto anterior do plano.
- **Projeto cancelado**: o cancelamento, no fim, fecha os chamados ainda abertos do projeto, com o evento na conversa (ADR-0043).
- **Pessoas**: as mesmas seis que entram; Beatriz Lima, que já é gerente de projeto no sistema, gerencia três projetos novos, e Marina é leitora num deles, sem abrir chamados ali.

## Risks / Trade-offs

- [Projeto carregado pela metade numa falha conta como existente] → o comando para na primeira falha e diz qual chamada falhou; para carregar de novo, apaga-se o banco, como antes.
- [Datas dos projetos novos relativas ao dia em que rodam] → numa demonstração carregada semanas antes, os projetos novos ficam com as datas da semana da nova carga.
