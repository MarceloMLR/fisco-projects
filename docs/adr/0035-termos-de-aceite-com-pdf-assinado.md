---
status: aceita
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0035: Termos de aceite sempre por PDF assinado eletronicamente fora do sistema

## Contexto e problema

No MVP, o gestor pede o aceite de um marco a uma pessoa do projeto, que pode aceitar eletronicamente (o sistema registra nome, data e hora), aceitar enviando o termo assinado em PDF ou recusar com motivo. O gestor também pode exigir o termo assinado e anexar um termo recebido por fora, o que cobre clientes externos, que não acessam o sistema (ADR-0007). Quanta força o aceite precisa ter?

## Fatores de decisão

- Aceite com força formal, sem depender de um clique dentro do sistema.
- Não depender de plataformas de assinatura pagas nem de integrações específicas.
- Funcionar também para quem não acessa o sistema.

## Opções consideradas

- A. Como no MVP (aceite eletrônico por clique ou PDF assinado), com registro reforçado do aceite eletrônico
- B. A mais integração com plataforma de assinatura eletrônica (por exemplo, Clicksign ou DocuSign)
- C. Só termo assinado em PDF, sem aceite eletrônico por clique

## Resultado da decisão

Opção escolhida: **C. Só termo assinado em PDF**, por decisão de Marcelo: a pessoa assina o PDF eletronicamente, com a ferramenta que preferir, e envia o arquivo assinado no aceite.

Escopo:

| Item | Classificação |
| --- | --- |
| Pedido de aceite ligado a um marco, para uma pessoa do projeto, com título, mensagem e o termo em PDF a ser assinado | v1 |
| A pessoa assina o PDF fora do sistema (por exemplo, assinatura gov.br, certificado digital ou outra ferramenta) e envia o PDF assinado no aceite | v1 |
| Recusar com motivo | v1 |
| Registro de quem enviou o termo assinado e quando | v1 |
| O gestor anexa um termo assinado recebido por fora, em nome do responsável (para clientes externos) | v1 |
| Termo aceito marca o marco como atingido e muda a cor dele no Gantt; a recusa mantém o marco em aberto e avisa o gestor | v1 |
| Termo assinado salvo na pasta de termos do projeto, vinculado ao marco, sem poder ser excluído (ADR-0031) | v1 |
| Aceite eletrônico por clique dentro do sistema | fora |
| Validar automaticamente a assinatura digital do PDF | depois |
| Vários aprovadores no mesmo termo | depois |
| Gerar o PDF do termo a partir de um modelo | depois |
| Integração com plataforma de assinatura eletrônica | depois |

Os lembretes de aceite pendente serão decididos no bloco de e-mail.

### Consequências

- Bom, porque todo aceite tem um documento assinado, com a força da ferramenta de assinatura usada.
- Bom, porque o mesmo fluxo vale para quem acessa o sistema e para clientes externos (pelo gestor).
- Ruim, porque aceites simples também exigem assinar e enviar um PDF.
- Ruim, porque, na v1, o sistema não confere se o PDF tem de fato uma assinatura válida.

### Confirmação

Testes cobrindo: o aceite só é registrado com o envio de um PDF; a recusa exige motivo; o aceite marca o marco como atingido e a recusa não; o termo assinado vai para a pasta de termos vinculado ao marco e não pode ser excluído; o gestor consegue anexar o termo em nome do responsável, com registro de quem anexou.

## Prós e contras das opções

### A. Como no MVP, com registro reforçado

- Bom, porque aceites simples são rápidos.
- Ruim, porque o aceite por clique tem força formal menor.

### B. Integração com plataforma de assinatura

- Bom, porque a assinatura tem força formal maior e fica dentro do fluxo.
- Ruim, porque tem custo por assinatura e integração específica por plataforma.

### C. Só PDF assinado

- Bom, porque todo aceite é formal e independe de plataforma.
- Ruim, porque torna mais trabalhosos os aceites simples.

## Mais informações

- A validade jurídica de cada forma de assinatura deve ser confirmada com um advogado; esta decisão não é parecer jurídico.
