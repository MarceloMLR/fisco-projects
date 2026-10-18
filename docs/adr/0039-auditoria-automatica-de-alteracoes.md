---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0039: Auditoria automática de todas as alterações e dos eventos de segurança

## Contexto e problema

O MVP tem um registro de quem fez o quê e quando, com filtro. Decisões anteriores contam com ele: trocas de papel (ADR-0006, ADR-0008) e criação, alteração e exclusão de acessos (ADR-0034). Os módulos também têm históricos próprios (linha do tempo do chamado, status dos documentos). O que a auditoria geral registra?

## Fatores de decisão

- Responder a qualquer pergunta do tipo "quem mudou isto e quando".
- Esforço baixo por módulo.
- Não expor dados sensíveis (senhas) e respeitar a LGPD.
- Expectativa de quem venha a adquirir o código.

## Opções consideradas

- A. Auditoria automática de todas as criações, alterações e exclusões, mais eventos de segurança
- B. Auditoria só de eventos escolhidos (segurança e administração)
- C. Sem auditoria geral, só os históricos de cada módulo

## Resultado da decisão

Opção escolhida: **A. Auditoria automática de todas as alterações**, porque cobre qualquer mudança com um único mecanismo e atende à expectativa de rastreabilidade de um produto corporativo.

Escopo:

| Item | Classificação |
| --- | --- |
| Registro automático de criações, alterações e exclusões nos dados principais, com quem, quando e os campos alterados (antes e depois) | v1 |
| Eventos de segurança: entrada, recusa de acesso, troca de papel, desativação e reativação, mudança de configuração | v1 |
| Senhas dos acessos nunca entram na auditoria, nem cifradas; a auditoria registra apenas que a senha foi alterada | v1 |
| Tela de Auditoria na Administração (só administradores), com filtros por pessoa, tipo de item, projeto e período | v1 |
| Registros somente leitura: ninguém altera nem apaga pelo sistema | v1 |
| Prazo de retenção e anonimização (LGPD) | no bloco de governança |
| Histórico de alterações do projeto visível para os gestores | depois |
| Exportar a auditoria | depois |
| Registrar quem visualizou cada item | fora |

Regra de implementação: o registro é feito num ponto único da camada de dados (por exemplo, ao salvar as alterações), e não em cada módulo.

### Consequências

- Bom, porque toda alteração tem rastro, sem esforço em cada módulo.
- Bom, porque eventos de segurança ficam reunidos para o administrador.
- Ruim, porque o volume de registros cresce com o uso.
- Ruim, porque a auditoria guarda dados pessoais e precisa de política de retenção e anonimização.

### Confirmação

Testes cobrindo: criar, alterar e excluir um item gera registro com antes e depois; eventos de segurança são registrados; alterar uma senha gera registro sem o valor da senha; não existe caminho para alterar ou apagar registros de auditoria; a tela de Auditoria só é acessível a administradores e os filtros funcionam.

## Prós e contras das opções

### A. Auditoria automática de tudo

- Bom, porque é completa e uniforme.
- Ruim, porque gera volume e exige cuidado com dados pessoais.

### B. Só eventos escolhidos

- Bom, porque é pequena e focada.
- Ruim, porque deixa sem rastro mudanças em cronograma e projetos.

### C. Só históricos dos módulos

- Bom, porque é o menor esforço.
- Ruim, porque não há visão única e muitas alterações ficam sem registro.
