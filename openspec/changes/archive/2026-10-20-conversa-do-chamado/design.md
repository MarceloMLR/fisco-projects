# Design: conversa do chamado (Chamados-2)

## Context

- **O que já existe:** a Chamados-1 criou o módulo `Tickets` com a abertura, a lista e uma página simples do chamado.
- **ADRs:**
  - o ADR-0026 decide o conteúdo da conversa;
  - o ADR-0069 decide a apresentação;
  - o ADR-0030 decide onde ficam os anexos.
- **Canvas:** a tela `Chamado` desenhou a conversa com a descrição no topo, as mensagens em duas colunas, a caixa de resposta fixa e o painel lateral. Os tokens `msg-*`, `role-*` e `avatar-*` já estão no `tokens.json`.
- **Editor:** o `CLAUDE.md` prevê o Tiptap no editor dos chamados.

## Goals / Non-Goals

**Goals:**

- Conversar no chamado com formatação, menções e anexos, com as regras de acesso do ADR-0044.
- Página do chamado no desenho aprovado.

**Non-Goals:** os da proposta (status, eventos, e-mail da menção, pasta do projeto escolhida, notas internas).

## Decisions

### 1. Texto formatado em JSON do Tiptap, validado no servidor

- **Formato:** o texto vai e volta como o documento do Tiptap (ProseMirror) em JSON, guardado como texto no banco.
- **Validação (`RichText.Parse`, no domínio):**
  - aceita só `doc`, `paragraph`, `text`, `hardBreak`, `bulletList`, `orderedList`, `listItem` e `mention` (com `id` e `label`);
  - nas marcas, aceita só `bold` e `link` com endereço `http`, `https` ou `mailto`;
  - qualquer outra coisa recusa a mensagem com 400;
  - um texto vazio também é recusado, e o limite é 20.000 caracteres de texto.
- **Texto simples:** o servidor guarda também o texto simples, para a busca e para o e-mail dos avisos.
- **Na tela:** o `RichText` monta os elementos do React a partir do JSON, nó por nó, sem HTML. O XSS fica fora por construção.
- **Alternativa:** guardar HTML e limpar com uma biblioteca. Recusada: mais uma dependência, e o HTML limpo ainda precisaria ser lido de volta para as menções.

### 2. Mensagens, anexos e quem acompanha

- **`TicketMessage`:** o chamado, o autor, o texto (JSON e simples) e quando foi mandada. Não muda depois de gravada.
- **`TicketAttachment`:** o chamado, a mensagem (nula na descrição), o arquivo no `IFileStorage`, o nome, o tamanho, o tipo, quem enviou e quando.
- **`TicketFollower`:** o chamado e a pessoa, uma linha por par.
- **Auditoria:** mensagens, anexos e quem acompanha ficam fora dela. A conversa já é o histórico e não muda, como a `ScheduleChange`, e registrar cada mensagem encheria a tela de Auditoria.
- **Atualização:** a abertura e cada mensagem atualizam o `UpdatedAt` do chamado.

### 3. Quem manda, quem é mencionado e quem acompanha (`TicketAccess`)

- **Mandar mensagem:** o administrador, o gerente, o líder técnico, quem atende o time do chamado (`Serves`) e o solicitante.
- **Mencionáveis:**
  - os gestores (gerente e líder técnico);
  - os envolvidos: o solicitante, quem atende o time e o responsável;
  - nunca quem está fora do projeto.
- **Acompanhar:**
  - o solicitante acompanha ao abrir o chamado, e quem é mencionado passa a acompanhar ao ser mencionado;
  - quem vê o chamado entra e sai por escolha (`PUT` e `DELETE /api/tickets/{number}/following`).

### 4. Anexos

- **Envio:** os anexos vão no mesmo pedido da mensagem, em `multipart/form-data`, com o texto no campo `body` e os arquivos em `files`.
- **Anexos da descrição:** vão em `POST /api/tickets/{number}/attachments`, logo depois da abertura. Só o solicitante os envia, e só enquanto o chamado não tem mensagens. Se o envio falha, o chamado existe sem os anexos, e a tela avisa.
- **Limite:** `Tickets:Attachments:MaxMegabytes` (15). Cada mensagem leva até 10 arquivos, e o pedido inteiro aceita até 80 MB.
- **Tipos recusados:**
  - por extensão: `.exe`, `.dll`, `.msi`, `.bat`, `.cmd`, `.com`, `.scr`, `.ps1`, `.vbs`, `.js`, `.jar`, `.sh`, `.app` e outros;
  - pelo começo do arquivo: `MZ` (executável do Windows) e `\x7fELF`.
- **Pasta:**
  - `{código} - {nome}/Chamados/#{número}` na raiz configurada (ADR-0030), criada na primeira vez e guardada no chamado (`AttachmentFolderId`);
  - quando a área de documentos ligar o projeto a uma pasta escolhida, os chamados novos passam a usá-la.
- **Abrir um anexo:**
  - `GET /api/tickets/{number}/attachments/{id}`, só para quem vê o chamado;
  - imagens e PDF vão `inline`, e o resto vai como download, com `X-Content-Type-Options: nosniff` e `Content-Security-Policy: sandbox`.

### 5. A página do chamado no canvas

- **Cabeçalho:** número, código e time, título, status, prioridade e "Aberto por".
- **Faixa "Conversa":** mostra o número de interações. O filtro "Tudo / Mensagens / Histórico" vem com os eventos, na Chamados-3.
- **A conversa:**
  - rola por dentro, com a descrição no topo (`fp-desc`) e as mensagens (`fp-message`);
  - o solicitante fica à esquerda e os demais à direita;
  - os tons são `requester`, `owner`, `manager`, `techlead`, `dev` e `functional`. O tom do responsável vale sobre o do time;
  - cada mensagem tem o selo do papel e "(você)";
  - um separador por dia, com "Hoje" e "Ontem";
  - acima de 10 interações, as antigas ficam em "Mostrar N interações anteriores".
- **Resposta:** fica fixa embaixo, com o editor, "Escolher arquivos" e "Enviar", só para quem manda mensagem.
- **Painel:**
  - "Pessoas", com as cores de cada papel, "Acompanham" e o botão "Acompanhar" ou "Deixar de acompanhar";
  - "Anexos da conversa";
  - o projeto e a última atualização.
- **Papel de cada autor:** a API devolve o papel no chamado de cada pessoa da conversa (`TicketParticipantRole`), calculado no servidor no momento da leitura (ADR-0069: "a cor de uma pessoa muda se ela passar a ser a responsável").

### 6. Editor

- **Componente:** `RichTextEditor`, em `web/src/components`.
- **Tiptap 3.31.4:** `@tiptap/react`, `@tiptap/pm` e `@tiptap/starter-kit`, todos MIT. O `StarterKit` fica com parágrafos, negrito, listas, quebras, link e desfazer.
- **Menção:** um nó próprio, `mention`, inserido pelo botão "@", que abre a lista dos mencionáveis. A digitação de "@" com sugestão fica para depois.
- **Link:** o botão de link pede o endereço num diálogo pequeno.

## Risks / Trade-offs

- **[Tamanho do pacote]** O Tiptap acrescenta cerca de 100 KB. Mitigação: o editor só é carregado nas telas de chamado, sob demanda (`lazy`), como o Gantt.
- **[Anexos da abertura em dois pedidos]** A abertura e os anexos não são atômicos. Mitigação: só o solicitante anexa à descrição, logo depois da abertura, e a tela avisa se falhar.
