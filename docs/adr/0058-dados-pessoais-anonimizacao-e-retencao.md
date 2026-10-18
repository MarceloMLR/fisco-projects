---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0058: Dados pessoais com anonimização a pedido e prazos de retenção configuráveis (LGPD)

## Contexto e problema

O FiscoProjects guarda dados pessoais das pessoas da empresa (nome, e-mail, vínculo com o Entra e atividade), e a auditoria registra o antes e o depois de cada alteração (ADR-0039). Ficaram pendentes a anonimização de usuários a pedido (ADR-0008) e o prazo de retenção da auditoria (ADR-0039). A empresa que usa o sistema é a controladora dos dados e decide os prazos; o sistema oferece padrões configuráveis. Esta decisão não é parecer jurídico; os prazos devem ser confirmados com um advogado.

## Fatores de decisão

- Atender pedidos de remoção sem quebrar o histórico dos projetos.
- Guardar dados pessoais só pelo tempo necessário.
- Rastrear quem executou a anonimização.
- Esforço proporcional à v1.

## Opções consideradas

- A. Anonimizar a pessoa, mantendo o histórico
- B. Excluir a pessoa e tudo o que ela criou
- C. Tratar diretamente no banco, quando acontecer

## Resultado da decisão

Opção escolhida: **A. Anonimizar, mantendo o histórico**, com prazos de retenção configuráveis.

Escopo:

| Item | Classificação |
| --- | --- |
| Anonimização a pedido, feita pelo administrador: nome, e-mail e vínculo com o Entra trocados por um identificador (por exemplo, "Pessoa removida nº 12") em todo o sistema, inclusive na auditoria | v1 |
| A anonimização fica registrada na auditoria (quem fez e quando), sem o nome da pessoa | v1 |
| Auditoria guardada por 5 anos (configurável), com limpeza automática mensal do que passar do prazo | v1 |
| Conteúdo dos e-mails enviados apagado da fila depois de 30 dias, ficando só a data e o tipo do aviso | v1 |
| Notificações do sino por 90 dias (ADR-0038) e logs sem dados sensíveis (ADR-0057) | v1 (já decidido) |
| Exportar os dados de uma pessoa (direito de acesso e portabilidade) | depois |
| Varrer o texto livre das mensagens ao anonimizar | fora (a empresa revisa caso a caso) |

### Consequências

- Bom, porque pedidos de remoção são atendidos sem buracos no histórico dos projetos.
- Bom, porque dados antigos são limpos automaticamente.
- Ruim, porque o texto livre das mensagens pode continuar citando a pessoa pelo nome.
- Neutro, porque os prazos padrão precisam ser validados por cada empresa com sua assessoria jurídica.

### Confirmação

Testes cobrindo: após a anonimização, nome, e-mail e vínculo com o Entra não aparecem em nenhuma tabela, inclusive na auditoria; chamados e mensagens continuam acessíveis; a anonimização gera registro na auditoria sem o nome; a limpeza mensal remove registros acima do prazo configurado; o conteúdo de e-mails com mais de 30 dias é apagado.

## Prós e contras das opções

### A. Anonimizar mantendo o histórico

- Bom, porque equilibra privacidade e integridade dos registros.
- Ruim, porque não alcança menções em texto livre.

### B. Excluir tudo da pessoa

- Bom, porque remove todos os dados.
- Ruim, porque quebra o histórico de chamados e projetos.

### C. Tratar no banco

- Bom, porque não exige esforço agora.
- Ruim, porque é arriscado e não deixa registro.
