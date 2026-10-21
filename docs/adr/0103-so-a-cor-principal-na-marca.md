---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0103: Só a cor principal na marca da instalação, sem cor secundária nem cores das mensagens

Substitui a cor secundária do ADR-0100 e o ADR-0101 inteiro. Mantém o ADR-0082 (cor principal), o logo para o tema escuro e o seletor de cores do ADR-0100.

## Contexto e problema

Na Acesso-8.1 e na Acesso-8.2, a marca da instalação ganhou uma cor secundária (barra lateral, destaques de apoio e painel da entrada) e uma cor por papel nas mensagens dos chamados, além do logo para o tema escuro e do seletor de cores. Ao usar as Configurações gerais, Marcelo pediu para voltar a ter só a cor principal: as cores extras abrem combinações demais para conferir e, por enquanto, não fazem falta. O logo para o tema escuro continua. O que sai e o que fica?

## Fatores de decisão

- Uma marca simples de configurar: um nome, os logos e uma cor.
- As telas iguais ao design system aprovado, a não ser pela cor principal.
- Nada de colunas sem uso no banco nem campos sem efeito na API.

## Opções consideradas

1. Tirar a cor secundária e as cores das mensagens da tela, da API e do banco.
2. Só esconder os campos da tela, mantendo a API e as colunas.

## Resultado da decisão

Opção 1. As Configurações gerais têm o nome, o logo, o logo para o tema escuro e a cor principal (com o seletor de cores do navegador). A marca (`GET /api/installation`) traz o nome, os endereços dos logos e a cor principal; o formulário não recebe mais a cor secundária nem as cores das mensagens. Uma migração apaga as sete colunas. A barra lateral, os destaques, o painel da entrada e a conversa dos chamados voltam às cores do design system, e a cor principal continua derivando os tokens da marca (ADR-0082).

O logo para o tema escuro aparece onde a barra lateral do design system é escura, isto é, no tema escuro; no claro, aparece o logo.

### Consequências

- Boa, porque a marca volta a ter uma cor só para conferir nos dois temas.
- Boa, porque as cores da conversa voltam a ser as do design system, conferidas na tela aprovada.
- Ruim, porque quem já tinha escolhido cores extras numa instalação de desenvolvimento as perde; não há instalação em produção.
- Os registros antigos da auditoria com esses campos continuam lá e aparecem pelo nome técnico do campo.
