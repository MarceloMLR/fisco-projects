---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0024: Fluxo de status dos chamados em tabela, com o fluxo do MVP como padrão

## Contexto e problema

O MVP tem seis status (Aberto, Em triagem, Em andamento, Aguardando solicitante, Resolvido e Fechado) e uma tabela de transições com a ação, quem pode executá-la e se ela exige mensagem. O ideia.md pede que o fluxo fique em tabela, e não fixo no código, porque o processo vai mudar. Quanto dessa flexibilidade entra na v1?

## Fatores de decisão

- O processo de atendimento pode mudar com o uso.
- Esforço proporcional à v1.
- Evitar fluxos quebrados (status sem saída, transições inválidas).
- Relatórios por status consistentes.

## Opções consideradas

- A. Fluxo fixo no código
- B. Fluxo em tabela, com o do MVP como padrão e sem tela de edição na v1
- C. Editor de fluxo na Administração

## Resultado da decisão

Opção escolhida: **B. Fluxo em tabela, com o do MVP como padrão**, porque a estrutura já nasce pronta para mudar e um editor pode ser acrescentado depois sem refazer as regras.

Escopo:

| Item | Classificação |
| --- | --- |
| Status e transições do MVP (ação, quem pode e se exige mensagem) guardados em tabela | v1 |
| A primeira resposta de quem atende num chamado Aberto equivale a "Assumir chamado": define o responsável (ADR-0023) e muda o status para Em andamento | v1 |
| Chamado Resolvido sem retorno do solicitante fecha sozinho depois de 5 dias úteis (valor configurável) | v1 |
| Página "Fluxo de chamados", com o diagrama e a tabela, montada a partir das regras cadastradas | v1 |
| Fluxos diferentes por destino (funcional, técnico, filas) | depois |
| Editor de fluxo na Administração | depois |
| Reabrir um chamado Fechado | fora (abre-se um chamado novo) |

Fluxo padrão (do MVP):

| De | Para | Ação | Quem pode | Exige mensagem |
| --- | --- | --- | --- | --- |
| Aberto | Em triagem | Iniciar triagem | Quem atende | Não |
| Aberto | Em andamento | Assumir chamado (ou primeira resposta de quem atende) | Quem atende | Não |
| Em triagem | Em andamento | Iniciar atendimento | Quem atende | Não |
| Em andamento | Aguardando solicitante | Pedir informações | Quem atende | Sim |
| Aguardando solicitante | Em andamento | Enviar resposta | Solicitante ou quem atende | Sim |
| Em andamento | Resolvido | Marcar como resolvido | Quem atende | Não |
| Resolvido | Fechado | Confirmar e fechar | Solicitante, gestor ou administrador | Não |
| Resolvido | Fechado | Fechamento automático após o prazo configurado | Sistema | Não |
| Resolvido | Em andamento | Reabrir | Solicitante | Sim |
| Aberto ou Em triagem | Fechado | Cancelar chamado | Solicitante, gestor ou administrador | Sim |

### Consequências

- Bom, porque mudar o fluxo não exige reescrever regras de negócio.
- Bom, porque o editor de fluxo pode vir depois sobre a mesma estrutura.
- Bom, porque o fechamento automático evita chamados parados em Resolvido.
- Ruim, porque, na v1, mudar o fluxo exige alguém técnico.
- Ruim, porque o sistema precisa validar as transições lidas do banco e garantir a integridade do fluxo (todo status com saída, exceto Fechado).
- Neutro, porque a coluna "Quem pode" usa papéis que serão revistos na matriz de permissões.

### Confirmação

Testes cobrindo: cada transição do fluxo padrão é permitida a quem pode e negada a quem não pode; transições que exigem mensagem são recusadas sem mensagem; a primeira resposta de quem atende num chamado Aberto define o responsável e muda para Em andamento; o fechamento automático acontece após o prazo configurado, em dias úteis; a página do fluxo reflete a tabela; não existe caminho para reabrir um chamado Fechado.

## Prós e contras das opções

### A. Fluxo fixo no código

- Bom, porque é o mais simples de construir e testar.
- Ruim, porque qualquer mudança no processo exige nova versão.

### B. Fluxo em tabela, sem editor na v1

- Bom, porque prepara a mudança de processo com esforço moderado.
- Ruim, porque, sem tela, só alguém técnico altera o fluxo.

### C. Editor de fluxo na Administração

- Bom, porque dá flexibilidade total a cada empresa.
- Ruim, porque é muito trabalho e abre risco de fluxos quebrados.
- Ruim, porque relatórios por status ficam difíceis com status diferentes por empresa.
