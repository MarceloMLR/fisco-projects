---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0043: Permissões no projeto e no cronograma

## Contexto e problema

Com os papéis definidos (ADR-0040) e as regras de membros (ADR-0042), falta a matriz de quem pode fazer o quê no projeto e no cronograma. Esta é a primeira parte da matriz de permissões; as demais tratam de chamados, de documentos, links, acessos e termos, e da Administração.

## Fatores de decisão

- Seguir o modelo de papéis do ADR-0040 (líder técnico com os poderes do gerente, exceto cronograma e termos).
- Decisões de gestão (status, cronograma) concentradas no gerente.
- Nenhum chamado esquecido ao encerrar um projeto.

## Opções consideradas

- A. Matriz abaixo, com o status do projeto só com o gerente
- B. Mesma matriz, com o líder técnico também mudando o status do projeto

## Resultado da decisão

Opção escolhida: **A**, porque pausar, concluir ou cancelar um projeto é decisão de gestão, como o cronograma.

Matriz:

| Ação | Administrador | Gerente | Líder técnico | Colaborador e Desenvolvedor | Leitor |
| --- | --- | --- | --- | --- | --- |
| Criar projeto | ✓ | ✓ (papel no sistema) | — | — | — |
| Ver o projeto e o cronograma | ✓ | ✓ | ✓ | ✓ | ✓ |
| Editar código, nome, descrição e cliente do projeto | ✓ | ✓ | ✓ | — | — |
| Mudar o status (pausar, concluir, cancelar, reabrir) | ✓ | ✓ | — | — | — |
| Excluir o projeto (ADR-0041) | ✓ | — | — | — | — |
| Incluir, remover e trocar o papel das pessoas (ADR-0042) | ✓ | ✓ | ✓, menos gerentes | — | — |
| Editar o cronograma (fases, tarefas, marcos, dependências, responsáveis, linha de base) | ✓ | ✓ | — (só vê) | — | — |
| Atualizar o progresso de uma tarefa | ✓ | ✓, qualquer tarefa | só as próprias | só as próprias | — |
| Marcar um marco como atingido | ✓ | ✓ | — | — | — |
| Salvar o cronograma como modelo | ✓ | ✓ | — | — | — |
| Exportar o cronograma (PDF e XML) | ✓ | ✓ | ✓ | ✓ | ✓ |

Regras de encerramento do projeto:

- O projeto só pode ser concluído quando todos os seus chamados estão fechados (qualquer status diferente de Fechado impede a conclusão, inclusive Resolvido aguardando confirmação).
- Ao cancelar o projeto, os chamados abertos são fechados automaticamente com o motivo "projeto cancelado" registrado na linha do tempo, depois de uma confirmação que mostra quantos são.
- Concluir com tarefas abaixo de 100% ou termos de aceite pendentes gera um aviso, mas não bloqueia.
- Projeto concluído ou cancelado é somente leitura e não recebe chamados novos até ser reaberto (ADR-0016, ADR-0022).

Outras regras:

- O Leitor não é responsável por tarefas.
- Quem atende numa fila sem ser membro do projeto não vê o projeto nem o cronograma, só os chamados da fila (ADR-0023).

### Consequências

- Bom, porque decisões de gestão ficam com o gerente.
- Bom, porque nenhum projeto é concluído com chamados abertos, e o cancelamento não deixa chamados órfãos.
- Ruim, porque concluir um projeto pode exigir fechar ou cancelar chamados antes.

### Confirmação

Testes cobrindo cada célula da matriz (permitido e negado) e as regras de encerramento: conclusão bloqueada com chamado não fechado; cancelamento fecha os chamados abertos com o motivo na linha do tempo; aviso ao concluir com tarefas incompletas ou aceites pendentes; projeto encerrado não aceita alterações nem chamados novos.

## Prós e contras das opções

### A. Status só com o gerente

- Bom, porque concentra a decisão de encerrar no gerente.
- Ruim, porque o líder técnico depende do gerente para pausar o projeto.

### B. Líder técnico também muda o status

- Bom, porque dá mais autonomia ao líder técnico.
- Ruim, porque divide uma decisão de gestão.
