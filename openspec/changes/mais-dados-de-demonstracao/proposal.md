# Proposta: mais dados de demonstração (Cronograma-3.2)

## Why

Marcelo carregou a demonstração e não encontrou chamados com conversa. O banco dele já tinha o ERP-FIN de uma carga antiga, e o comando, que não repete, só disse que os dados já estavam lá. Mesmo num banco novo, quase todos os chamados de exemplo têm uma mensagem ou nenhuma, e só o ERP-FIN e o TRIB-RS têm chamados. Para mostrar o produto e conferir as telas, a demonstração precisa de chamados com vida.

## What Changes

- **Chamados em todos os projetos**, inclusive no concluído, com os chamados já fechados ou cancelados antes da conclusão (ADR-0043).
- **Todos os status do fluxo**: Aberto, Em triagem, Em andamento, Aguardando solicitante, Resolvido, Fechado e Cancelado, e um chamado reaberto (ADR-0024).
- **Conversas longas**, com várias pessoas e papéis, menções e anexos de vários tipos (XML, PDF, imagem e planilha), inclusive anexos na descrição, todos fictícios e feitos pelo comando (ADR-0026, ADR-0069).
- **Prioridade e acompanhamento**: a gerente sobe a prioridade de um chamado e o administrador passa a acompanhar outro (ADR-0025, ADR-0026).
- **Fornecedores**: dois chamados em fornecedores, um deles com o link e três atualizações de status (ADR-0028).
- **O que acontece com cada chamado vira uma sequência de passos no plano** (mensagem, ação do fluxo, prioridade, acompanhar, atribuir, transferir, registrar e atualizar no fornecedor), na ordem em que alguém faria na tela.
- **Dados já carregados**: o aviso passa a dizer como recriar o banco local para carregar de novo.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `dados-de-demonstracao`: carga repetida com a orientação para recarregar; chamados em todos os projetos; conversas longas com anexos de vários tipos; todos os status; dois chamados em fornecedores com várias atualizações; prioridade e acompanhamento.

## Impact

- `web/scripts/demo/plan.ts`, `load.ts` e o novo `png.ts` (a imagem fictícia), com os testes de `web/scripts/demo`.
- Nenhuma mudança na API nem nas telas.
