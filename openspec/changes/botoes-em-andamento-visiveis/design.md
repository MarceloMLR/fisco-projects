## Context

A Fundação-5.5 pôs o andamento no `Button` e no `IconButton` de `web/src/components` (ADR-0131): pela promessa do clique (`usePendingAction`), pelo envio do `Form` em volta (`FormPending`) ou pelo `loading` passado pela tela. O andamento termina junto com a resposta, então, com o servidor do `dev` respondendo em pouco mais de 100 ms, o ícone aparece por um ou dois quadros. Dois botões ficaram de fora porque a tela os desabilita por um estado com outro nome: `disabled={progress !== undefined}` em Documentos e `disabled={exporting || ...}` em Horas dos projetos.

## Goals / Non-Goals

**Goals:**

- O ícone visível por pelo menos meio segundo, para todo botão em andamento, sem que cada tela faça nada.
- Os dois botões que ainda ficam cinza passam a ficar em andamento.

**Non-Goals:**

- Atrasar a ação, a resposta ou o fechamento do diálogo.

## Decisions

- **O tempo mínimo fica no botão, sobre o estado já combinado.** O `Button` e o `IconButton` calculam o andamento como hoje (`loading`, a promessa do clique e, no de envio, o `Form`) e passam o resultado por um `useMinimumBusy`, em `pending.ts`: quando o andamento começa, guarda a hora; quando termina antes de `minimumBusyMs` (500 ms), continua mostrando até completar, com um `setTimeout` limpo ao desmontar. Assim vale para as três fontes, num lugar só. Alternativa: atrasar o fim da promessa no `usePendingAction`, que não cobriria o `loading` nem o `Form`.
- **Sem clique enquanto mostra.** O clique é ignorado enquanto o botão aparece em andamento, como no andamento de verdade, para não mandar a ação de novo logo depois da resposta.
- **Os dois botões pelo `loading`.** Documentos passa `loading={progress !== undefined}` e mantém o rótulo com a contagem dos arquivos, que diz quanto falta no envio de vários; Horas dos projetos passa `loading={exporting}` e deixa o `disabled` só para a lista vazia.

## Risks / Trade-offs

- [Um botão que fica desabilitado depois da resposta (Salvar sem mais alterações) mostra o ícone até o meio segundo e só então fica cinza] → é o esperado: primeiro o sinal de que foi aceito, depois o estado novo.
- [Testes que clicam duas vezes seguidas no mesmo botão] → esperam o fim do andamento antes do segundo clique.
