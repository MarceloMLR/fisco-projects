## Why

A demonstração tem três clientes e cinco projetos, quase todos da mesma gerente, e chamados concentrados no ERP-FIN. Marcelo pediu mais dados, para mostrar melhor tudo o que o sistema faz (Cronograma-3.3, a pedido de Marcelo). Hoje o comando não carrega nada quando o ERP-FIN existe, então os dados novos só entrariam apagando o banco da demonstração que já está no ar.

## What Changes

- O `npm run demo:data` passa a carregar só os projetos do plano que ainda não existem, com os clientes e fornecedores que faltam; os que existem ficam como estão.
- Três clientes novos e seis projetos novos: um em cada status que faltava (Pausado e Cancelado), um criado a partir do modelo "Implantação de ERP", um interno e três da segunda gerente, Beatriz Lima.
- Mais chamados em todos os projetos novos, com conversas, anexos, fornecedor, prioridade, atribuição, reabertura e cancelamento; documentos, links, acessos e termos de aceite também fora do ERP-FIN.
- O Meu trabalho de Beatriz ganha tarefa atrasada, tarefa da semana e chamado aguardando a resposta dela.

## Capabilities

### New Capabilities

### Modified Capabilities
- `dados-de-demonstracao`: a carga só do que falta e a demonstração mais rica.

## Impact

- `web/scripts/demo/plan.ts`, `load.ts` e testes; `CLAUDE.md` e roadmap. Nenhuma mudança na API nem nas telas.
