---
status: aceita (fotos do Microsoft 365 substituído pelo ADR-0124)
date: 2026-10-26
decision-makers: Marcelo Ribeiro
---

# ADR-0118: Cartão da pessoa ao clicar no avatar

Complementa os ADRs 0062 a 0067 (identidade visual) e o ADR-0069 (cores dos papéis nos chamados).

## Contexto e problema

As telas mostram as pessoas pelo avatar com as iniciais e o nome ao lado. Com duas pessoas de mesmo nome, como as duas Marina Costa que Marcelo testou, não há como saber qual é qual fora da menção, que já mostra o e-mail (Chamados-2.5). Marcelo pediu que o clique na foto da pessoa abra um cartão pequeno com o nome, o e-mail e o resto que ajude a reconhecê-la. Como mostrar esse cartão?

## Fatores de decisão

- O mesmo cartão em todas as telas, que funcione no celular, onde não existe "passar o mouse".
- As listas não trazem o e-mail de cada pessoa: ele só é lido quando alguém abre o cartão.
- O e-mail das pessoas da empresa já aparece para quem tem sessão, na busca de pessoas (ADR-0005, ADR-0007); a pessoa anonimizada não tem mais e-mail (ADR-0110).
- Fotos do Microsoft 365 não são usadas na v1 (design system, Avatar). Substituído pelo ADR-0124: o avatar mostra a foto da pessoa no Microsoft 365.

## Opções consideradas

1. Cartão aberto pelo clique no avatar (ou Enter e espaço), com o nome e o e-mail lidos da API ao abrir (`GET /api/people/{id}`).
2. Cartão ao passar o mouse sobre o avatar.
3. O e-mail em toda resposta que traz uma pessoa, escrito ao lado do nome, sem cartão.

## Resultado da decisão

Opção 1. O avatar de outra pessoa vira um botão ("Ver o cartão de Marina Costa") que abre o cartão junto dele: o avatar maior, na cor do papel quando a tela usa as cores (ADR-0069), o nome, o papel da pessoa naquela tela quando a tela o conhece (no chamado, no projeto, no sistema na tela Usuários), o e-mail e "Copiar e-mail", que confirma no alto da tela (ADR-0109). Da pessoa anonimizada, o cartão diz "Nome e e-mail removidos", sem copiar. O cartão usa a superfície do Menu do design system (`bg-elevated`, borda, `radius-lg` e `shadow-menu`), sem sombra nova. O avatar da pessoa na sessão continua abrindo o menu da conta, e o do cabeçalho do detalhe da pessoa em Membros não abre o cartão, porque aquele detalhe já é o dela.

A opção 2 não funciona no toque e abre cartões sem a pessoa querer. A 3 encheria as listas de e-mails e mudaria todas as respostas.

### Consequências

- Boa, porque qualquer tela separa pessoas de mesmo nome com um clique, do mesmo jeito.
- Boa, porque o e-mail sai da API só quando alguém o pede, e o anonimizado continua sem e-mail.
- Neutra, porque o avatar, que era só decorativo, passa a ser um botão onde mostra outra pessoa.
