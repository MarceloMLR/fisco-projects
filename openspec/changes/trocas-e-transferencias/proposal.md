# Proposta: trocas e transferências (Chamados-5)

## Why

O responsável hoje só nasce da primeira resposta, e nada muda o solicitante nem o time de um chamado. Quem sai do projeto continua como responsável. Os ADRs já decidiram como corrigir:
- **ADR-0068:**
  - só o administrador, o gerente e o líder técnico trocam o responsável, um a um ou em lote;
  - o novo responsável é alguém que atende o time do chamado, ou o gerente do projeto;
  - a transferência entre Funcional e Desenvolvimento deixa o chamado sem responsável.
- **ADR-0027:**
  - o solicitante é trocado pelos gestores ou por ele mesmo, e o anterior continua acompanhando;
  - a lista permite marcar vários chamados e trocar o responsável ou o solicitante de todos.
- **ADR-0044:** a transferência é de quem atende e dos gestores, e a troca em lote, só do administrador e dos gestores.
- **ADR-0042:**
  - quem sai do projeto mostra os chamados que tem e permite passá-los a outra pessoa;
  - se ninguém passar os chamados, eles ficam sem responsável.

## What Changes

- **Trocar o responsável** (`PUT /api/tickets/assignee`):
  - com um ou vários números, só pelo administrador e pelos gestores;
  - o novo responsável sai de quem atende o time do chamado ou é o gerente do projeto, e passa a acompanhar;
  - a conversa registra a troca;
  - `GET /api/tickets/assignee-choices` devolve quem serve para todos os chamados escolhidos.
- **Trocar o solicitante** (`PUT /api/tickets/requester`):
  - um a um pelos gestores, pelo administrador e pelo próprio solicitante; em lote, só pelos gestores e pelo administrador;
  - o novo solicitante é alguém do projeto que abre chamados;
  - o anterior continua acompanhando, e a conversa registra a troca;
  - `GET /api/tickets/requester-choices` devolve as opções.
- **Transferir de time** (`PUT /api/tickets/{number}/team`):
  - por quem atende e pelos gestores, para o outro time, desde que ele tenha alguém para atender;
  - o chamado fica sem responsável, e a primeira resposta do novo time define o próximo;
  - a conversa registra a transferência.
- **Quem sai do projeto ou muda de papel:**
  - os chamados em aberto em que a pessoa é responsável e que ela não pode mais atender ficam sem responsável, com o evento na conversa;
  - o módulo Projects avisa os outros módulos pelo contrato `IProjectMemberChanges`, depois de gravar a mudança.
- **Lista:** filtro pela pessoa (`personId`, responsável ou solicitante) e, em cada chamado, o que a pessoa pode trocar (`canChangeAssignee`, `canChangeRequester`).
- **Na tela:**
  - a lista ganha a seleção, a barra "N selecionados", "Atribuir a…", "Trocar solicitante" e "Limpar seleção", como no canvas, e o link "Atribuir" embaixo de "Sem responsável";
  - o "⋮" do chamado ganha "Trocar responsável", "Trocar solicitante" e "Transferir para …";
  - o detalhe da pessoa na aba Membros mostra os chamados em aberto dela e "Passar pendências", e a confirmação da remoção avisa que os chamados ficam sem responsável.
- **Demonstração:** um chamado atribuído pelo gerente e um transferido de time.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `chamados`: troca do responsável e do solicitante, em lote, transferência de time e chamados de quem sai.
- `membros-do-projeto`: chamados no detalhe da pessoa e na remoção.
- `dados-de-demonstracao`: trocas na demonstração.

## Impact

- **Backend:**
  - módulo `Tickets`: os casos de uso, os eventos `AssigneeCleared`, `RequesterChanged` e `TeamChanged`, e os endpoints;
  - `Projects.Contracts`: o contrato `IProjectMemberChanges`, chamado pelo `Projects` ao remover ou trocar o papel.
- **Frontend:** lista de chamados, página do chamado e aba Membros.
- **Fora do escopo:**
  - avisos por e-mail das trocas (grupo 6);
  - ausência com substituto automático (ADR-0027, depois).
