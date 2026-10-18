---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0001: Registrar as decisões do projeto em ADR (formato MADR)

## Contexto e problema

O Marco está em discovery: vamos decidir, uma a uma, o escopo de cada módulo, a arquitetura e a infraestrutura. Cada decisão tem alternativas, prós e contras, e precisamos saber depois por que uma opção foi escolhida e outras descartadas, principalmente se o código vier a ser vendido ou mantido por outras pessoas. Em que formato e onde registrar essas decisões?

## Fatores de decisão

- Guardar o raciocínio (alternativas, prós e contras), não só o resultado.
- Servir para decisões de produto e escopo, não apenas de arquitetura.
- Texto simples, versionável no Git junto com o código.
- Pouco esforço para decisões pequenas.

## Opções consideradas

- ADR no formato MADR
- ADR no modelo original de Michael Nygard
- Log único de decisões (uma tabela em um arquivo)

## Resultado da decisão

Opção escolhida: **ADR no formato MADR**, porque registra contexto, alternativas, prós e contras e consequências, serve para qualquer tipo de decisão e tem uma variante mínima para decisões pequenas.

Regras:

- Um ADR por decisão, numerado com quatro dígitos (`0001`, `0002`…), arquivo `NNNN-titulo-em-kebab-case.md`, sem acentos.
- Escrito em português do Brasil.
- Status: `proposta` → `aceita`. Uma decisão substituída fica com status `substituída por ADR-NNNN`.
- ADR aceito não é editado no conteúdo: mudar de ideia gera um novo ADR que substitui o anterior.
- Uma decisão por vez: cada ADR é apresentado, discutido e aprovado isoladamente.
- Enquanto o repositório não existe, os ADRs ficam no projeto do Claude, em `claude/adr/`. Na primeira mudança do OpenSpec (setup do repositório), são movidos para `docs/adr/`.

### Consequências

- Bom, porque qualquer pessoa (ou o Claude Code em sessões futuras) entende por que cada escolha foi feita.
- Bom, porque decisões revistas ficam rastreáveis pela cadeia de substituição.
- Ruim, porque exige escrever um arquivo por decisão, mesmo as pequenas (mitigado pela variante mínima).

### Confirmação

Toda decisão tomada no discovery tem um ADR com status `aceita` antes de virar spec no OpenSpec. O `CLAUDE.md` do projeto vai apontar para `docs/adr/` como leitura obrigatória.

## Prós e contras das opções

### ADR no formato MADR

- Bom, porque registra as alternativas descartadas e o motivo.
- Bom, porque serve para decisões de produto, escopo, arquitetura e ferramentas.
- Bom, porque tem variante mínima para decisões simples.
- Neutro, porque tem mais seções que o modelo do Nygard.

### ADR no modelo original de Michael Nygard

- Bom, porque é o mais enxuto (contexto, decisão, consequências).
- Ruim, porque não registra as opções descartadas nem os prós e contras.

### Log único de decisões

- Bom, porque é rápido de escrever e de consultar.
- Ruim, porque perde o raciocínio de cada escolha.
- Ruim, porque um arquivo único gera conflitos e fica difícil de revisar no Git.

## Mais informações

- MADR: https://adr.github.io/madr/
