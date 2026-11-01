## Why

Num projeto fiscal, boa parte do atraso vem de fora: a tarefa espera o cliente mandar os arquivos do plano de contas, o marco depende da homologação na Sefaz. Quem abre o cronograma vê a tarefa atrasada e não sabe por quê, e o campo de notas dos arquivos do MS Project se perde na importação. GanttB-2, do grupo 15 (Aprimoramento do Gantt 2), a pedido de Marcelo; a forma e quem escreve ficam no ADR-0139.

## What Changes

- Cada tarefa (também a resumo e a subtarefa) e cada marco passa a ter, opcionalmente, uma nota de texto simples de até 2.000 caracteres, escrita, trocada e apagada por quem edita o cronograma (o administrador e o gerente) e vista por todos do projeto.
- O pedido de incluir e de editar item aceita `note`: ausente ou nula mantém a nota, vazia apaga; a resposta do `GET /schedule` traz `note` em cada item.
- A nota entra no desfazer e na auditoria; não muda datas nem progresso e fica fora da linha de base e dos modelos.
- MS Project: a exportação grava a nota no `Notes` da tarefa; a importação a traz, cortada em 2.000 caracteres com o aviso "notas longas cortadas".
- Na tela: o ícone de nota ao lado do nome na Lista e na tabela da Linha do tempo, com o texto ao apontar; a nota no detalhe e o campo "Notas" para quem edita; a coluna "Notas" no Excel.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: a nota de cada tarefa e marco na API, no desfazer, na auditoria, no MS Project, no Excel e nas telas.
- `dados-de-demonstracao`: notas em itens do projeto principal.

## Non-goals

- Conversa de notas com autor e data, menções e anexos (é o chamado, ADR-0026).
- Nota escrita pelo responsável da tarefa (ADR-0139, "depois").
- Texto formatado e nota na fase.
- Nota na página de impressão (PDF).

## Impact

- `src/Modules/Projects`: o campo no `ScheduleItem` (migração), no plano do item, no desfazer, nos pedidos e respostas, na exportação e na importação do MS Project; os testes.
- `web/src`: o campo no editor, a nota no detalhe, o ícone na Lista e na Linha do tempo, o Excel, a auditoria, os textos, os tipos gerados e os testes; a demonstração.
- Documentação: o ADR-0139, `CLAUDE.md`, `docs/glossario.md` e o roadmap (GanttB-2, ADR-0019).
