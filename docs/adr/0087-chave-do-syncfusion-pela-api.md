---
status: substituída pelo ADR-0134
date: 2026-10-19
decision-makers: Marcelo Ribeiro
---

# ADR-0087: Chave de licença do Syncfusion entregue pela API, a partir da configuração da instalação

Complementa o ADR-0051, que pôs a chave do Syncfusion fora do repositório, "na configuração ou nos segredos do ambiente", sem dizer como ela chega ao navegador.

## Contexto e problema

O Gantt do Syncfusion roda no navegador e só deixa de mostrar o aviso de versão de avaliação quando a página registra a chave de licença (`registerLicense`). Cada instalação é de uma empresa (ADR-0003), e a licença comunitária não é transferível: quem adquirir o código usa a própria chave (ADR-0051). Como a chave chega à página sem entrar no repositório?

## Fatores de decisão

- Nenhuma chave no repositório nem no workflow de build (ADR-0051, ADR-0053).
- Um mesmo build do frontend para todas as instalações (ADR-0003, ADR-0055).
- Cada instalação com a própria chave, trocada sem novo build.

## Opções consideradas

- A. Variável de ambiente no build do frontend (`VITE_...`), que grava a chave no JavaScript
- B. A API entrega a chave lida da configuração da instalação, a quem tem sessão

## Resultado da decisão

Opção escolhida: **B**, porque o build fica igual para todas as instalações e a chave muda só na configuração.

Regras:

- A chave fica na configuração `Syncfusion:LicenseKey`: nos segredos do usuário no desenvolvimento e no Key Vault em produção (ADR-0052). Sem ela, a linha do tempo funciona com o aviso de avaliação.
- A API devolve a chave em `GET /api/client-settings`, só com sessão, e nunca a grava em log.
- Só o componente `ScheduleGantt` lê essa chave e chama `registerLicense`, antes de desenhar a linha do tempo.

### Consequências

- Bom, porque o mesmo build serve a todas as instalações e nenhuma chave passa pelo GitHub.
- Bom, porque trocar a chave é mudar uma configuração.
- Ruim, porque a página faz uma chamada a mais antes da primeira linha do tempo.
- Neutro, porque a chave fica visível a quem tem sessão, como em qualquer licença registrada no navegador.

### Confirmação

Testes cobrindo: `GET /api/client-settings` devolve a chave configurada só com sessão e nada sem ela configurada; o build do frontend não contém chave; só o `ScheduleGantt` importa pacotes `@syncfusion/*`.

## Prós e contras das opções

### A. Variável no build

- Bom, porque não precisa de chamada à API.
- Ruim, porque cada instalação precisaria do próprio build e a chave passaria pelos segredos do CI.

### B. Chave pela API

- Bom, porque o build é único e a chave fica com a instalação.
- Ruim, porque acrescenta um endpoint e uma chamada.
