## Context

A carga de demonstração (ADR-0089) entra como cada pessoa e faz pela API o que ela faria na tela. Hoje cada chamado do plano tem listas separadas (mensagens, mudanças de status, fornecedores, troca de pessoa e de time), carregadas nessa ordem fixa, o que não permite intercalar uma mensagem depois de uma atualização no fornecedor, nem reabrir um chamado resolvido e continuar a conversa.

## Goals / Non-Goals

**Goals:** chamados com histórias realistas em todos os projetos; todos os status; anexos de vários tipos; a orientação para recarregar.

**Non-Goals:** chamados vencidos (os chamados abrem no dia da carga, pela própria API); recarregar sem apagar o banco.

## Decisions

- **Passos em ordem.** O que acontece com um chamado vira uma lista de passos (`steps`), cada um com quem o faz: mensagem, ação do fluxo, prioridade, acompanhar, atribuir, transferir, registrar no fornecedor e atualizar no fornecedor. A carga percorre os passos na ordem. Alternativa descartada: manter as listas separadas e acrescentar mais, o que fixaria a ordem.
- **Anexos fictícios pela extensão.** O arquivo do plano tem o nome e as linhas: `.pdf` vira um PDF com as linhas (como os documentos), `.png` vira uma imagem pequena feita pelo comando (`png.ts`, com `node:zlib`, sem biblioteca), e o resto vira texto (XML, CSV, TXT). Nada real fica no repositório.
- **Anexos da descrição** são enviados por quem abriu o chamado, antes de qualquer passo.
- **Projeto concluído com chamados**: os chamados dele terminam fechados ou cancelados nos passos, e o status do projeto continua sendo aplicado depois dos chamados (ADR-0043).
- **Recarregar**: o comando continua sem repetir; a mensagem passa a dizer para apagar o banco local e os arquivos de `App_Data` e rodar de novo.

## Risks / Trade-offs

- **[Plano maior]** O plano cresce. Mitigação: as funções `say` e `act` encurtam os passos, e os testes conferem cada chamado contra as chamadas feitas.
