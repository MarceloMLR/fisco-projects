---
status: aceita (apresentação da conversa no ADR-0069, que substitui o "arrastando e soltando" da abertura)
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0026: Mensagens dos chamados visíveis a todos que veem o chamado, com anexos e menções

## Contexto e problema

No MVP, qualquer pessoa que vê o chamado manda mensagens e anexos de até 15 MB, e tudo fica visível para todos. Com times e filas (ADR-0023), avaliou-se a necessidade de notas internas, visíveis só para quem atende. Como os clientes não acessam os chamados na v1 (ADR-0007), as pessoas que veem um chamado são da própria empresa: o solicitante, quem atende e os gestores do projeto. Como funciona a conversa do chamado?

## Fatores de decisão

- Histórico completo e simples dentro do chamado.
- Clientes não acessam os chamados na v1.
- Evitar regras de visibilidade desnecessárias.

## Opções consideradas

- A. Todas as mensagens visíveis para todos que veem o chamado
- B. Mensagens públicas e notas internas visíveis só para quem atende e para o gerente do projeto

## Resultado da decisão

Opção escolhida: **A. Todas as mensagens visíveis para todos que veem o chamado**, porque, sem clientes no sistema, quem vê o chamado já é a equipe envolvida, e as notas internas só acrescentariam regras.

Escopo:

| Item | Classificação |
| --- | --- |
| Mensagens visíveis para todos que veem o chamado (quem vê é definido na matriz de permissões; indicação de Marcelo: solicitante, quem atende e gestores do projeto) | v1 |
| Mensagens com formatação básica (negrito, listas, links) | v1 |
| Anexos na abertura e nas mensagens, arrastando e soltando, até 15 MB por arquivo (configurável) | v1 (na abertura, substituído pelo ADR-0069: só o botão "Escolher arquivos", com vários arquivos de uma vez) |
| Bloqueio de arquivos executáveis e outros tipos perigosos | v1 |
| Visualizar imagens e PDF no navegador | v1 |
| Acompanhar um chamado: solicitante e responsável automaticamente; outras pessoas com acesso por escolha | v1 |
| Mencionar com @nome: os gestores do projeto podem ser mencionados em qualquer chamado do projeto; as demais pessoas, só se estiverem envolvidas no chamado (solicitante e quem atende o destino). Quem é mencionado passa a acompanhar e é avisado por e-mail | v1 |
| Mencionar um grupo inteiro (por exemplo, @Técnico ou @TI) | depois (com o ADR-0068, os grupos seriam @Funcional e @Desenvolvimento) |
| Notas internas | depois (necessárias se clientes passarem a acessar os chamados, ADR-0007) |
| Editar ou apagar mensagens já enviadas | depois |
| Responder por e-mail, com a resposta virando mensagem no chamado | depois |

O local onde os anexos são guardados será decidido no bloco de documentos. A forma do aviso de menção (e um possível sino de notificações ou aviso pelo Teams) será decidida no bloco de e-mail e notificações.

### Consequências

- Bom, porque toda a conversa fica no chamado, sem regras de visibilidade por mensagem.
- Bom, porque a menção chama gestores e envolvidos sem expor o chamado a quem não deveria vê-lo.
- Ruim, porque, se clientes passarem a acessar os chamados, será preciso acrescentar notas internas.
- Neutro, porque, sem notificações dentro do sistema na v1, a menção avisa só por e-mail.

### Confirmação

Testes cobrindo: toda mensagem aparece para todos que veem o chamado; anexo acima do limite ou de tipo bloqueado é recusado; a lista de menção mostra os gestores do projeto e os envolvidos no chamado, e ninguém além deles; quem é mencionado passa a acompanhar o chamado; mensagens enviadas não podem ser editadas nem apagadas.

## Prós e contras das opções

### A. Tudo visível para quem vê o chamado

- Bom, porque é simples e transparente.
- Ruim, porque não há espaço reservado para conversas só entre quem atende.

### B. Mensagens públicas e notas internas

- Bom, porque separa a conversa interna.
- Ruim, porque acrescenta regras e risco de erro de visibilidade sem necessidade enquanto clientes não acessam o sistema.
