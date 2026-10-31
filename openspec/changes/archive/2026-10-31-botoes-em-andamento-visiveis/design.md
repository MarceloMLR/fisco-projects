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
- **Só depois de ir ao servidor.** O cliente da API conta as chamadas que faz (`api/activity.ts`); o `usePendingAction` compara a contagem antes da ação e no fim, e o `Form` passa o resultado aos botões de envio. Sem chamada (o formulário recusado pelas próprias regras, copiar), o botão volta na hora, para os erros aparecerem e o novo clique funcionar. O que a tela passa em `loading`, como uma mutação, conta como ida ao servidor. Alternativas descartadas: um limite de tempo (uma ação de menos de 50 ms não esperou), que dependia da velocidade da máquina, e contar as respostas vistas, que somava as leituras de fundo.
- **Botão de envio fora do formulário.** O `Form` com `id` publica o próprio envio, e o botão `type="submit"` ligado por `form=` (o Salvar no cabeçalho da página) o lê (`useFormSending`), no lugar do `loading={isSubmitting}`, que valia também para a recusa das regras da tela.
- **Sem clique enquanto mostra.** O clique é ignorado enquanto o botão aparece em andamento, como no andamento de verdade, para não mandar a ação de novo logo depois da resposta.
- **Os dois botões pelo `loading`.** Documentos passa `loading={progress !== undefined}` e mantém o rótulo com a contagem dos arquivos, que diz quanto falta no envio de vários; Horas dos projetos passa `loading={exporting}` e deixa o `disabled` só para a lista vazia.

## Risks / Trade-offs

- [Um botão que fica desabilitado depois da resposta (Salvar sem mais alterações) mostra o ícone até o meio segundo e só então fica cinza] → é o esperado: primeiro o sinal de que foi aceito, depois o estado novo.
- [Testes que clicam duas vezes seguidas no mesmo botão] → esperam o fim do andamento antes do segundo clique.
