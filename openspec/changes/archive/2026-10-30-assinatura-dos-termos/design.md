## Context

Os termos de aceite moram no módulo `Documents` (Documentos-5, ADR-0095): `acceptance_terms` com a pessoa do pedido (`RecipientId`), o termo a assinar guardado em `Pedidos de aceite` da pasta do projeto, o histórico em `acceptance_events`, as regras de quem faz o quê em `AcceptanceAccess`, o termo assinado como documento Aprovado da pasta de termos (`DocumentArea.OwnTerms` para quem assinou) e o marco atingido pelo `IScheduleMilestones`. O pedido e o lembrete vão por e-mail pelo `INoticeBoard`; o Meu trabalho traz os pedidos pelo `AcceptanceWorkSource`, e o resumo diário, os pendentes da pessoa pelo `IPendingAcceptances`.

O ADR-0129 (proposto) troca a resposta da pessoa do projeto pela assinatura de signatários digitados num provedor trocável, deixa os termos só com a gestão e guarda o e-mail dos signatários só enquanto o pedido está aberto. Esta change entrega o contrato do provedor, o provedor local e as telas; a Autentique vem na Assinatura-2.

## Goals / Non-Goals

**Goals:**

- Um contrato de provedor pequeno, com os mesmos testes para cada implementação, como o `FileStorageContract` do `Files`.
- A situação de cada pedido aplicada por um caso de uso só, chamado pela conferência em segundo plano, pela página local e, depois, pelo aviso do provedor, sem gravar duas vezes.
- O e-mail dos signatários fora do banco assim que o pedido fecha.
- A demonstração e os testes sem provedor externo.

**Non-Goals:**

- A Autentique, o aviso do provedor (webhook), o Key Vault e o guia (Assinatura-2).
- Ordem de assinatura, identificação por SMS ou certificado e modelos de termo (ADR-0129, depois).

## Decisions

### Dados

- `acceptance_terms` ganha o provedor (`SignatureProvider`, texto, vazio no termo anexado e nos pedidos anteriores) e o documento no provedor (`ProviderDocumentId`), e o `RecipientId` passa a ser opcional (só os termos anteriores o têm). A situação continua Pendente, Assinado ou Recusado, e o termo ganha um token de concorrência (`rowversion`), como o chamado.
- `acceptance_signers`: `Id`, `TermId`, `Name` (até 120), `Email` (até 254, vazio quando o pedido fecha), `Status` (Pendente, Assinou, Recusou), `RespondedAt`, `RefusalReason` (até 1.000) e `ProviderSignerId`. Na auditoria, o rótulo é o nome, o termo é a referência e o contexto, o e-mail é segredo (ADR-0039) e o id no provedor fica fora.
- Os signatários guardam só o nome e o e-mail digitados, sem vínculo com usuário: a anonimização não os alcança, e o e-mail some ao fechar o pedido (D2 do ADR-0129).

Alternativa descartada: os signatários em JSON no termo. Seria uma tabela a menos, mas a auditoria e o apagamento do e-mail ficam mais simples com uma linha por signatário.

### Contrato do provedor

`ISignatureProvider` (Application do `Documents`):

| Operação | O quê |
| --- | --- |
| `SendAsync(request)` | Manda o termo (título, mensagem, nome e conteúdo do PDF) com os signatários (nome e e-mail); devolve o id do documento e o id de cada signatário |
| `StatusAsync(documentId)` | A situação de cada signatário (pendente, assinou com a hora, recusou com o motivo) |
| `RemindAsync(documentId)` | Manda o link de novo a quem não assinou |
| `CancelAsync(documentId)` | Cancela o documento aberto |
| `SignedFileAsync(documentId)` | O PDF assinado, com o nome |
| `DeleteAsync(documentId)` | Exclui o documento fechado, quando o provedor permite |

Falha do provedor vira `SignatureProviderException` (sem dados pessoais na mensagem) e, na API, 502. A implementação é escolhida por `Signature:Provider` (`Local` no desenvolvimento; vazio, sem provedor). Os testes do contrato (`SignatureProviderContract`) valem para cada implementação, como o `FileStorageContract`.

### Situação dos pedidos

Um caso de uso só, `ApplySignatureStatus`, recebe o pedido, pergunta ao provedor a situação e aplica o que mudou: o signatário que assinou ou recusou entra no histórico; a recusa recusa o termo e cancela o documento; com todos assinados, baixa o PDF assinado, grava o documento Aprovado na pasta de termos, marca o marco atingido e avisa a gestão. Cada signatário só sai de Pendente uma vez, o termo só fecha uma vez, e o token de concorrência faz a segunda gravação simultânea desistir sem erro (a próxima conferência acha o pedido já fechado).

Quem chama:

- `SignatureCheck`, em segundo plano (ADR-0047), a cada `Signature:CheckMinutes` (15), confere os pedidos abertos de todos os projetos em aberto.
- A página local, logo depois de cada assinatura ou recusa.
- O aviso do provedor, na Assinatura-2.

Alternativa descartada: aplicar o conteúdo do aviso do provedor direto. O aviso só dispara a conferência: a situação vem sempre do provedor, e um aviso forjado não muda nada.

### Provedor local

