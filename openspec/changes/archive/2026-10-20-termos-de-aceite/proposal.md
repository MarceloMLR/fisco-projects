# Proposta: termos de aceite (Documentos-5)

## Why

O aceite das entregas de um projeto hoje é pedido por e-mail, e o termo assinado se perde entre anexos. O ADR-0035 decidiu os termos de aceite da v1, e o ADR-0045, quem faz o quê:

- **Pedido:** ligado a um marco, para uma pessoa do projeto, com título, mensagem e o termo em PDF a ser assinado.
- **Resposta:** a pessoa assina o PDF fora do sistema e envia o PDF assinado, ou recusa com motivo.
- **Termo recebido por fora:** o gestor anexa o termo assinado em nome do responsável.
- **Efeito:** o termo assinado vai para a pasta de termos, vinculado ao marco, sem poder ser excluído, e marca o marco como atingido; a recusa mantém o marco em aberto e avisa o gestor.
- **Quem vê:** o administrador e o gerente veem os termos; a pessoa do pedido vê o próprio.

O ADR-0095 (novo) decide o que os dois não decidem: o pedido cancelado sai da lista, a recusa encerra o pedido e o termo anexado sem pedido fica com uma pessoa do projeto.

## What Changes

- **Termos de aceite** (`acceptance_terms` e `acceptance_events`, no módulo `Documents`, ADR-0092):
  - pedir aceite: o marco, a pessoa, o título, a mensagem e o termo em PDF, guardado pelo `IFileStorage` na pasta `{código} - {nome}/Pedidos de aceite`;
  - responder: o termo assinado (só PDF) ou a recusa com motivo, uma vez, só pela pessoa do pedido, em qualquer papel;
  - anexar o termo assinado num pedido pendente, em nome da pessoa, ou sem pedido, com o marco, o título e a pessoa responsável;
  - lembrar a pessoa e cancelar o pedido pendente;
  - o histórico de cada termo (pedido, lembrete, assinado, anexado, recusado).
- **Termo assinado:**
  - vira um documento Aprovado na pasta de termos, vinculado ao marco, que não muda de status nem é excluído (ADR-0093);
  - o marco fica atingido no dia, por um contrato novo do `Projects` (`IScheduleMilestones`);
  - a pasta de termos passa a aparecer também para quem assinou um termo, só com o dele (ADR-0045).
- **Avisos:**
  - o pedido e o lembrete vão por e-mail na hora para a pessoa (ADR-0037);
  - o termo assinado e a recusa vão para o sino e para o resumo de quem pediu e do gerente;
  - o resumo diário traz os pedidos pendentes da pessoa, por um contrato novo do `Documents` (`IPendingAcceptances`).
- **Encerramento do projeto:** o resumo do encerramento e o diálogo "Concluir projeto" avisam os termos pendentes, sem impedir (ADR-0043), por um contrato novo do `Projects` (`IProjectAcceptances`).
- **Na tela:**
  - a aba "Termos de aceite" do projeto, como no canvas ProjetoTermos, para quem vê algum termo, com a lista, a situação, o filtro e "Pedir aceite" e "Anexar termo assinado";
  - a gaveta do termo (ADR-0081), com o marco, para quem, o pedido, a mensagem, o termo a assinar, o termo assinado, o histórico e as ações; a resposta da pessoa do pedido pela própria gaveta, também no celular, como no canvas CelularTermo.
- **Demonstração:** termos do ERP-FIN assinados, recusado e pendente.

## Capabilities

### New Capabilities

- `termos-de-aceite`: pedido, resposta, termo assinado, termo anexado, lembrar e cancelar, permissões e a tela.

### Modified Capabilities

- `documentos`: a pasta de termos aparece para quem assinou um termo, só com o dele.
- `projetos`: o resumo e o diálogo do encerramento avisam os termos pendentes.
- `avisos`: os avisos dos termos e os pedidos pendentes no resumo diário.
- `dados-de-demonstracao`: termos na demonstração.

## Impact

- **Backend:** o módulo `Documents`, com as tabelas `acceptance_terms` e `acceptance_events` e a migração; os contratos `IScheduleMilestones` e `IProjectAcceptances` no `Projects.Contracts` e `IPendingAcceptances` no `Documents.Contracts`; os tipos de aviso dos termos no `Notifications.Contracts`, com os textos dos e-mails.
- **Frontend:** a aba Termos de aceite, a gaveta do termo e os diálogos.
- **Documentação:** ADR-0095, CLAUDE.md, glossário e roadmap.

## Non-goals

- Aceite eletrônico por clique, validar a assinatura do PDF, vários aprovadores e gerar o termo de um modelo (fora ou depois, ADR-0035).
- Nome de quem assinou por fora (ADR-0095): fica só no PDF.
