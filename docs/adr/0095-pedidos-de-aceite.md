---
status: aceita
date: 2026-10-20
decision-makers: Marcelo Ribeiro
---

# ADR-0095: Pedido de aceite cancelado sai da lista, recusa encerra o pedido e termo anexado sempre de uma pessoa do projeto

Detalha o ADR-0035 e o ADR-0045 nos pontos que eles não decidem.

## Contexto e problema

O ADR-0035 decidiu os termos de aceite da v1: o pedido ligado a um marco, para uma pessoa do projeto, com o termo em PDF; a resposta com o PDF assinado fora do sistema ou a recusa com motivo; e o termo assinado recebido por fora, anexado pelo gestor em nome do responsável. O ADR-0045 deixou os termos com o administrador e o gerente, e a resposta com a pessoa a quem o pedido foi endereçado. O canvas ProjetoTermos mostra a lista com Pendentes, Assinados e Recusados, o histórico de cada pedido, "Lembrar" e "Cancelar pedido".

Ficam três perguntas:

- O que acontece com um pedido pendente cancelado?
- Depois de uma recusa, o mesmo pedido pode ser respondido de novo?
- O termo assinado recebido por fora, sem pedido no sistema, fica em nome de quem? O cliente externo não entra no sistema (ADR-0007).

## Fatores de decisão

- A lista de termos mostra o que vale: o que está pendente, assinado ou recusado.
- Cada pedido tem uma resposta só, com o registro de quem respondeu e quando.
- Não guardar dados pessoais de quem não está no sistema sem necessidade (ADR-0058).
- Quem recebeu o termo vê o próprio termo (ADR-0045).

## Opções consideradas

Pedido cancelado:

- A1. O pedido é excluído, com o termo a assinar; a auditoria guarda a exclusão
- A2. O pedido fica Cancelado na lista, com o histórico

Depois da recusa:

- B1. O pedido fica Recusado; para pedir de novo, o gestor faz um pedido novo
- B2. O gestor reabre o pedido recusado, que volta a Pendente

Termo recebido por fora, sem pedido:

- C1. O gestor escolhe uma pessoa do projeto como responsável pelo termo
- C2. O gestor digita o nome de quem assinou, de fora do sistema
- C3. O termo fica sem pessoa, só com quem anexou

## Resultado da decisão

Opções escolhidas:

- **A1.** O pedido cancelado não teve resposta e não vale para o marco; mantê-lo na lista mistura o que vale com o que foi desistido. A auditoria registra a exclusão, com quem cancelou e quando.
- **B1.** A recusa é a resposta do pedido, com o motivo. Um pedido novo, com o termo corrigido, deixa as duas respostas na lista.
- **C1.** O termo continua de uma pessoa do projeto, que o vê como os demais termos dela, e o sistema não guarda o nome de quem está fora dele. Quem assinou de fato está no próprio PDF.

Regras:

- **Pedido:** só o administrador e o gerente pedem, para qualquer pessoa do projeto, em qualquer papel (ADR-0045), com o marco, o título, a mensagem e o termo em PDF.
- **Resposta:** só a pessoa do pedido responde, uma vez: o PDF assinado (só PDF) ou a recusa com o motivo.
- **Em nome:** o administrador e o gerente anexam o termo assinado num pedido pendente, em nome de quem o recebeu, ou sem pedido, escolhendo o marco, o título e a pessoa do projeto responsável. O registro guarda quem anexou.
- **Termo assinado:** vira um documento Aprovado na pasta de termos, vinculado ao marco, que não muda de status nem é excluído (ADR-0093), e o marco fica atingido no dia, se ainda não estava.
- **Lembrar:** o gestor manda de novo o aviso do pedido pendente, registrado no histórico. O resumo diário também traz os pedidos pendentes da pessoa (ADR-0037).
- **Avisos:** o pedido e o lembrete vão por e-mail na hora para a pessoa do pedido; o termo assinado e a recusa vão para o sino e para o resumo diário de quem pediu e do gerente do projeto (ADR-0037, ADR-0038).
- **Projeto encerrado:** só leitura também nos termos; os pedidos pendentes ficam como estão e saem do resumo diário.

### Consequências

- Bom, porque a lista e o histórico mostram só pedidos que valeram.
- Bom, porque todo termo tem uma pessoa do projeto, que o vê, e nenhum dado pessoal de fora do sistema é digitado.
- Ruim, porque o pedido cancelado só fica na auditoria.
- Ruim, porque o termo de um cliente externo aparece em nome de uma pessoa do projeto, e o nome de quem assinou fica só no PDF.

### Confirmação

Testes cobrindo: o cancelamento tira o pedido da lista e fica na auditoria; o pedido recusado não recebe outra resposta; o termo anexado sem pedido fica com a pessoa escolhida e com quem anexou.

## Prós e contras das opções

### A1. Pedido cancelado excluído

- Bom, porque a lista fica só com o que vale.
- Ruim, porque o histórico do pedido cancelado fica só na auditoria.

### A2. Pedido cancelado na lista

- Bom, porque o histórico fica junto.
- Ruim, porque a lista ganha um status que o canvas não prevê e que não vale para o marco.

### B1. Recusa encerra o pedido

- Bom, porque cada pedido tem uma resposta.
- Ruim, porque pedir de novo exige um pedido novo.

### B2. Reabrir o pedido recusado

- Bom, porque reaproveita o pedido.
- Ruim, porque o mesmo pedido teria várias respostas.

### C1. Uma pessoa do projeto responsável

- Bom, porque segue o pedido "para uma pessoa do projeto" do ADR-0035 e a visibilidade do ADR-0045.
- Ruim, porque quem assinou de fato aparece só no PDF.

### C2. Nome digitado de quem assinou

- Bom, porque mostra quem assinou.
- Ruim, porque guarda dado pessoal de quem não está no sistema, fora da anonimização do ADR-0058.

### C3. Sem pessoa

- Bom, porque é o mais simples.
- Ruim, porque o termo fica sem dono, e a lista mostra "Para" vazio.
