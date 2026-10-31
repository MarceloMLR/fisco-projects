## Why

No teste da Fundação-5.5 no `dev`, em 30/10/2026, Marcelo não viu o ícone de carregamento, só o botão ficando cinza. Duas coisas aparecem: quando o servidor responde rápido, o ícone só pisca e a pessoa não chega a ver que o clique foi aceito; e dois botões ainda ficam cinza enquanto mandam algo ao servidor, "Enviar arquivos" (Documentos) e "Baixar planilha" (Horas dos projetos). Marcelo escolheu deixar o ícone visível por pelo menos meio segundo (ADR-0132). Correção da Fundação-5.5: Fundação-5.5.1.

## What Changes

- O botão em andamento fica com o ícone de carregamento por pelo menos meio segundo, mesmo quando a resposta chega antes; nesse tempo continua sem aceitar novo clique. A ação não espera: só o botão continua mostrando o ícone.
- "Enviar arquivos", na aba Documentos, fica em andamento enquanto envia, sem ficar cinza; continua dizendo quantos arquivos já foram ("Enviando 1 de 3…").
- "Baixar planilha", em Horas dos projetos, fica em andamento enquanto monta a planilha, sem ficar cinza.
- O design system diz o tempo mínimo no guia do Button.

## Capabilities

### New Capabilities

- Nenhuma.

### Modified Capabilities

- `estrutura-das-telas`: o tempo mínimo dos botões em andamento.

## Non-goals

- Segurar aberto o diálogo que fecha com a resposta: o tempo mínimo vale para o botão que continua na tela.
- Os itens dos menus "⋮" (ADR-0131).
- Mudar o indicador das listas, que continua depois de 300 ms (ADR-0080).

## Impact

- `web/src/components`: o `Button` e o `IconButton` (o tempo mínimo, num lugar só).
- `web/src/documents/ProjectDocumentsPage.tsx` e `web/src/admin/ProjectHoursPage.tsx`: os dois botões em andamento.
- Design system: o guia do Button.
- ADRs: 0131, 0132 (novo).
