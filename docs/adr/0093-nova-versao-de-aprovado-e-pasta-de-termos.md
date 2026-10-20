---
status: aceita
date: 2026-10-20
decision-makers: Marcelo Ribeiro
---

# ADR-0093: Nova versão de um documento aprovado volta para revisão, e a pasta de termos recebe só os termos assinados

Detalha o ADR-0031 e o ADR-0045 em dois pontos que eles não decidem.

## Contexto e problema

O ADR-0031 decidiu o status manual (Rascunho, Em revisão, Aprovado e Obsoleto) e a nova versão para o arquivo com o mesmo nome na mesma pasta. O ADR-0045 deixou a aprovação só com o administrador, o gerente e o líder técnico, e o envio de novas versões com todos, menos o leitor.

Ficam duas perguntas:

- Um colaborador pode enviar uma nova versão de um documento aprovado. Se o status não mudar, o conteúdo novo aparece como aprovado sem ninguém ter aprovado. O que acontece com o status na nova versão?
- A pasta de termos de aceite é uma das pastas padrão (ADR-0031). Os termos assinados chegam a ela pelo fluxo dos termos (ADR-0035) e não podem ser excluídos. Quem envia arquivos para ela pela área de documentos?

## Fatores de decisão

- O status Aprovado vale para o conteúdo que foi aprovado.
- Só o administrador, o gerente e o líder técnico aprovam (ADR-0045).
- A pasta de termos só tem o que o fluxo dos termos registrou, com a mesma visibilidade dos termos (ADR-0045).
- Regras simples, sem fluxo de aprovação (ADR-0031).

## Opções consideradas

Status na nova versão:

- A1. O status não muda
- A2. Toda nova versão volta para Rascunho
- A3. A nova versão de um documento aprovado volta para Em revisão; os outros status não mudam

Envio na pasta de termos:

- B1. Quem envia documentos envia também na pasta de termos
- B2. Só o administrador e o gerente enviam na pasta de termos
- B3. A pasta de termos recebe só os termos assinados, pelo fluxo dos termos de aceite

## Resultado da decisão

Opções escolhidas:

- **A3.** A aprovação era da versão anterior, e o conteúdo novo precisa ser revisto. Os outros status são decisões manuais de quem trabalha no documento e continuam como estão.
- **B3.** A pasta só tem termos assinados, todos com o marco e o registro de quem enviou. O termo recebido por fora entra pelo "Anexar termo assinado" dos termos de aceite (ADR-0035).

Regras:

- **Nova versão:** com o documento Aprovado, a nova versão o deixa Em revisão, com quem enviou e quando no registro do status. Rascunho, Em revisão e Obsoleto continuam como estão.
- **Pasta de termos:**
  - na área de documentos, ela é só leitura: não recebe arquivos nem novas versões;
  - os documentos dela são os termos assinados, aprovados e vinculados ao marco;
  - não mudam de status e não podem ser excluídos (ADR-0031);
  - ela aparece para o administrador e o gerente, com todos os termos, e para quem assinou um termo, só com o dele (ADR-0045).

### Consequências

- Bom, porque nenhum conteúdo aparece aprovado sem ter sido aprovado.
- Bom, porque a pasta de termos só tem o que o fluxo dos termos registrou.
- Ruim, porque o gestor precisa aprovar de novo cada nova versão de um documento aprovado.
- Ruim, porque um termo antigo, assinado antes do sistema, só entra pelo "Anexar termo assinado", ligado a um marco.

### Confirmação

Testes cobrindo:

- a nova versão de um documento aprovado fica Em revisão;
- a nova versão de um documento em outro status não muda o status;
- o envio na pasta de termos é recusado;
- o termo assinado não muda de status e não é excluído.

## Prós e contras das opções

### A1. O status não muda

- Bom, porque é o mais simples.
- Ruim, porque o conteúdo novo aparece como aprovado sem aprovação.

### A2. Toda nova versão volta para Rascunho

- Bom, porque é uma regra só.
- Ruim, porque o documento Em revisão volta para Rascunho e o Obsoleto volta à lista sem ninguém pedir.

### A3. Aprovado volta para Em revisão

- Bom, porque protege a aprovação e não mexe nos outros status.
- Ruim, porque é uma regra a mais para explicar.

### B1. Todos enviam na pasta de termos

- Bom, porque é igual às outras pastas.
- Ruim, porque quem envia não vê a pasta (ADR-0045), e a pasta passaria a ter documentos que não são termos.

### B2. Administrador e gerente enviam na pasta de termos

- Bom, porque cobre termos antigos sem marco.
- Ruim, porque a pasta passa a ter termos sem marco e sem o registro do aceite.

### B3. Só os termos assinados, pelo fluxo dos termos

- Bom, porque todo documento da pasta tem o marco e o registro de quem enviou.
- Ruim, porque o termo antigo precisa de um marco para entrar.
