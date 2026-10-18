---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0014: Tema claro, escuro e automático, com a preferência salva no perfil

## Contexto e problema

O ideia.md decidiu ter tema claro e escuro com botão para trocar; o MVP tem claro, escuro e automático (segue o sistema operacional). O botão fica no rodapé da barra lateral (ADR-0011). O ideia.md deixava para uma fase 2 salvar a preferência no perfil. Onde guardar a escolha de cada pessoa?

## Fatores de decisão

- A escolha deve acompanhar a pessoa em qualquer aparelho.
- Pouco esforço extra.
- Evitar retrabalho de design e de telas no futuro.

## Opções consideradas

- A. Claro, escuro e automático, com a preferência salva no perfil da pessoa
- B. Os mesmos três modos, com a preferência salva só no navegador
- C. Só o tema claro na v1

## Resultado da decisão

Opção escolhida: **A. Claro, escuro e automático, com a preferência salva no perfil**, porque a escolha acompanha a pessoa em qualquer aparelho e o perfil já existe desde o primeiro login (ADR-0005).

Regras:

- O padrão é "automático".
- A página de entrada, antes do login, segue sempre o sistema operacional.
- Os dois temas precisam ter contraste legível, verificado no design system.

### Consequências

- Bom, porque a preferência não se perde ao trocar de navegador ou aparelho.
- Bom, porque o design system nasce com os dois temas, sem revisão posterior das telas.
- Ruim, porque cada tela precisa ser desenhada e verificada nos dois temas.

### Confirmação

Testes cobrindo: a preferência salva no perfil é aplicada após o login em outro navegador; o modo automático segue o sistema operacional; a página de entrada usa o tema do sistema operacional.

## Prós e contras das opções

### A. Preferência no perfil

- Bom, porque acompanha a pessoa.
- Ruim, porque exige guardar e ler a preferência no servidor.

### B. Preferência só no navegador

- Bom, porque é o mais simples.
- Ruim, porque se perde ao trocar de navegador ou aparelho.

### C. Só tema claro na v1

- Bom, porque reduz design e testes visuais pela metade.
- Ruim, porque contradiz o ideia.md e obriga a revisar todas as telas quando o tema escuro entrar.
