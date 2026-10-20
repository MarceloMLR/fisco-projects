# Proposta: conversa do chamado (Chamados-2)

## Why

A Chamados-1 abriu os chamados, mas ninguém conversa neles ainda. Os ADRs já decidiram como é a conversa:
- **ADR-0026:**
  - as mensagens são visíveis a todos que veem o chamado e têm formatação básica;
  - há anexos de até 15 MB, sem tipos perigosos;
  - quem é mencionado passa a acompanhar o chamado, e qualquer pessoa com acesso pode acompanhar.
- **ADR-0069:** a conversa tem duas colunas, com uma cor e um selo por papel, rola por dentro, recolhe as interações antigas e reúne os anexos no painel lateral.
- **ADR-0044:** mandam mensagens o administrador, os gestores, quem atende e o solicitante; os outros membros só acompanham.

## What Changes

- **Mensagens** (`POST /api/tickets/{number}/messages`):
  - o texto tem negrito, listas e links, no editor Tiptap previsto no `CLAUDE.md`;
  - mensagens não se editam nem se apagam;
  - mandam o administrador, o gerente, o líder técnico, quem atende o time do chamado e o solicitante. Os demais recebem 403 (ADR-0044).
- **Menções:**
  - o "@" da barra oferece os gestores do projeto e os envolvidos no chamado: o solicitante, quem atende o time e o responsável (ADR-0026);
  - o servidor recusa menção a outra pessoa;
  - quem é mencionado passa a acompanhar o chamado. O aviso por e-mail vem com os avisos, no grupo 6.
- **Acompanhar:**
  - o solicitante acompanha desde a abertura, e quem é mencionado passa a acompanhar;
  - qualquer pessoa que vê o chamado pode acompanhar e deixar de acompanhar;
  - a lista ganha a visão "Que acompanho", com a contagem.
- **Anexos:**
  - nas mensagens e na abertura, pelo botão "Escolher arquivos", com vários de uma vez;
  - até 15 MB por arquivo, valor configurável em `Tickets:Attachments:MaxMegabytes`;
  - executáveis e scripts são recusados, pela extensão e pelo começo do arquivo;
  - os arquivos vão pelo `IFileStorage`, para a pasta do chamado (ADR-0030);
  - imagens e PDF abrem no navegador, e os outros tipos são baixados. Só quem vê o chamado abre os anexos dele.
- **Descrição com formatação:**
  - a abertura usa o mesmo editor;
  - as descrições já existentes, em texto simples, continuam aparecendo como estão.
- **Página do chamado no desenho do canvas (ADR-0069):**
  - o cabeçalho, a descrição no topo e a conversa em duas colunas, com a cor e o selo de cada papel e "(você)";
  - o separador por dia, as interações antigas recolhidas acima de 10, e a caixa de resposta fixa embaixo;
  - no painel ao lado, "Pessoas" (com as cores e quem acompanha), "Anexos da conversa", o projeto e a última atualização;
  - a lista mostra o número de interações de cada chamado.
- **Dados de demonstração:** mensagens com menções e um anexo nos chamados de exemplo.

## Non-goals

- **Fica para a Chamados-3:**
  - o responsável definido pela primeira resposta e as mudanças de status;
  - os eventos na conversa e o filtro "Tudo / Mensagens / Histórico", que só faz sentido com eventos.
- **Fica para outros grupos:**
  - o aviso por e-mail da menção e as notificações (grupo 6);
  - a pasta do projeto escolhida pelo administrador, na área de documentos (grupo 7). Até lá, o sistema cria a pasta com o código e o nome do projeto na raiz configurada.
- **Fica para depois (ADR-0026):** notas internas, editar ou apagar mensagens, responder por e-mail e mencionar um time inteiro.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `chamados`: mensagens, menções, acompanhar, anexos, a conversa na página do chamado e a visão "Que acompanho".
- `dados-de-demonstracao`: a conversa dos chamados de exemplo.

## Impact

- **Backend (`Tickets`):**
  - as entidades `TicketMessage`, `TicketAttachment` e `TicketFollower`, com a migração;
  - o texto formatado validado no domínio (`RichText`);
  - os casos de uso de mandar mensagem, anexar, acompanhar e baixar anexo, com as rotas;
  - a conversa e as permissões no `GET /api/tickets/{number}`.
- **Frontend:**
  - o editor `RichTextEditor` (Tiptap, MIT) e o `RichText`, que mostra o texto salvo sem HTML;
  - a página do chamado refeita no desenho do canvas;
  - o "Abrir chamado" com o editor e os anexos;
  - a visão "Que acompanho" e as interações na lista.
- **Docs:** `CLAUDE.md`, glossário e roadmap.
