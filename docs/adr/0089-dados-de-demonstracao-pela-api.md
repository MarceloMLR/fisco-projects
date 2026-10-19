---
status: proposta
date: 2026-10-19
decision-makers: Marcelo Ribeiro
---

# ADR-0089: Dados de demonstração carregados por um comando que usa a própria API

Complementa a regra do roadmap "Os dados fictícios de demonstração crescem a cada change" e o ADR-0078 (entrada local).

## Contexto e problema

Cada change com tela deveria deixar dados fictícios de demonstração, mas eles só existiram no ambiente do agente, criados por scripts fora do repositório. Na máquina de Marcelo, depois de atualizar o código, as telas aparecem vazias. Como carregar, em qualquer instalação de desenvolvimento, as mesmas pessoas, clientes, projetos e cronogramas das capturas?

## Fatores de decisão

- Dados só fictícios e nunca em produção (ADR-0002, ADR-0003).
- As regras do sistema valem para os dados de demonstração: datas pelo calendário, permissões, auditoria.
- Fácil de manter: cada change acrescenta os dados dela sem conhecer o banco.
- Datas sempre atuais, para haver tarefas atrasadas, a linha de hoje e projetos em andamento.

## Opções consideradas

- A. Script SQL que grava direto nas tabelas
- B. Carga na subida da API, em desenvolvimento, chamando os casos de uso dos módulos
- C. Um comando que chama a própria API, entrando pela entrada local como cada pessoa

## Resultado da decisão

Opção escolhida: **C**, porque os dados passam pelas mesmas regras e permissões que a tela usa, e o comando não funciona onde a entrada local está desligada, como em produção (ADR-0078).

Regras:

- O comando fica no frontend (`npm run demo:data`), em TypeScript, com os tipos gerados do contrato da API; uma mudança da API que o quebre reprova a checagem de tipos.
- Ele entra como as pessoas do diretório de demonstração (ADR-0078) e faz cada ação com quem a faria: o administrador cria clientes e projetos, a gerente monta o cronograma e cada responsável atualiza o progresso da própria tarefa.
- As datas são relativas ao dia em que roda: a segunda-feira de duas semanas antes é o primeiro dia do projeto principal.
- Se os dados já foram carregados (o projeto ERP-FIN existe), o comando não muda nada.
- Cada change que cria telas ou dados novos acrescenta os dela ao comando.

### Consequências

- Bom, porque os dados respeitam todas as regras e aparecem na auditoria como ações das pessoas.
- Bom, porque não há como carregá-los em produção.
- Ruim, porque o comando precisa da API rodando e é mais lento que um script SQL.
- Neutro, porque os nomes das pessoas e empresas ficam no comando, fora do código do sistema (ADR-0003).

### Confirmação

Testes cobrindo: o comando cria as pessoas, os clientes, os projetos, os membros e os cronogramas pelas rotas da API, com quem faz cada ação; não muda nada quando os dados já existem; para com uma mensagem quando a entrada local está desligada; as datas acompanham o dia em que roda.

## Prós e contras das opções

### A. Script SQL

- Bom, porque é rápido e não precisa da API.
- Ruim, porque pula as regras do domínio, as datas calculadas e a auditoria, e quebra a cada migração.

### B. Carga na subida da API

- Bom, porque não precisa de comando separado.
- Ruim, porque põe dados de demonstração dentro do host da API, perto da produção, e precisa conhecer os casos de uso de todos os módulos.

### C. Comando pela API

- Bom, porque usa só o que a tela usa e não chega à produção.
- Ruim, porque depende da API e da entrada local ligadas.