O `LocalSignatureProvider` guarda cada documento numa pasta (`Signature:Local:Folder`, `App_Data/signatures`, fora do Git), como os e-mails locais: o PDF e um JSON com os signatários, a situação e um código aleatório de cada um (32 bytes, base64url). A página `/sign/:token` (fora da estrutura das telas, sem sessão) mostra o título, o nome do signatário, o termo e "Assinar" e "Recusar" com o motivo, pelas rotas `GET /api/local-signatures/{token}`, `GET .../document`, `POST .../sign` e `POST .../refuse`, sem sessão; sem o provedor local, elas respondem 404 (ficam no contrato da API, que é gerado sem ele). O PDF "assinado" é o próprio termo. A aplicação não sobe em produção com o provedor local (como o `EmailOptionsValidation`).

O provedor local não manda e-mail: a gaveta mostra o link de cada signatário pendente (`signingLink`, só com o provedor local), para a demonstração e os testes. O provedor de verdade manda o e-mail ele mesmo.

### API

- `POST /api/projects/{id}/acceptances` (multipart): o marco, o título, a mensagem, o PDF e os signatários (`signers[i].name`, `signers[i].email`); 409 sem provedor, 502 com o provedor recusando.
- `POST .../acceptances/attached`: o termo anexado sem pedido, sem pessoa (`recipientId` sai).
- `POST .../acceptances/{termId}/attached`, `.../reminder` e `DELETE .../{termId}` continuam, agora pelo provedor; o lembrete de um pedido anterior responde 409.
- `GET .../acceptances` e `GET .../acceptances/{termId}` trazem os signatários (nome, situação, quando, motivo e o e-mail só no pedido aberto) e, no termo anterior, a pessoa.
- Saem as rotas de resposta da pessoa (o PDF assinado e a recusa) e a área `OwnTerms` dos documentos.

### Permissões

`AcceptanceAccess` fica só com a gestão: o administrador e o gerente veem e fazem tudo; os outros papéis recebem 403; quem não está no projeto, 404; projeto encerrado, 409. A pasta de termos e os termos assinados ficam só da gestão (`DocumentAccess`). A matriz dos testes de permissão é atualizada (ADR-0056).

### Avisos, Meu trabalho e resumo

- O pedido e o lembrete deixam de gerar aviso (o provedor manda o e-mail). O termo assinado e a recusa continuam pelo `INoticeBoard` (`AcceptanceSigned`, `AcceptanceRefused`), para quem pediu e o gerente. Os tipos `AcceptanceRequested` e `AcceptanceReminded` ficam no enum, para o sino mostrar as notificações antigas.
- O `AcceptanceWorkSource` deixa de trazer o pedido para a pessoa responder e continua com os pedidos pendentes que ela fez ("Lembrar").
- O `IPendingAcceptances` (pedidos pendentes da pessoa no resumo diário) sai, com o uso no `Notifications`.

### Exclusão do projeto

O `DocumentProjectDeletion` cancela no provedor os documentos dos pedidos abertos e pede a exclusão dos fechados, antes de apagar as linhas; a falha vai para o log, sem dados pessoais, e não impede a exclusão (ADR-0111).

### Telas

- `AcceptanceDialog`: "Pedir assinatura" com o marco, o título, a mensagem, o PDF e os signatários em linhas de nome e e-mail (React Hook Form com `useFieldArray`, de 1 a 10, "Incluir signatário" e o botão de tirar), e "Anexar termo assinado" sem a pessoa. Sem provedor (a lista diz pelo `canRequest`), a aba mostra só "Anexar termo assinado".
- A lista troca "Para" por "Signatários" ("2 de 3 assinaram", com os nomes na linha de baixo), e a aba aparece só para a gestão.
- A gaveta (`AcceptanceDrawer`) ganha a seção Signatários, cada um com a situação por ícone, cor e texto, o e-mail enquanto o pedido está aberto e, com o provedor local, o link "Abrir assinatura"; sai a resposta da pessoa.
- `LocalSigningPage` (`/sign/:token`), fora da estrutura, sempre clara, como a página de impressão.
- As telas novas não estão no canvas; vão nas capturas do PR para Marcelo conferir, ou num canvas antes, se ele pedir.

### Demonstração

O plano troca a pessoa do pedido por signatários fictícios do cliente (`@cliente.example`). A carga pede a assinatura pelo provedor local e assina ou recusa pelas rotas locais, com o link que o detalhe do termo traz: um termo assinado pelos dois signatários, um recusado, um pendente com um dos dois assinado, e o anexado pela gerente.

## Risks / Trade-offs

- [Os termos passam a depender de um serviço externo] → O contrato isola o provedor; sem provedor, a gestão anexa o termo recebido por fora, como hoje.
- [A conferência em segundo plano faz chamadas ao provedor] → Só os pedidos abertos de projetos em aberto, a cada 15 minutos; o aviso do provedor (Assinatura-2) deixa a conferência só de garantia.
- [Duas vias aplicando a mesma assinatura] → Cada mudança só acontece a partir de Pendente, com o token de concorrência no termo.
- [Rotas sem sessão do provedor local] → Respondem 404 sem o provedor local, que não sobe em produção; cada link vale para um signatário e um pedido aberto.
- [A pessoa do projeto perde o termo que via] → Decisão de Marcelo (ADR-0129); os termos anteriores continuam na pasta de termos, para a gestão.

## Migration Plan

A migração cria `acceptance_signers`, acrescenta o provedor, o documento no provedor e o token de concorrência em `acceptance_terms` e torna o `RecipientId` opcional. Os termos existentes não mudam: ficam com a pessoa do pedido e sem provedor. Voltar a versão deixa as colunas novas sem uso, e os pedidos feitos pelo provedor ficam sem a pessoa (ADR-0125).
